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
            ReadOnlyCollection<LimbNode> limbNodes = newGenotype.LimbNodes;
            ReadOnlyCollection<NeuronDefinition> brainNeuronDefinitions = newGenotype.BrainNeuronDefinitions;
            List<string> path = new();
            WeightedActionList mutationChoices = new WeightedActionList
            {
                (MutationParameters.Root.ChangeLimbNodes, new System.Action(() => limbNodes = MutateLimbNodes(path, limbNodes, brainNeuronDefinitions))),
                (MutationParameters.Root.ChangeBrainNeuronDefinitions, new System.Action(() =>
                    brainNeuronDefinitions = MutateItemInCollection(path, brainNeuronDefinitions,
                        (List<string> path, NeuronDefinition neuronDefinition) =>
                            MutateNeuronDefinition(
                                path,
                                EmitterAvailabilityMap.GenerateMapForBrain(
                                    brainNeuronDefinitions.Count,
                                    newGenotype.InstancedLimbNodes
                                ),
                                neuronDefinition
                        )))
                ),
            };

            mutationChoices.ChooseAction();

            newGenotype = Genotype.Construct(
                genotype.Id,
                // newGenotype.Lineage.Concat(new List<string>() { "M - " + string.Join(" ", path) }).ToList().AsReadOnly(),
                genotype.Lineage,
                brainNeuronDefinitions,
                limbNodes
            );
        }

        return newGenotype;
    }

    private static ReadOnlyCollection<LimbNode> MutateLimbNodes(List<string> path, ReadOnlyCollection<LimbNode> limbNodes,
        ReadOnlyCollection<NeuronDefinition> brainNeuronDefinitions)
    {
        List<LimbNode> newLimbNodes = limbNodes.ToList();

        if (limbNodes.Count < GenotypeParameters.MaxLimbNodes)
        {
            // If we can, always add a new node. It will be garbage collected unless an existing node mutates a connection to it.
            UnfinishedLimbNode unfinishedLimbNode = UnfinishedLimbNode.CreateRandom(null);
            List<ILimbNodeEssentialInfo> tempLimbNodes = limbNodes.Cast<ILimbNodeEssentialInfo>().ToList();
            tempLimbNodes.Add(unfinishedLimbNode);
            newLimbNodes.Add(LimbNode.CreateRandom(
                EmitterAvailabilityMap.GenerateMapForLimbNode(brainNeuronDefinitions.Count, tempLimbNodes, tempLimbNodes.Count - 1),
                unfinishedLimbNode
            ));
        }

        int nodeId = Random.Range(0, limbNodes.Count); // Don't include new node in selection.
        newLimbNodes[nodeId] = MutateLimbNode(
            path,
            EmitterAvailabilityMap.GenerateMapForLimbNode(brainNeuronDefinitions.Count, newLimbNodes.Cast<ILimbNodeEssentialInfo>().ToList(), nodeId),
            limbNodes[nodeId],
            newLimbNodes.Count
        );

        return newLimbNodes.AsReadOnly();
    }

    private static LimbNode MutateLimbNode(List<string> path, EmitterAvailabilityMap emitterAvailabilityMap, LimbNode limbNode, int numLimbNodes)
    {
        path.Add("LimbNode");

        Vector3 dimensions = limbNode.Dimensions;
        JointDefinition jointDefinition = limbNode.JointDefinition;
        int recursiveLimit = limbNode.RecursiveLimit;

        JointType jointType = limbNode.JointDefinition.Type;
        ReadOnlyCollection<JointAxisDefinition> jointAxisDefinitions = limbNode.JointDefinition.AxisDefinitions;

        ReadOnlyCollection<NeuronDefinition> neuronDefinitions = limbNode.NeuronDefinitions;
        ReadOnlyCollection<LimbConnection> connections = limbNode.Connections;

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
                jointAxisDefinitions = MutateItemInCollection(path, jointAxisDefinitions,
                    (List<string> path, JointAxisDefinition j) => MutateJointAxisDefinition(path, emitterAvailabilityMap, j))
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
                neuronDefinitions = MutateItemInCollection(path, neuronDefinitions,
                    (List<string> path, NeuronDefinition n) => MutateNeuronDefinition(path, emitterAvailabilityMap, n))
            )),
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

    private static JointAxisDefinition MutateJointAxisDefinition(List<string> path, EmitterAvailabilityMap emitterAvailabilityMap, JointAxisDefinition jointAxisDefinition)
    {
        path.Add("JointAxisDefinition");

        float limit = jointAxisDefinition.Limit;
        InputDefinition inputDefinition = jointAxisDefinition.InputDefinition;
        WeightedActionList mutationChoices = new WeightedActionList
        {
            (MutationParameters.JointAxisDefinition.ChangeJointLimit, new System.Action(() => {
                limit = MutateScalar(limit, JointDefinitionParameters.MinAngle, JointDefinitionParameters.MaxAngle);
                path.Add("Limit");
            })),
            (MutationParameters.JointAxisDefinition.ChangeInputDefinition, new System.Action(() =>
                inputDefinition = MutateInputDefinition(path, emitterAvailabilityMap, inputDefinition)
            ))
        };

        mutationChoices.ChooseAction();

        return new(
            limit,
            inputDefinition
        );
    }

    private static NeuronDefinition MutateNeuronDefinition(List<string> path, EmitterAvailabilityMap emitterAvailabilityMap, NeuronDefinition neuronDefinition)
    {
        path.Add("NeuronDefinition");

        NeuronType type = neuronDefinition.Type;
        ReadOnlyCollection<InputDefinition> inputDefinitions = neuronDefinition.InputDefinitions;
        WeightedActionList mutationChoices = new WeightedActionList
        {
            (MutationParameters.NeuronDefinition.ChangeNeuronType, new System.Action(() => {
                type = (NeuronType)MutateEnum(type);
                path.Add("NeuronType");
            })),
            (MutationParameters.NeuronDefinition.ChangeInputDefinition, new System.Action(() =>
                inputDefinitions = MutateItemInCollection(path, inputDefinitions,
                    (List<string> path, InputDefinition inputDefinition) => MutateInputDefinition(path, emitterAvailabilityMap, inputDefinition)
            )))
        };

        mutationChoices.ChooseAction();

        return new(
            type,
            inputDefinitions
        );
    }

    private static InputDefinition MutateInputDefinition(List<string> path, EmitterAvailabilityMap emitterAvailabilityMap, InputDefinition inputDefinition)
    {
        path.Add("InputDefinition");

        EmitterSetLocation emitterSetLocation = inputDefinition.EmitterSetLocation;
        int childLimbIndex = inputDefinition.ChildLimbIndex;
        string instanceId = inputDefinition.InstanceId;
        int emitterIndex = inputDefinition.EmitterIndex;
        float weight = inputDefinition.Weight;
        WeightedActionList mutationChoices = new WeightedActionList
        {
            (MutationParameters.InputDefinition.ChangeInputDefinitionInputSetLocation, new System.Action(() => {
                List<EmitterSetLocation> validLocations = emitterAvailabilityMap.GetValidInputSetLocations();
                emitterSetLocation = validLocations[Random.Range(0, validLocations.Count)];

                if (emitterSetLocation == EmitterSetLocation.ChildLimbs)
                {
                    List<int> validChildLimbIndices = emitterAvailabilityMap.GetValidChildLimbIndices();
                    if (!validChildLimbIndices.Contains(childLimbIndex))
                        childLimbIndex = validChildLimbIndices[Random.Range(0, validChildLimbIndices.Count)];
                }
                else
                    childLimbIndex = -1;

                if (emitterSetLocation == EmitterSetLocation.LimbInstances)
                {
                    List<string> validLimbInstanceIds = emitterAvailabilityMap.GetValidLimbInstanceIds();
                    if (string.IsNullOrEmpty(instanceId) || !validLimbInstanceIds.Contains(instanceId))
                        instanceId = validLimbInstanceIds[Random.Range(0, validLimbInstanceIds.Count)];
                }
                else
                    instanceId = null;

                if (emitterSetLocation == EmitterSetLocation.None)
                    emitterIndex = -1;
                else
                {
                    int emitterCountAtLocation = emitterAvailabilityMap.GetInputCountAtLocation(emitterSetLocation, childLimbIndex, instanceId);
                    if (emitterIndex < 0 || emitterIndex >= emitterCountAtLocation)
                        emitterIndex = Random.Range(0, emitterCountAtLocation);
                }

                path.Add("InputSetLocation");
            })),
            (MutationParameters.InputDefinition.ChangeInputDefinitionChildLimbIndex, new System.Action(() => {
                path.Add("ChildLimbIndex");
                if (emitterSetLocation != EmitterSetLocation.ChildLimbs)
                    path.Add("[FAILED]");
                else
                {
                    List<int> validChildLimbIndices = emitterAvailabilityMap.GetValidChildLimbIndices();
                    if (validChildLimbIndices.Count == 0)
                        path.Add("[FAILED]");
                    else
                        childLimbIndex = validChildLimbIndices[Random.Range(0, validChildLimbIndices.Count)];
                }
            })),
            (MutationParameters.InputDefinition.ChangeInputDefinitionInstanceId, new System.Action(() => {
                path.Add("InstanceId");
                if (emitterSetLocation != EmitterSetLocation.LimbInstances)
                    path.Add("[FAILED]");
                else
                {
                    List<string> validInstanceIds = emitterAvailabilityMap.GetValidLimbInstanceIds();
                    if (validInstanceIds.Count == 0)
                        path.Add("[FAILED]");
                    else
                        instanceId = validInstanceIds[Random.Range(0, validInstanceIds.Count)];
                }
            })),
            (MutationParameters.InputDefinition.ChangeInputDefinitionEmitterIndex, new System.Action(() => {
                path.Add("EmitterIndex");
                if (emitterSetLocation == EmitterSetLocation.None)
                    path.Add("[FAILED]");
                else
                    emitterIndex = Random.Range(0, emitterAvailabilityMap.GetInputCountAtLocation(emitterSetLocation, childLimbIndex, instanceId));
            })),
            (MutationParameters.InputDefinition.ChangeInputDefinitionWeight, new System.Action(() => {
                weight = MutateScalar(weight, InputDefinitionParameters.MinWeight, InputDefinitionParameters.MaxWeight);
                path.Add("Weight");
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

    private static LimbConnection MutateLimbConnection(List<string> path, LimbConnection limbConnection, int numLimbNodes)
    {
        path.Add("LimbConnection");

        int childNodeId = limbConnection.ChildNodeId;
        int parentFace = limbConnection.ParentFace;
        Vector2 position = limbConnection.Position;
        Vector3 orientation = limbConnection.Orientation;
        Vector3 scale = limbConnection.Scale;
        bool reflectionX = limbConnection.ReflectionX;
        bool reflectionY = limbConnection.ReflectionY;
        bool reflectionZ = limbConnection.ReflectionZ;
        bool terminalOnly = limbConnection.TerminalOnly;
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
