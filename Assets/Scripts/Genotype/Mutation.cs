using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class Mutation
{
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

    public static Genotype AddPotentialLimbNode(Genotype genotype)
    {
        List<LimbNode> limbNodes = genotype.LimbNodes.ToList();

        bool invalid = genotype.LimbNodes.Count == GenotypeParameters.MaxLimbNodes;
        if (!invalid)
        {
            UnfinishedLimbNode unfinishedLimbNode = UnfinishedLimbNode.CreateRandom(null);
            List<ILimbNodeEssentialInfo> tempLimbNodes = genotype.LimbNodes.Cast<ILimbNodeEssentialInfo>().ToList();
            tempLimbNodes.Add(unfinishedLimbNode);
            LimbNode newLimbNode = LimbNode.CreateRandom(
                EmitterAvailabilityMap.GenerateMapForLimbNode(genotype.BrainNeuronDefinitions.Count, tempLimbNodes, tempLimbNodes.Count - 1),
                unfinishedLimbNode
            );
            limbNodes.Add(newLimbNode);
        }
        return Genotype.Construct(
            genotype.Id,
            genotype.BrainNeuronDefinitions,
            limbNodes
        );
    }

    public static Genotype Mutate(Genotype genotype)
    {
        List<NeuronDefinition> brainNeuronDefinitions = genotype.BrainNeuronDefinitions.ToList();
        List<LimbNode> limbNodes = genotype.LimbNodes.ToList();

        WeightedActionList mutationChoices = new()
        {
            (MutationParameters.Root.ChangeBrain, new Action(() =>
            {
                brainNeuronDefinitions = MutateBrain(genotype);
            })),
            (MutationParameters.Root.ChangeLimbNode, new Action(() =>
            {
                int index = UnityEngine.Random.Range(0, genotype.LimbNodes.Count);
                bool invalid = genotype.LimbNodes.Count == 0;
                if (!invalid)
                    limbNodes.Select((l, i) => i == index ? MutateLimbNode(genotype, index) : l).ToList();
            }))
        };

        mutationChoices.ChooseAction();
        return Genotype.Construct(
            genotype.Id,
            brainNeuronDefinitions,
            limbNodes
        );
    }

    private static List<NeuronDefinition> MutateBrain(Genotype genotype)
    {
        List<NeuronDefinition> brainNeuronDefinitions = genotype.BrainNeuronDefinitions.ToList();

        WeightedActionList mutationChoices = new()
        {
            (MutationParameters.Brain.AddNeuron, new Action(() =>
            {
                bool invalid = genotype.BrainNeuronDefinitions.Count == GenotypeParameters.MaxBrainNeurons;
                if (!invalid)
                    brainNeuronDefinitions.Add(NeuronDefinition.CreateRandom(
                        EmitterAvailabilityMap.GenerateMapForBrain(genotype.BrainNeuronDefinitions.Count, genotype.InstancedLimbNodes)
                    ));
            })),
            (MutationParameters.Brain.RemoveNeuron, new Action(() =>
            {
                int index = UnityEngine.Random.Range(0, genotype.BrainNeuronDefinitions.Count);
                bool invalid = genotype.BrainNeuronDefinitions.Count == 0;
                if (!invalid)
                    brainNeuronDefinitions = brainNeuronDefinitions.Where((n, i) => i != index).ToList();
            })),
            (MutationParameters.Brain.ChangeNeuronDefinition, new Action(() =>
            {
                int index = UnityEngine.Random.Range(0, genotype.BrainNeuronDefinitions.Count);
                bool invalid = genotype.BrainNeuronDefinitions.Count == 0;
                if (!invalid)
                {
                    EmitterAvailabilityMap map = EmitterAvailabilityMap.GenerateMapForBrain(genotype.BrainNeuronDefinitions.Count, genotype.InstancedLimbNodes);
                    brainNeuronDefinitions = brainNeuronDefinitions.Select((n, i) => i == index ? MutateNeuronDefinition(map, genotype.BrainNeuronDefinitions[index]) : n).ToList();
                }
            }))
        };

        mutationChoices.ChooseAction();
        return brainNeuronDefinitions;
    }

    private static LimbNode MutateLimbNode(Genotype genotype, int limbNodeIndex)
    {
        LimbNode limbNode = genotype.LimbNodes[limbNodeIndex];
        Vector3 dimensions = limbNode.Dimensions;
        JointDefinition jointDefinition = limbNode.JointDefinition;
        int recursiveLimit = limbNode.RecursiveLimit;
        List<NeuronDefinition> neuronDefinitions = limbNode.NeuronDefinitions.ToList();
        List<LimbConnection> connections = limbNode.Connections.ToList();

        WeightedActionList mutationChoices = new()
        {
            (MutationParameters.LimbNode.ChangeDimensions, new Action(() =>
            {
                dimensions = MutateVector(limbNode.Dimensions, LimbNodeParameters.MinSize, LimbNodeParameters.MaxSize);
            })),
            (MutationParameters.LimbNode.ChangeJointDefinition, new Action(() =>
            {
                EmitterAvailabilityMap map = EmitterAvailabilityMap.GenerateMapForLimbNode(genotype.BrainNeuronDefinitions.Count, genotype.LimbNodes.Cast<ILimbNodeEssentialInfo>().ToList(), limbNodeIndex);
                jointDefinition = MutateJointDefinition(map, limbNode.JointDefinition);
            })),
            (MutationParameters.LimbNode.ChangeRecursiveLimit, new Action(() =>
            {
                recursiveLimit = MutateScalar(limbNode.RecursiveLimit, 0, LimbNodeParameters.MaxRecursiveLimit);
            })),
            (MutationParameters.LimbNode.AddNeuron, new Action(() =>
            {
                bool invalid = limbNode.NeuronDefinitions.Count == LimbNodeParameters.MaxNeurons;
                if (!invalid)
                    neuronDefinitions.Add(NeuronDefinition.CreateRandom(
                        EmitterAvailabilityMap.GenerateMapForLimbNode(genotype.BrainNeuronDefinitions.Count, genotype.LimbNodes.Cast<ILimbNodeEssentialInfo>().ToList(), limbNodeIndex)
                    ));
            })),
            (MutationParameters.LimbNode.RemoveNeuron, new Action(() =>
            {
                int index = UnityEngine.Random.Range(0, limbNode.NeuronDefinitions.Count);
                bool invalid = limbNode.NeuronDefinitions.Count == 0;
                if (!invalid)
                    neuronDefinitions = neuronDefinitions.Where((n, i) => i != index).ToList();
            })),
            (MutationParameters.LimbNode.ChangeNeuronDefinition, new Action(() =>
            {
                int index = UnityEngine.Random.Range(0, limbNode.NeuronDefinitions.Count);
                bool invalid = limbNode.NeuronDefinitions.Count == 0;
                if (!invalid)
                {
                    EmitterAvailabilityMap map = EmitterAvailabilityMap.GenerateMapForLimbNode(genotype.BrainNeuronDefinitions.Count, genotype.LimbNodes.Cast<ILimbNodeEssentialInfo>().ToList(), limbNodeIndex);
                    neuronDefinitions = neuronDefinitions.Select((n, i) => i == index ? MutateNeuronDefinition(map, limbNode.NeuronDefinitions[index]) : n).ToList();
                }
            })),
            (MutationParameters.LimbNode.AddLimbConnection, new Action(() =>
            {
                bool invalid = limbNode.Connections.Count == LimbNodeParameters.MaxLimbConnections;
                if (!invalid)
                    connections.Add(LimbConnection.CreateRandom(UnityEngine.Random.Range(0, genotype.LimbNodes.Count)));
            })),
            (MutationParameters.LimbNode.RemoveLimbConnection, new Action(() =>
            {
                int index = UnityEngine.Random.Range(0, limbNode.Connections.Count);
                bool invalid = limbNode.Connections.Count == 0;
                if (!invalid)
                    connections = connections.Where((c, i) => i != index).ToList();
            })),
            (MutationParameters.LimbNode.ChangeLimbConnection, new Action(() =>
            {
                int index = UnityEngine.Random.Range(0, limbNode.Connections.Count);
                bool invalid = limbNode.Connections.Count == 0;
                if (!invalid)
                    connections = connections.Select((c, i) => MutateLimbConnection(genotype, limbNodeIndex, index)).ToList();
            }))
        };

        mutationChoices.ChooseAction();
        return new(
            dimensions,
            jointDefinition,
            recursiveLimit,
            neuronDefinitions,
            connections
        );
    }

    private static JointDefinition MutateJointDefinition(EmitterAvailabilityMap emitterAvailabilityMap, JointDefinition jointDefinition)
    {
        JointType type = jointDefinition.Type;
        List<JointAxisDefinition> axisDefinitions = jointDefinition.AxisDefinitions.ToList();

        WeightedActionList mutationChoices = new()
        {
            (MutationParameters.JointDefinition.ChangeJointType, new Action(() =>
            {
                type = Enum.Parse<JointType>(MutateEnum(jointDefinition.Type).ToString());
            })),
            (MutationParameters.JointDefinition.ChangeJointAxisDefinition, new Action(() =>
            {
                int index = UnityEngine.Random.Range(0, jointDefinition.AxisDefinitions.Count);
                bool invalid = jointDefinition.AxisDefinitions.Count == 0;
                if (!invalid)
                    axisDefinitions[index] = MutateJointAxisDefinition(emitterAvailabilityMap, jointDefinition.AxisDefinitions[index]);
            }))
        };

        mutationChoices.ChooseAction();
        return new(
            type,
            axisDefinitions
        );
    }

    private static JointAxisDefinition MutateJointAxisDefinition(EmitterAvailabilityMap emitterAvailabilityMap, JointAxisDefinition jointAxisDefinition)
    {
        float limit = jointAxisDefinition.Limit;
        InputDefinition inputDefinition = jointAxisDefinition.InputDefinition;

        WeightedActionList mutationChoices = new()
        {
            (MutationParameters.JointAxisDefinition.ChangeJointLimit, new Action(() =>
            {
                limit = MutateScalar(jointAxisDefinition.Limit, JointDefinitionParameters.MinAngle, JointDefinitionParameters.MaxAngle);
            })),
            (MutationParameters.JointAxisDefinition.ChangeInputDefinition, new Action(() =>
            {
                inputDefinition = MutateInputDefinition(emitterAvailabilityMap, jointAxisDefinition.InputDefinition);
            }))
        };

        mutationChoices.ChooseAction();
        return new(
            limit,
            inputDefinition
        );
    }

    private static LimbConnection MutateLimbConnection(Genotype genotype, int limbNodeIndex, int limbConnectionIndex)
    {
        LimbConnection connection = genotype.LimbNodes[limbNodeIndex].Connections[limbConnectionIndex];
        int childNodeId = connection.ChildNodeId;
        int parentFace = connection.ParentFace;
        Vector2 position = connection.Position;
        Vector3 orientation = connection.Orientation;
        Vector3 scale = connection.Scale;
        bool reflectionX = connection.ReflectionX;
        bool reflectionY = connection.ReflectionY;
        bool reflectionZ = connection.ReflectionZ;
        bool terminalOnly = connection.TerminalOnly;

        WeightedActionList mutationChoices = new()
        {
            (MutationParameters.LimbConnection.ChangeChildNode, new Action(() =>
            {
                childNodeId = MutateScalar(connection.ChildNodeId, 0, genotype.LimbNodes.Count - 1);
            })),
            (MutationParameters.LimbConnection.ChangeParentFace, new Action(() =>
            {
                parentFace = MutateScalar(connection.ParentFace, 0, 5);
            })),
            (MutationParameters.LimbConnection.ChangePosition, new Action(() =>
            {
                position = MutateVector(connection.Position, -1f, 1f);
            })),
            (MutationParameters.LimbConnection.ChangeOrientation, new Action(() =>
            {
                orientation = MutateVector(connection.Orientation, LimbConnectionParameters.MinAngle, LimbConnectionParameters.MaxAngle);
            })),
            (MutationParameters.LimbConnection.ChangeScale, new Action(() =>
            {
                scale = MutateVector(connection.Scale, LimbConnectionParameters.MinScale, LimbConnectionParameters.MaxScale);
            })),
            (MutationParameters.LimbConnection.ChangeReflectionX, new Action(() =>
            {
                reflectionX = !connection.ReflectionX;
            })),
            (MutationParameters.LimbConnection.ChangeReflectionY, new Action(() =>
            {
                reflectionY = !connection.ReflectionY;
            })),
            (MutationParameters.LimbConnection.ChangeReflectionZ, new Action(() =>
            {
                reflectionZ = !connection.ReflectionZ;
            })),
            (MutationParameters.LimbConnection.ChangeTerminalOnly, new Action(() =>
            {
                terminalOnly = !connection.TerminalOnly;
            }))
        };

        mutationChoices.ChooseAction();
        return new(
            childNodeId,
            parentFace,
            position,
            orientation,
            scale,
            reflectionX,
            reflectionY,
            reflectionZ,
            terminalOnly
        );
    }

    private static NeuronDefinition MutateNeuronDefinition(EmitterAvailabilityMap emitterAvailabilityMap, NeuronDefinition neuronDefinition)
    {
        NeuronType type = neuronDefinition.Type;
        List<InputDefinition> inputDefinitions = neuronDefinition.InputDefinitions.ToList();

        WeightedActionList mutationChoices = new()
        {
            (MutationParameters.NeuronDefinition.ChangeNeuronType, new Action(() =>
            {
                type = Enum.Parse<NeuronType>(MutateEnum(neuronDefinition.Type).ToString());
            })),
            (MutationParameters.NeuronDefinition.ChangeInputDefinition, new Action(() =>
            {
                int index = UnityEngine.Random.Range(0, neuronDefinition.InputDefinitions.Count);
                bool invalid = neuronDefinition.InputDefinitions.Count == 0;
                if (!invalid)
                    inputDefinitions[index] = MutateInputDefinition(emitterAvailabilityMap, inputDefinitions[index]);
            }))
        };

        mutationChoices.ChooseAction();
        return new(
            type,
            inputDefinitions
        );
    }

    private static InputDefinition MutateInputDefinition(EmitterAvailabilityMap emitterAvailabilityMap, InputDefinition inputDefinition)
    {
        EmitterSetLocation emitterSetLocation = inputDefinition.EmitterSetLocation;
        int childLimbIndex = inputDefinition.ChildLimbIndex;
        string instanceId = inputDefinition.InstanceId;
        int emitterIndex = inputDefinition.EmitterIndex;
        float weight = inputDefinition.Weight;

        WeightedActionList mutationChoices = new()
        {
            (MutationParameters.InputDefinition.ChangeEmitterSetLocation, new Action(() =>
            {
                List<EmitterSetLocation> validLocations = emitterAvailabilityMap.GetValidInputSetLocations();
                emitterSetLocation = validLocations[UnityEngine.Random.Range(0, validLocations.Count)];
            })),
            (MutationParameters.InputDefinition.ChangeChildLimbIndex, new Action(() =>
            {
                List<int> validChildLimbIndices = emitterAvailabilityMap.GetValidChildLimbIndices();
                bool invalid = validChildLimbIndices.Count == 0;
                childLimbIndex = invalid ? 0 : validChildLimbIndices[UnityEngine.Random.Range(0, validChildLimbIndices.Count)];
            })),
            (MutationParameters.InputDefinition.ChangeInstanceId, new Action(() =>
            {
                List<string> validInstanceIds = emitterAvailabilityMap.GetValidLimbInstanceIds();
                bool invalid = validInstanceIds.Count == 0;
                instanceId = invalid ? null : validInstanceIds[UnityEngine.Random.Range(0, validInstanceIds.Count)];
            })),
            (MutationParameters.InputDefinition.ChangeEmitterIndex, new Action(() =>
            {
                int emitterCount = emitterAvailabilityMap.GetEmitterCountAtLocation(inputDefinition.EmitterSetLocation, inputDefinition.ChildLimbIndex, inputDefinition.InstanceId);
                bool invalid = emitterCount <= 0;
                emitterIndex = invalid ? 0 : UnityEngine.Random.Range(0, emitterCount);
            })),
            (MutationParameters.InputDefinition.ChangeWeight, new Action(() =>
            {
                weight = MutateScalar(inputDefinition.Weight, InputDefinitionParameters.MinWeight, InputDefinitionParameters.MaxWeight);
            }))
        };

        mutationChoices.ChooseAction();
        return new(
            emitterSetLocation,
            childLimbIndex,
            instanceId,
            emitterIndex,
            weight
        );
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
