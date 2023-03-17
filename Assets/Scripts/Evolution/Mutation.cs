using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public static class Mutation
{
    private class WeightedActionList : List<(float, System.Action)>
    {
        public void ChooseAction()
        {
            float choiceIndex = Random.value * this.Sum(x => x.Item1);
            float currentPosition = 0f;
            foreach ((float weight, System.Action action) in this)
            {
                if (choiceIndex >= currentPosition && choiceIndex <= currentPosition + weight)
                {
                    action();
                    break;
                }
                currentPosition += weight;
            }
        }
    }

    public static Genotype Mutate(Genotype genotype)
    {
        int numberOfMutations = Mathf.Max(0, Mathf.RoundToInt(MutationParameters.MutationRate + (Utilities.RandomGaussian() * MutationParameters.MutationRate)));
        Genotype newGenotype = genotype;
        for (int i = 0; i < numberOfMutations; i++)
        {
            ReadOnlyCollection<LimbNode> limbNodes = newGenotype.limbNodes;
            ReadOnlyCollection<NeuronDefinition> brainNeuronDefinitions = newGenotype.brainNeuronDefinitions;
            List<string> path = new();
            WeightedActionList mutationChoices = new WeightedActionList
            {
                (MutationParameters.Root.ChangeLimbNodes, new System.Action(() => limbNodes = MutateLimbNodes(path, limbNodes))),
                (MutationParameters.Root.ChangeBrainNeuronDefinitions, new System.Action(() =>
                    brainNeuronDefinitions = MutateItemInCollection(path, brainNeuronDefinitions, MutateNeuronDefinition))),
            };

            mutationChoices.ChooseAction();

            newGenotype = new(
                genotype.id,
                newGenotype.lineage.Concat(new List<string>() { "M - " + string.Join(" ", path) }).ToList().AsReadOnly(),
                brainNeuronDefinitions,
                limbNodes
            );
        }

        return newGenotype;
    }

    private static ReadOnlyCollection<LimbNode> MutateLimbNodes(List<string> path, ReadOnlyCollection<LimbNode> limbNodes)
    {
        List<LimbNode> newLimbNodes = limbNodes.ToList();

        if (limbNodes.Count < GenotypeParameters.MaxLimbNodes)
        {
            // If we can, always add a new node. It will be garbage collected unless an existing node mutates a connection to it.
            newLimbNodes.Add(LimbNode.CreateRandom(null));
        }

        int index = Random.Range(0, limbNodes.Count);
        newLimbNodes[index] = MutateLimbNode(path, limbNodes[index], newLimbNodes.Count);

        return newLimbNodes.AsReadOnly();
    }

    private static LimbNode MutateLimbNode(List<string> path, LimbNode limbNode, int numLimbNodes)
    {
        path.Add("LimbNode");

        Vector3 dimensions = limbNode.dimensions;
        JointDefinition jointDefinition = limbNode.jointDefinition;
        int recursiveLimit = limbNode.recursiveLimit;

        JointType jointType = limbNode.jointDefinition.type;
        ReadOnlyCollection<JointAxisDefinition> jointAxisDefinitions = limbNode.jointDefinition.axisDefinitions;

        ReadOnlyCollection<NeuronDefinition> neuronDefinitions = limbNode.neuronDefinitions;
        ReadOnlyCollection<LimbConnection> connections = limbNode.connections;

        WeightedActionList mutationChoices = new WeightedActionList
        {
            (MutationParameters.LimbNode.ChangeLimbDimensions, new System.Action(() => {
                dimensions = MutateVector(dimensions, LimbNodeParameters.MinSize, LimbNodeParameters.MaxSize);
                path.Add("Dimensions");
            })),
            (MutationParameters.LimbNode.ChangeJointType, new System.Action(() => {
                jointType = (JointType)MutateEnum(jointType);
                path.Add("JointType");
            })),
            (MutationParameters.LimbNode.ChangeJointAxisDefinition, new System.Action(() =>
                jointAxisDefinitions = MutateItemInCollection(path, jointAxisDefinitions, MutateJointAxisDefinition)
            )),
            (MutationParameters.LimbNode.ChangeRecursiveLimit, new System.Action(() => {
                recursiveLimit = MutateScalar(recursiveLimit, 0, LimbNodeParameters.MaxRecursiveLimit);
                path.Add("RecursiveLimit");
            })),
            (MutationParameters.LimbNode.AddLimbConnection, new System.Action(() =>
            {
                List<LimbConnection> c = connections.ToList();
                c.Add(LimbConnection.CreateRandom(Random.Range(0, numLimbNodes)));
                connections = c.AsReadOnly();
                path.Add("AddLimbConnection");
            })),
            (MutationParameters.LimbNode.RemoveLimbConnection, new System.Action(() =>
            {
                path.Add("RemoveLimbConnection");
                if (connections.Count > 1)
                {
                    List<LimbConnection> c = connections.ToList();
                    c.RemoveAt(Random.Range(0, c.Count));
                    connections = c.AsReadOnly();
                }
                else
                    path.Add("[FAILED]");
            })),
            (MutationParameters.LimbNode.ChangeNeuronDefinition, new System.Action(() =>
                neuronDefinitions = MutateItemInCollection(path, neuronDefinitions, MutateNeuronDefinition))),
            (MutationParameters.LimbNode.ChangeLimbConnection, new System.Action(() =>
                connections = MutateItemInCollection(path, connections, (List<string> path, LimbConnection c) => MutateLimbConnection(path, c, numLimbNodes))))
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

    private static JointAxisDefinition MutateJointAxisDefinition(List<string> path, JointAxisDefinition jointAxisDefinition)
    {
        path.Add("JointAxisDefinition");

        float limit = jointAxisDefinition.limit;
        SignalReceiverInputDefinition inputDefinition = jointAxisDefinition.inputDefinition;
        WeightedActionList mutationChoices = new WeightedActionList
        {
            (MutationParameters.JointAxisDefinition.ChangeJointLimit, new System.Action(() => {
                limit = MutateScalar(limit, JointDefinitionParameters.MinAngle, JointDefinitionParameters.MaxAngle);
                path.Add("Limit");
        })),
            (MutationParameters.JointAxisDefinition.ChangeSignalReceiverInputDefinition, new System.Action(() =>
                inputDefinition = MutateSignalReceiverInputDefinition(path, inputDefinition)))
        };

        mutationChoices.ChooseAction();

        return new(
            limit,
            inputDefinition
        );
    }

    private static NeuronDefinition MutateNeuronDefinition(List<string> path, NeuronDefinition neuronDefinition)
    {
        path.Add("NeuronDefinition");

        NeuronType type = neuronDefinition.type;
        ReadOnlyCollection<SignalReceiverInputDefinition> inputDefinitions = neuronDefinition.inputDefinitions;
        WeightedActionList mutationChoices = new WeightedActionList
        {
            (MutationParameters.NeuronDefinition.ChangeNeuronType, new System.Action(() => {
                type = (NeuronType)MutateEnum(type);
                path.Add("NeuronType");
            })),
            (MutationParameters.NeuronDefinition.ChangeSignalReceiverInputDefinition, new System.Action(() =>
                inputDefinitions = MutateItemInCollection(path, inputDefinitions, MutateSignalReceiverInputDefinition)))
        };

        mutationChoices.ChooseAction();

        return new(
            type,
            inputDefinitions
        );
    }

    private static SignalReceiverInputDefinition MutateSignalReceiverInputDefinition(List<string> path, SignalReceiverInputDefinition signalReceiverInputDefinition)
    {
        path.Add("SignalReceiverInputDefinition");

        float preference = signalReceiverInputDefinition.preference;
        float weight = signalReceiverInputDefinition.weight;
        WeightedActionList mutationChoices = new WeightedActionList
        {
            (MutationParameters.SignalReceiverInputDefinition.ChangeSignalReceiverInputPreference, new System.Action(() => {
                preference = MutateScalar(preference, 0f, 1f);
                path.Add("Preference");
            })),
            (MutationParameters.SignalReceiverInputDefinition.ChangeSignalReceiverInputWeight, new System.Action(() => {
                weight = MutateScalar(weight, NeuronDefinitionParameters.MinWeight, NeuronDefinitionParameters.MaxWeight);
                path.Add("Weight");
            }))
        };

        mutationChoices.ChooseAction();

        return new(
            preference,
            weight
        );
    }

    private static LimbConnection MutateLimbConnection(List<string> path, LimbConnection limbConnection, int numLimbNodes)
    {
        path.Add("LimbConnection");

        int childNodeId = limbConnection.childNodeId;
        int parentFace = limbConnection.parentFace;
        Vector2 position = limbConnection.position;
        Vector3 orientation = limbConnection.orientation;
        Vector3 scale = limbConnection.scale;
        bool reflectionX = limbConnection.reflectionX;
        bool reflectionY = limbConnection.reflectionY;
        bool reflectionZ = limbConnection.reflectionZ;
        bool terminalOnly = limbConnection.terminalOnly;
        WeightedActionList mutationChoices = new WeightedActionList
        {
            (MutationParameters.LimbConnection.ChangeChildNode, new System.Action(() =>  {
                childNodeId = MutateScalar(childNodeId, 0, numLimbNodes - 1);
                path.Add("ChildNodeID");
            })),
            (MutationParameters.LimbConnection.ChangeParentFace, new System.Action(() =>  {
                parentFace = MutateScalar(parentFace, 0, 5);
                path.Add("ParentFace");
            })),
            (MutationParameters.LimbConnection.ChangePosition, new System.Action(() =>  {
                position = MutateVector(position, -1f, 1f);
                path.Add("Position");
            })),
            (MutationParameters.LimbConnection.ChangeOrientation, new System.Action(() => {
                orientation = MutateVector(orientation, LimbConnectionParameters.MinAngle, LimbConnectionParameters.MaxAngle);
                path.Add("Orientation");
            })),
            (MutationParameters.LimbConnection.ChangeScale, new System.Action(() => {
                scale = MutateVector(scale, LimbConnectionParameters.MinScale, LimbConnectionParameters.MaxScale);
                path.Add("Scale");
            })),
            (MutationParameters.LimbConnection.ChangeReflectionX, new System.Action(() =>  {
                reflectionX = !reflectionX;
                path.Add("ReflectionX");
            })),
            (MutationParameters.LimbConnection.ChangeReflectionY, new System.Action(() =>  {
                reflectionY = !reflectionY;
                path.Add("ReflectionY");
            })),
            (MutationParameters.LimbConnection.ChangeReflectionZ, new System.Action(() =>  {
                reflectionZ = !reflectionZ;
                path.Add("ReflectionZ");
            })),
            (MutationParameters.LimbConnection.ChangeTerminalOnly, new System.Action(() =>  {
                terminalOnly = !terminalOnly;
                path.Add("TerminalOnly");
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

    private static ReadOnlyCollection<T> MutateItemInCollection<T>(List<string> path, ReadOnlyCollection<T> collection, System.Func<List<string>, T, T> mutationFunction)
    {
        if (collection.Count > 0)
        {
            int index = Random.Range(0, collection.Count);
            List<T> c = collection.ToList();
            c[index] = mutationFunction(path, c[index]);
            return c.AsReadOnly();
        }
        else
        {
            path.Add("[FAILED]");
            return collection;
        }
    }

    private static object MutateEnum(System.Enum e)
    {
        System.Array enumOptions = System.Enum.GetValues(e.GetType());
        return enumOptions.GetValue(UnityEngine.Random.Range(0, enumOptions.Length));
    }

    private static Vector2 MutateVector(Vector2 vector, float min, float max)
    {
        int index = Random.Range(0, 2);
        vector[index] = MutateScalar(vector[index], min, max);
        return vector;
    }

    private static Vector3 MutateVector(Vector3 vector, float min, float max)
    {
        int index = Random.Range(0, 3);
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

    private static bool Chance(float probability)
    {
        return Random.value <= probability;
    }
}
