using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public struct Step
{
    public string action;
    public int? index;
}

[Serializable]
public class MutationOperation
{
    public string path;
    public string newValue;
    public bool invalid;

    public void AddStepToPath(Step step)
    {
        path = (string.IsNullOrEmpty(path) ? "" : path + Mutation.levelSeparator) + step.action + (step.index == null ? "" : (Mutation.indexLeftSeparator + step.index + Mutation.indexRightSeparator));
    }
}

public static class Mutation
{
    public const string levelSeparator = ">";
    public const string indexLeftSeparator = "[";
    public const string indexRightSeparator = "]";

    private class WeightedActionList : List<(float, Action)>
    {
        public void ChooseAction()
        {
            float choiceIndex = UnityEngine.Random.value * this.Sum(x => x.Item1);
            float currentPosition = 0f;
            foreach ((float weight, Action action) in this)
            {
                if (choiceIndex <= currentPosition + weight)
                {
                    action();
                    break;
                }
                currentPosition += weight;
            }
        }
    }

    public static Func<Genotype, Genotype> ParseMutationFunction(MutationOperation mutationOperation)
    {
        if (mutationOperation.invalid)
            return (genotype) => genotype;

        Queue<Step> pathTokens = new();

        // TODO: This is a rare bug. Remove this after we know what causes it.
        if (string.IsNullOrEmpty(mutationOperation.path)) {
            Debug.Log("ERROR: Empty Mutation Bug\n" + mutationOperation.path + " : " + mutationOperation.newValue + " : " + mutationOperation.invalid);
            return (genotype) => genotype;
        }

        foreach (string token in mutationOperation.path.Split(levelSeparator))
        {
            string[] sections = token.Split(indexLeftSeparator);
            string action = sections[0];
            int? index = sections.Length > 1 ? int.Parse(sections[1].Split(indexRightSeparator)[0]) : null;
            pathTokens.Enqueue(new() { action = action, index = index });
        }
        Func<Genotype, Genotype> mutationFunction = RootMutation.ParseMutationFunction(pathTokens, mutationOperation.newValue);
        return (Genotype genotype) => mutationFunction(genotype);
    }

    public static MutationOperation CreateRandomMutation(Genotype genotype)
    {
        return RootMutation.CreateRandomMutation(genotype);
    }

    public static MutationOperation AddPotentialLimbNode(Genotype genotype)
    {
        return RootMutation.AddPotentialLimbNode(genotype);
    }

    public static class RootMutation
    {
        public const string ChangeBrain = "ChangeBrain";
        public const string ChangeLimbNode = "ChangeLimbNode";
        public const string AddDisconnectedLimbNode = "AddDisconnectedLimbNode"; // Special - operation is done once for every child, but is removed if no connections mutate to point to the new node.

        public static Func<Genotype, Genotype> ParseMutationFunction(Queue<Step> pathTokens, string newValue)
        {
            Step step = pathTokens.Dequeue();
            Func<IList<NeuronDefinition>, IList<NeuronDefinition>> brainMutationFunc = step.action == ChangeBrain ? BrainMutation.ParseMutationFunction(pathTokens, newValue) : null;
            Func<LimbNode, LimbNode> limbNodeMutationFunc = step.action == ChangeLimbNode ? LimbNodeMutation.ParseMutationFunction(pathTokens, newValue) : null;
            return (genotype) =>
            {
                return Genotype.Construct(
                    genotype.Id,
                    genotype.Ancestry,
                    step.action == ChangeBrain ? brainMutationFunc(genotype.BrainNeuronDefinitions) : genotype.BrainNeuronDefinitions,
                    step.action == AddDisconnectedLimbNode ? genotype.LimbNodes.Concat(new[] { JsonUtility.FromJson<LimbNode>(newValue) }).ToList()
                    : step.action == ChangeLimbNode ? genotype.LimbNodes.Select((l, i) => i == (int)step.index ? limbNodeMutationFunc(l) : l).ToList() : genotype.LimbNodes
                );
            };
        }

        public static MutationOperation CreateRandomMutation(Genotype genotype)
        {
            MutationOperation mutation = new();
            WeightedActionList mutationChoices = new()
            {
                (MutationParameters.Root.ChangeBrain, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeBrain });
                    mutation = BrainMutation.CreateRandomMutation(genotype, mutation);
                })),
                (MutationParameters.Root.ChangeLimbNode, new Action(() =>
                {
                    int index = UnityEngine.Random.Range(0, genotype.LimbNodes.Count);
                    mutation.AddStepToPath(new() { action = ChangeLimbNode, index = index });
                    mutation.invalid = genotype.LimbNodes.Count == 0;
                    mutation = mutation.invalid ? mutation : LimbNodeMutation.CreateRandomMutation(genotype, index, mutation);
                }))
            };
            mutationChoices.ChooseAction();
            return mutation;
        }

        public static MutationOperation AddPotentialLimbNode(Genotype genotype)
        {
            MutationOperation mutation = new();
            mutation.AddStepToPath(new() { action = AddDisconnectedLimbNode });
            mutation.invalid = genotype.LimbNodes.Count == GenotypeParameters.MaxLimbNodes;
            if (!mutation.invalid)
            {
                UnfinishedLimbNode unfinishedLimbNode = UnfinishedLimbNode.CreateRandom(null);
                List<ILimbNodeEssentialInfo> tempLimbNodes = genotype.LimbNodes.Cast<ILimbNodeEssentialInfo>().ToList();
                tempLimbNodes.Add(unfinishedLimbNode);
                LimbNode newLimbNode = LimbNode.CreateRandom(
                    EmitterAvailabilityMap.GenerateMapForLimbNode(genotype.BrainNeuronDefinitions.Count, tempLimbNodes, tempLimbNodes.Count - 1),
                    unfinishedLimbNode
                );
                mutation.newValue = JsonUtility.ToJson(newLimbNode);
            }
            return mutation;
        }
    }

    public static class BrainMutation
    {
        public const string AddNeuron = "AddNeuron";
        public const string RemoveNeuron = "RemoveNeuron";
        public const string ChangeNeuronDefinition = "ChangeNeuronDefinition";

        public static Func<IList<NeuronDefinition>, IList<NeuronDefinition>> ParseMutationFunction(Queue<Step> pathTokens, string newValue)
        {
            Step step = pathTokens.Dequeue();
            Func<NeuronDefinition, NeuronDefinition> neuronDefinitionMutationFunc = step.action == ChangeNeuronDefinition ? NeuronDefinitionMutation.ParseMutationFunction(pathTokens, newValue) : null;
            return (brainNeuronDefinitions) =>
            {
                return step.action == AddNeuron ? brainNeuronDefinitions.Concat(new[] { JsonUtility.FromJson<NeuronDefinition>(newValue) }).ToList()
                    : step.action == RemoveNeuron ? brainNeuronDefinitions.Where((n, i) => i != (int)step.index).ToList()
                    : step.action == ChangeNeuronDefinition ? brainNeuronDefinitions.Select((n, i) => i == (int)step.index ? neuronDefinitionMutationFunc(n) : n).ToList() : brainNeuronDefinitions;
            };
        }

        public static MutationOperation CreateRandomMutation(Genotype genotype, MutationOperation mutation)
        {
            WeightedActionList mutationChoices = new()
            {
                (MutationParameters.Brain.AddNeuron, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = AddNeuron });
                    mutation.invalid = genotype.BrainNeuronDefinitions.Count == GenotypeParameters.MaxBrainNeurons;
                    mutation.newValue = mutation.invalid ? null : JsonUtility.ToJson(NeuronDefinition.CreateRandom(
                        EmitterAvailabilityMap.GenerateMapForBrain(genotype.BrainNeuronDefinitions.Count, genotype.InstancedLimbNodes)
                    ));
                })),
                (MutationParameters.Brain.RemoveNeuron, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = RemoveNeuron, index = UnityEngine.Random.Range(0, genotype.BrainNeuronDefinitions.Count) });
                    mutation.invalid = genotype.BrainNeuronDefinitions.Count == 0;
                })),
                (MutationParameters.Brain.ChangeNeuronDefinition, new Action(() =>
                {
                    int index = UnityEngine.Random.Range(0, genotype.BrainNeuronDefinitions.Count);
                    mutation.AddStepToPath(new() { action = ChangeNeuronDefinition, index = index });
                    mutation.invalid = genotype.BrainNeuronDefinitions.Count == 0;
                    if (!mutation.invalid)
                        mutation = NeuronDefinitionMutation.CreateRandomMutation(
                            EmitterAvailabilityMap.GenerateMapForBrain(genotype.BrainNeuronDefinitions.Count, genotype.InstancedLimbNodes), genotype.BrainNeuronDefinitions[index],
                            mutation
                        );
                }))
            };
            mutationChoices.ChooseAction();
            return mutation;
        }
    }

    public static class LimbNodeMutation
    {
        public const string ChangeDimensions = "ChangeDimensions";
        public const string ChangeJointDefinition = "ChangeJointDefinition";
        public const string ChangeRecursiveLimit = "ChangeRecursiveLimit";
        public const string AddNeuron = "AddNeuron";
        public const string RemoveNeuron = "RemoveNeuron";
        public const string ChangeNeuronDefinition = "ChangeNeuronDefinition";
        public const string AddLimbConnection = "AddLimbConnection";
        public const string RemoveLimbConnection = "RemoveLimbConnection";
        public const string ChangeLimbConnection = "ChangeLimbConnection";

        public static Func<LimbNode, LimbNode> ParseMutationFunction(Queue<Step> pathTokens, string newValue)
        {
            Step step = pathTokens.Dequeue();
            Func<JointDefinition, JointDefinition> jointDefinitionMutationFunc = step.action == ChangeJointDefinition ? JointDefinitionMutation.ParseMutationFunction(pathTokens, newValue) : null;
            Func<NeuronDefinition, NeuronDefinition> neuronDefinitionMutationFunc = step.action == ChangeNeuronDefinition ? NeuronDefinitionMutation.ParseMutationFunction(pathTokens, newValue) : null;
            Func<LimbConnection, LimbConnection> limbConnectionMutationFunc = step.action == ChangeLimbConnection ? LimbConnectionMutation.ParseMutationFunction(pathTokens, newValue) : null;

            return (limbNode) =>
            {
                return new(
                    step.action == ChangeDimensions ? JsonUtility.FromJson<Vector3>(newValue) : limbNode.Dimensions,
                    step.action == ChangeJointDefinition ? jointDefinitionMutationFunc(limbNode.JointDefinition) : limbNode.JointDefinition,
                    step.action == ChangeRecursiveLimit ? int.Parse(newValue) : limbNode.RecursiveLimit,
                    step.action == AddNeuron ? limbNode.NeuronDefinitions.Concat(new[] { JsonUtility.FromJson<NeuronDefinition>(newValue) }).ToList()
                    : step.action == RemoveNeuron ? limbNode.NeuronDefinitions.Where((n, i) => i != (int)step.index).ToList()
                    : step.action == ChangeNeuronDefinition ? limbNode.NeuronDefinitions.Select((n, i) => i == (int)step.index ? neuronDefinitionMutationFunc(n) : n).ToList() : limbNode.NeuronDefinitions,
                    step.action == AddLimbConnection ? limbNode.Connections.Concat(new[] { JsonUtility.FromJson<LimbConnection>(newValue) }).ToList()
                    : step.action == RemoveLimbConnection ? limbNode.Connections.Where((c, i) => i != (int)step.index).ToList()
                    : step.action == ChangeLimbConnection ? limbNode.Connections.Select((c, i) => i == (int)step.index ? limbConnectionMutationFunc(c) : c).ToList() : limbNode.Connections
                );
            };
        }

        public static MutationOperation CreateRandomMutation(Genotype genotype, int limbNodeIndex, MutationOperation mutation)
        {
            LimbNode limbNode = genotype.LimbNodes[limbNodeIndex];
            WeightedActionList mutationChoices = new()
            {
                (MutationParameters.LimbNode.ChangeDimensions, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeDimensions });
                    mutation.newValue = JsonUtility.ToJson(MutateVector(limbNode.Dimensions, LimbNodeParameters.MinSize, LimbNodeParameters.MaxSize));
                })),
                (MutationParameters.LimbNode.ChangeJointDefinition, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeJointDefinition });
                    EmitterAvailabilityMap map = EmitterAvailabilityMap.GenerateMapForLimbNode(genotype.BrainNeuronDefinitions.Count, genotype.LimbNodes.Cast<ILimbNodeEssentialInfo>().ToList(), limbNodeIndex);
                    mutation = JointDefinitionMutation.CreateRandomMutation(map, limbNode.JointDefinition, mutation);
                })),
                (MutationParameters.LimbNode.ChangeRecursiveLimit, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeRecursiveLimit });
                    mutation.newValue = MutateScalar(limbNode.RecursiveLimit, 0, LimbNodeParameters.MaxRecursiveLimit).ToString();
                })),
                (MutationParameters.LimbNode.AddNeuron, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = AddNeuron });
                    mutation.invalid = limbNode.NeuronDefinitions.Count == LimbNodeParameters.MaxNeurons;
                    mutation.newValue = mutation.invalid ? null : JsonUtility.ToJson(NeuronDefinition.CreateRandom(
                        EmitterAvailabilityMap.GenerateMapForLimbNode(genotype.BrainNeuronDefinitions.Count, genotype.LimbNodes.Cast<ILimbNodeEssentialInfo>().ToList(), limbNodeIndex)
                    ));
                })),
                (MutationParameters.LimbNode.RemoveNeuron, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = RemoveNeuron, index = UnityEngine.Random.Range(0, limbNode.NeuronDefinitions.Count) });
                    mutation.invalid = limbNode.NeuronDefinitions.Count == 0;
                })),
                (MutationParameters.LimbNode.ChangeNeuronDefinition, new Action(() =>
                {
                    int index = UnityEngine.Random.Range(0, limbNode.NeuronDefinitions.Count);
                    mutation.AddStepToPath(new() { action = ChangeNeuronDefinition, index = index });
                    mutation.invalid = limbNode.NeuronDefinitions.Count == 0;
                    if (!mutation.invalid)
                    {
                        EmitterAvailabilityMap map = EmitterAvailabilityMap.GenerateMapForLimbNode(genotype.BrainNeuronDefinitions.Count, genotype.LimbNodes.Cast<ILimbNodeEssentialInfo>().ToList(), limbNodeIndex);
                        mutation = NeuronDefinitionMutation.CreateRandomMutation(map, limbNode.NeuronDefinitions[index], mutation);
                    }
                })),
                (MutationParameters.LimbNode.AddLimbConnection, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = AddLimbConnection });
                    mutation.invalid = limbNode.Connections.Count == LimbNodeParameters.MaxLimbConnections;
                    mutation.newValue = mutation.invalid ? null : JsonUtility.ToJson(LimbConnection.CreateRandom(UnityEngine.Random.Range(0, genotype.LimbNodes.Count)));
                })),
                (MutationParameters.LimbNode.RemoveLimbConnection, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = RemoveLimbConnection, index = UnityEngine.Random.Range(0, limbNode.Connections.Count) });
                    mutation.invalid = limbNode.Connections.Count == 0;
                })),
                (MutationParameters.LimbNode.ChangeLimbConnection, new Action(() =>
                {
                    int index = UnityEngine.Random.Range(0, limbNode.Connections.Count);
                    mutation.AddStepToPath(new() { action = ChangeLimbConnection, index = index });
                    mutation.invalid = limbNode.Connections.Count == 0;
                    if (!mutation.invalid)
                        mutation = LimbConnectionMutation.CreateRandomMutation(genotype, limbNodeIndex, index, mutation);
                }))
            };
            mutationChoices.ChooseAction();
            return mutation;
        }
    }

    public static class JointDefinitionMutation
    {
        public const string ChangeJointType = "ChangeJointType";
        public const string ChangeJointAxisDefinition = "ChangeJointAxisDefinition";

        public static Func<JointDefinition, JointDefinition> ParseMutationFunction(Queue<Step> pathTokens, string newValue)
        {
            Step step = pathTokens.Dequeue();
            Func<JointAxisDefinition, JointAxisDefinition> jointAxisDefinitionMutationFunc = step.action == ChangeJointAxisDefinition ? JointAxisDefinitionMutation.ParseMutationFunction(pathTokens, newValue) : null;
            return (jointDefinition) =>
            {
                return new(
                    step.action == ChangeJointType ? Enum.Parse<JointType>(Utilities.SentenceToPascalCase(newValue)) : jointDefinition.Type,
                    step.action == ChangeJointAxisDefinition ? jointDefinition.AxisDefinitions.Select((a, i) => i == (int)step.index ? jointAxisDefinitionMutationFunc(a) : a).ToList() : jointDefinition.AxisDefinitions
                );
            };
        }

        public static MutationOperation CreateRandomMutation(EmitterAvailabilityMap emitterAvailabilityMap, JointDefinition jointDefinition, MutationOperation mutation)
        {
            WeightedActionList mutationChoices = new()
            {
                (MutationParameters.JointDefinition.ChangeJointType, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeJointType });
                    mutation.newValue = MutateEnum(jointDefinition.Type).ToString();
                })),
                (MutationParameters.JointDefinition.ChangeJointAxisDefinition, new Action(() =>
                {
                    int index = UnityEngine.Random.Range(0, jointDefinition.AxisDefinitions.Count);
                    mutation.AddStepToPath(new() { action = ChangeJointAxisDefinition, index = index });
                    mutation.invalid = jointDefinition.AxisDefinitions.Count == 0;
                    if (!mutation.invalid)
                        mutation = JointAxisDefinitionMutation.CreateRandomMutation(emitterAvailabilityMap, jointDefinition.AxisDefinitions[index], mutation);
                }))
            };
            mutationChoices.ChooseAction();
            return mutation;
        }
    }

    public static class JointAxisDefinitionMutation
    {
        public const string ChangeJointLimit = "ChangeJointLimit";
        public const string ChangeInputDefinition = "ChangeInputDefinition";

        public static Func<JointAxisDefinition, JointAxisDefinition> ParseMutationFunction(Queue<Step> pathTokens, string newValue)
        {
            Step step = pathTokens.Dequeue();
            Func<InputDefinition, InputDefinition> inputDefinitionMutationFunc = step.action == ChangeInputDefinition ? InputDefinitionMutation.ParseMutationFunction(pathTokens, newValue) : null;
            return (jointAxisDefinition) =>
            {
                return new(
                    step.action == ChangeJointLimit ? float.Parse(newValue) : jointAxisDefinition.Limit,
                    step.action == ChangeInputDefinition ? inputDefinitionMutationFunc(jointAxisDefinition.InputDefinition) : jointAxisDefinition.InputDefinition
                );
            };
        }

        public static MutationOperation CreateRandomMutation(EmitterAvailabilityMap emitterAvailabilityMap, JointAxisDefinition jointAxisDefinition, MutationOperation mutation)
        {
            WeightedActionList mutationChoices = new()
            {
                (MutationParameters.JointAxisDefinition.ChangeJointLimit, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeJointLimit });
                    mutation.newValue = MutateScalar(jointAxisDefinition.Limit, JointDefinitionParameters.MinAngle, JointDefinitionParameters.MaxAngle).ToString();
                })),
                (MutationParameters.JointAxisDefinition.ChangeInputDefinition, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeInputDefinition });
                    mutation = InputDefinitionMutation.CreateRandomMutation(emitterAvailabilityMap, jointAxisDefinition.InputDefinition, mutation);
                }))
            };
            mutationChoices.ChooseAction();
            return mutation;
        }
    }

    public static class LimbConnectionMutation
    {
        public const string ChangeChildNode = "ChangeChildNode";
        public const string ChangeParentFace = "ChangeParentFace";
        public const string ChangePosition = "ChangePosition";
        public const string ChangeOrientation = "ChangeOrientation";
        public const string ChangeScale = "ChangeScale";
        public const string ChangeReflectionX = "ChangeReflectionX";
        public const string ChangeReflectionY = "ChangeReflectionY";
        public const string ChangeReflectionZ = "ChangeReflectionZ";
        public const string ChangeTerminalOnly = "ChangeTerminalOnly";

        public static Func<LimbConnection, LimbConnection> ParseMutationFunction(Queue<Step> pathTokens, string newValue)
        {
            Step step = pathTokens.Dequeue();
            return (connection) =>
            {
                return new(
                    step.action == ChangeChildNode ? int.Parse(newValue) : connection.ChildNodeId,
                    step.action == ChangeParentFace ? int.Parse(newValue) : connection.ParentFace,
                    step.action == ChangePosition ? JsonUtility.FromJson<Vector3>(newValue) : connection.Position,
                    step.action == ChangeOrientation ? JsonUtility.FromJson<Vector3>(newValue) : connection.Orientation,
                    step.action == ChangeScale ? JsonUtility.FromJson<Vector3>(newValue) : connection.Scale,
                    step.action == ChangeReflectionX ? bool.Parse(newValue) : connection.ReflectionX,
                    step.action == ChangeReflectionY ? bool.Parse(newValue) : connection.ReflectionY,
                    step.action == ChangeReflectionZ ? bool.Parse(newValue) : connection.ReflectionZ,
                    step.action == ChangeTerminalOnly ? bool.Parse(newValue) : connection.TerminalOnly
                );
            };
        }

        public static MutationOperation CreateRandomMutation(Genotype genotype, int limbNodeIndex, int limbConnectionIndex, MutationOperation mutation)
        {
            LimbConnection connection = genotype.LimbNodes[limbNodeIndex].Connections[limbConnectionIndex];
            WeightedActionList mutationChoices = new()
            {
                (MutationParameters.LimbConnection.ChangeChildNode, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeChildNode });
                    mutation.newValue = MutateScalar(connection.ChildNodeId, 0, genotype.LimbNodes.Count - 1).ToString();
                })),
                (MutationParameters.LimbConnection.ChangeParentFace, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeParentFace });
                    mutation.newValue = MutateScalar(connection.ParentFace, 0, 5).ToString();
                })),
                (MutationParameters.LimbConnection.ChangePosition, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangePosition });
                    mutation.newValue = JsonUtility.ToJson(MutateVector(connection.Position, -1f, 1f));
                })),
                (MutationParameters.LimbConnection.ChangeOrientation, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeOrientation });
                    mutation.newValue = JsonUtility.ToJson(MutateVector(connection.Orientation, LimbConnectionParameters.MinAngle, LimbConnectionParameters.MaxAngle));
                })),
                (MutationParameters.LimbConnection.ChangeScale, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeScale });
                    mutation.newValue = JsonUtility.ToJson(MutateVector(connection.Scale, LimbConnectionParameters.MinScale, LimbConnectionParameters.MaxScale));
                })),
                (MutationParameters.LimbConnection.ChangeReflectionX, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeReflectionX });
                    mutation.newValue = (!connection.ReflectionX).ToString();
                })),
                (MutationParameters.LimbConnection.ChangeReflectionY, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeReflectionY });
                    mutation.newValue = (!connection.ReflectionY).ToString();
                })),
                (MutationParameters.LimbConnection.ChangeReflectionZ, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeReflectionZ });
                    mutation.newValue = (!connection.ReflectionZ).ToString();
                })),
                (MutationParameters.LimbConnection.ChangeTerminalOnly, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeTerminalOnly });
                    mutation.newValue = (!connection.TerminalOnly).ToString();
                }))
            };
            mutationChoices.ChooseAction();
            return mutation;
        }
    }

    public static class NeuronDefinitionMutation
    {
        public const string ChangeNeuronType = "ChangeNeuronType";
        public const string ChangeInputDefinition = "ChangeInputDefinition";

        public static Func<NeuronDefinition, NeuronDefinition> ParseMutationFunction(Queue<Step> pathTokens, string newValue)
        {
            Step step = pathTokens.Dequeue();
            Func<InputDefinition, InputDefinition> inputDefinitionMutationFunc = step.action == ChangeInputDefinition ? InputDefinitionMutation.ParseMutationFunction(pathTokens, newValue) : null;
            return (neuronDefinition) =>
            {
                return new(
                    step.action == ChangeNeuronType ? Enum.Parse<NeuronType>(newValue) : neuronDefinition.Type,
                    step.action == ChangeInputDefinition ? neuronDefinition.InputDefinitions.Select((def, i) => i == (int)step.index ? inputDefinitionMutationFunc(def) : def).ToList() : neuronDefinition.InputDefinitions
                );
            };
        }

        public static MutationOperation CreateRandomMutation(EmitterAvailabilityMap emitterAvailabilityMap, NeuronDefinition neuronDefinition, MutationOperation mutation)
        {
            WeightedActionList mutationChoices = new()
            {
                (MutationParameters.NeuronDefinition.ChangeNeuronType, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeNeuronType });
                    mutation.newValue = MutateEnum(neuronDefinition.Type).ToString();
                })),
                (MutationParameters.NeuronDefinition.ChangeInputDefinition, new Action(() =>
                {
                    int index = UnityEngine.Random.Range(0, neuronDefinition.InputDefinitions.Count);
                    mutation.AddStepToPath(new() { action = ChangeInputDefinition, index = index });
                    mutation.invalid = neuronDefinition.InputDefinitions.Count == 0;
                    if (!mutation.invalid)
                        mutation = InputDefinitionMutation.CreateRandomMutation(emitterAvailabilityMap, neuronDefinition.InputDefinitions[index], mutation);
                }))
            };
            mutationChoices.ChooseAction();
            return mutation;
        }
    }

    public static class InputDefinitionMutation
    {
        public const string ChangeEmitterSetLocation = "ChangeEmitterSetLocation";
        public const string ChangeChildLimbIndex = "ChangeChildLimbIndex";
        public const string ChangeInstanceId = "ChangeInstanceId";
        public const string ChangeEmitterIndex = "ChangeEmitterIndex";
        public const string ChangeWeight = "ChangeWeight";

        public static Func<InputDefinition, InputDefinition> ParseMutationFunction(Queue<Step> pathTokens, string newValue)
        {
            Step step = pathTokens.Dequeue();
            return (inputDefinition) =>
            {
                return new(
                    step.action == ChangeEmitterSetLocation ? Enum.Parse<EmitterSetLocation>(newValue) : inputDefinition.EmitterSetLocation,
                    step.action == ChangeChildLimbIndex ? int.Parse(newValue) : inputDefinition.ChildLimbIndex,
                    step.action == ChangeInstanceId ? newValue : inputDefinition.InstanceId,
                    step.action == ChangeEmitterIndex ? int.Parse(newValue) : inputDefinition.EmitterIndex,
                    step.action == ChangeWeight ? float.Parse(newValue) : inputDefinition.Weight
                );
            };
        }

        public static MutationOperation CreateRandomMutation(EmitterAvailabilityMap emitterAvailabilityMap, InputDefinition inputDefinition, MutationOperation mutation)
        {
            WeightedActionList mutationChoices = new()
            {
                (MutationParameters.InputDefinition.ChangeEmitterSetLocation, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeEmitterSetLocation });
                    List<EmitterSetLocation> validLocations = emitterAvailabilityMap.GetValidInputSetLocations();
                    mutation.newValue = validLocations[UnityEngine.Random.Range(0, validLocations.Count)].ToString();
                })),
                (MutationParameters.InputDefinition.ChangeChildLimbIndex, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeChildLimbIndex });
                    List<int> validChildLimbIndices = emitterAvailabilityMap.GetValidChildLimbIndices();
                    mutation.invalid = validChildLimbIndices.Count == 0;
                    mutation.newValue = mutation.invalid ? null : validChildLimbIndices[UnityEngine.Random.Range(0, validChildLimbIndices.Count)].ToString();
                })),
                (MutationParameters.InputDefinition.ChangeInstanceId, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeInstanceId });
                    List<string> validInstanceIds = emitterAvailabilityMap.GetValidLimbInstanceIds();
                    mutation.invalid = validInstanceIds.Count == 0;
                    mutation.newValue = mutation.invalid ? null : validInstanceIds[UnityEngine.Random.Range(0, validInstanceIds.Count)].ToString();
                })),
                (MutationParameters.InputDefinition.ChangeEmitterIndex, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeEmitterIndex });
                    int emitterCount = emitterAvailabilityMap.GetEmitterCountAtLocation(inputDefinition.EmitterSetLocation, inputDefinition.ChildLimbIndex, inputDefinition.InstanceId);
                    mutation.invalid = emitterCount <= 0;
                    mutation.newValue = mutation.invalid ? null : UnityEngine.Random.Range(0, emitterCount).ToString();
                })),
                (MutationParameters.InputDefinition.ChangeWeight, new Action(() =>
                {
                    mutation.AddStepToPath(new() { action = ChangeWeight });
                    mutation.newValue = MutateScalar(inputDefinition.Weight, InputDefinitionParameters.MinWeight, InputDefinitionParameters.MaxWeight).ToString();
                }))
            };
            mutationChoices.ChooseAction();
            return mutation;
        }
    }

    private static object MutateEnum(Enum e)
    {
        Array enumOptions = Enum.GetValues(e.GetType());
        return enumOptions.GetValue(UnityEngine.Random.Range(0, enumOptions.Length));
    }

    private static Vector2 MutateVector(Vector2 vector, float min, float max)
    {
        int index = UnityEngine.Random.Range(0, 2);
        vector[index] = MutateScalar(vector[index], min, max);
        return vector;
    }

    private static Vector3 MutateVector(Vector3 vector, float min, float max)
    {
        int index = UnityEngine.Random.Range(0, 3);
        vector[index] = MutateScalar(vector[index], min, max);
        return vector;
    }

    private static float MutateScalar(float scalar, float min, float max)
    {
        return Mathf.Clamp(scalar + (scalar * Utilities.RandomGaussian()), min, max);
    }

    private static int MutateScalar(int scalar, int min, int max)
    {
        return Mathf.Clamp(scalar + Mathf.RoundToInt(scalar * Utilities.RandomGaussian()), min, max);
    }
}
