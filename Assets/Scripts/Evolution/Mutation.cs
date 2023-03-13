using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public static class Mutation
{
    public static Genotype Mutate(Genotype genotype)
    {
        return new Genotype(
            genotype.id,
            genotype.lineage,
            MutateNeuronDefinitions(genotype.brainNeuronDefinitions),
            MutateLimbNodes(genotype.limbNodes)
        );
    }

    private static ReadOnlyCollection<NeuronDefinition> MutateNeuronDefinitions(ReadOnlyCollection<NeuronDefinition> neuronDefinitions)
    {
        List<NeuronDefinition> newNeuronDefinitions = new List<NeuronDefinition>();

        foreach (NeuronDefinition neuronDefinition in neuronDefinitions)
            newNeuronDefinitions.Add(MutateNeuronDefinition(neuronDefinition));

        if (newNeuronDefinitions.Count > 0 && Chance(MutationParameters.RemoveNeuron))
            newNeuronDefinitions.RemoveAt(Random.Range(0, newNeuronDefinitions.Count));

        if (Chance(MutationParameters.AddNeuron))
            newNeuronDefinitions.Add(NeuronDefinition.CreateRandom());

        return newNeuronDefinitions.AsReadOnly();
    }

    private static NeuronDefinition MutateNeuronDefinition(NeuronDefinition neuronDefinition)
    {
        NeuronType type = Chance(MutationParameters.ChangeNeuronType) ? (NeuronType)MutateEnum(neuronDefinition.type) : neuronDefinition.type;
        int numberOfInputs = type.NumberOfInputs();

        List<float> inputPreferences = new List<float>();
        List<float> inputWeights = new List<float>();
        for (int i = 0; i < numberOfInputs; i++)
        {
            inputPreferences.Add(i < neuronDefinition.inputPreferences.Count && Chance(MutationParameters.ChangeNeuronInputPreference)
            ? MutateScalar(neuronDefinition.inputPreferences[i])
            : Random.Range(0f, 1f));
            inputWeights.Add(i < neuronDefinition.inputWeights.Count && Chance(MutationParameters.ChangeNeuronInputWeight)
            ? MutateScalar(neuronDefinition.inputWeights[i])
            : Random.Range(NeuronDefinitionGenerationParameters.MinWeight, NeuronDefinitionGenerationParameters.MaxWeight));
        }

        return new NeuronDefinition(
            type,
            inputPreferences,
            inputWeights
        );
    }

    private static ReadOnlyCollection<LimbNode> MutateLimbNodes(ReadOnlyCollection<LimbNode> limbNodes)
    {
        List<LimbNode> newLimbNodes = limbNodes.ToList();

        // Always add a new node. It will be garbage collected unless an existing node
        // mutates a connection to it.
        newLimbNodes.Add(LimbNode.CreateRandom(null));

        for (int i = 0; i < limbNodes.Count; i++)
            newLimbNodes[i] = MutateLimbNode(limbNodes[i], newLimbNodes.Count);

        return newLimbNodes.AsReadOnly();
    }

    private static LimbNode MutateLimbNode(LimbNode limbNode, int numLimbNodes)
    {
        Vector3 dimensions = new Vector3(
            Chance(MutationParameters.ChangeLimbDimensions) ? MutateScalar(limbNode.dimensions.x) : limbNode.dimensions.x,
            Chance(MutationParameters.ChangeLimbDimensions) ? MutateScalar(limbNode.dimensions.y) : limbNode.dimensions.y,
            Chance(MutationParameters.ChangeLimbDimensions) ? MutateScalar(limbNode.dimensions.z) : limbNode.dimensions.z
        );

        JointDefinition jointDefinition = MutateJointDefinition(limbNode.jointDefinition);

        int recursiveLimit = Chance(MutationParameters.ChangeRecursiveLimit) ? MutateScalar(limbNode.recursiveLimit) : limbNode.recursiveLimit;

        ReadOnlyCollection<NeuronDefinition> neuronDefinitions = MutateNeuronDefinitions(limbNode.neuronDefinitions);
        ReadOnlyCollection<LimbConnection> connections = MutateLimbConnections(limbNode.connections, numLimbNodes);

        return new LimbNode(
            dimensions,
            jointDefinition,
            recursiveLimit,
            neuronDefinitions,
            connections
        );
    }

    private static JointDefinition MutateJointDefinition(JointDefinition jointDefinition)
    {
        JointType type = Chance(MutationParameters.ChangeJointType) ? (JointType)MutateEnum(jointDefinition.type) : jointDefinition.type;
        int degreesOfFreedom = type.DegreesOfFreedom();

        List<float> limits = new List<float>();
        for (int i = 0; i < degreesOfFreedom; i++)
        {
            limits.Add(i < jointDefinition.limits.Count && Chance(MutationParameters.ChangeJointLimit)
            ? Mathf.Abs(MutateScalar(jointDefinition.limits[i]))
            : Random.Range(JointDefinitionGenerationParameters.MinAngle, JointDefinitionGenerationParameters.MaxAngle));
        }

        List<ReadOnlyCollection<float>> effectorInputPreferences = new List<ReadOnlyCollection<float>>();
        List<ReadOnlyCollection<float>> effectorInputWeights = new List<ReadOnlyCollection<float>>();
        for (int i = 0; i < degreesOfFreedom; i++)
        {
            ReadOnlyCollection<float> inputPreferences = (new List<float> {
                i < jointDefinition.effectorInputPreferences.Count && Chance(MutationParameters.ChangeJointEffectorInputPreference)
                ? MutateScalar(jointDefinition.effectorInputPreferences[i][0])
                : Random.Range(0f, 1f)
            }).AsReadOnly();
            ReadOnlyCollection<float> inputWeights = (new List<float> {
                i < jointDefinition.effectorInputWeights.Count && Chance(MutationParameters.ChangeJointEffectorInputWeight)
                ? MutateScalar(jointDefinition.effectorInputWeights[i][0])
                : Random.Range(JointDefinitionGenerationParameters.MinWeight, JointDefinitionGenerationParameters.MaxWeight)
            }).AsReadOnly();
            effectorInputPreferences.Add(inputPreferences);
            effectorInputWeights.Add(inputWeights);
        }

        return new JointDefinition(
            type,
            limits.AsReadOnly(),
            effectorInputPreferences.AsReadOnly(),
            effectorInputWeights.AsReadOnly()
        );
    }

    private static ReadOnlyCollection<LimbConnection> MutateLimbConnections(ReadOnlyCollection<LimbConnection> limbConnections, int numLimbNodes)
    {
        List<LimbConnection> newLimbConnections = limbConnections.Select(x => MutateLimbConnection(x, numLimbNodes)).ToList();

        if (newLimbConnections.Count > 0 && Chance(MutationParameters.RemoveLimbConnection))
            newLimbConnections.RemoveAt(Random.Range(0, newLimbConnections.Count));

        if (Chance(MutationParameters.AddLimbConnection))
            newLimbConnections.Add(LimbConnection.CreateRandom(Random.Range(0, numLimbNodes)));

        return newLimbConnections.AsReadOnly();
    }

    private static LimbConnection MutateLimbConnection(LimbConnection limbConnection, int numLimbNodes)
    {
        int childNodeId = Chance(MutationParameters.ChangeLimbConnectionChildNode) ? Mathf.Clamp(MutateScalar(limbConnection.childNodeId), 0, numLimbNodes - 1) : limbConnection.childNodeId;

        int parentFace = Chance(MutationParameters.ChangeLimbConnectionParentFace) ? Mathf.Clamp(MutateScalar(limbConnection.parentFace), 0, 5) : limbConnection.parentFace;

        Vector2 position = new Vector2(
            Chance(MutationParameters.ChangeLimbConnectionPosition) ? MutateScalar(limbConnection.position.x) : limbConnection.position.x,
            Chance(MutationParameters.ChangeLimbConnectionPosition) ? MutateScalar(limbConnection.position.y) : limbConnection.position.y
        );

        Vector3 orientation = new Vector3(
            Chance(MutationParameters.ChangeLimbConnectionOrientation) ? MutateScalar(limbConnection.orientation.x) : limbConnection.orientation.x,
            Chance(MutationParameters.ChangeLimbConnectionOrientation) ? MutateScalar(limbConnection.orientation.y) : limbConnection.orientation.y,
            Chance(MutationParameters.ChangeLimbConnectionOrientation) ? MutateScalar(limbConnection.orientation.z) : limbConnection.orientation.z
        );

        Vector3 scale = new Vector3(
            Chance(MutationParameters.ChangeLimbConnectionScale) ? MutateScalar(limbConnection.scale.x) : limbConnection.scale.x,
            Chance(MutationParameters.ChangeLimbConnectionScale) ? MutateScalar(limbConnection.scale.y) : limbConnection.scale.y,
            Chance(MutationParameters.ChangeLimbConnectionScale) ? MutateScalar(limbConnection.scale.z) : limbConnection.scale.z
        );

        bool reflection = Chance(MutationParameters.ChangeLimbConnectionReflection) ? !limbConnection.reflection : limbConnection.reflection;

        bool terminalOnly = Chance(MutationParameters.ChangeLimbConnectionTerminalOnly) ? !limbConnection.terminalOnly : limbConnection.terminalOnly;

        return new LimbConnection(
            childNodeId,
            parentFace,
            position,
            orientation,
            scale,
            reflection,
            terminalOnly
        );
    }

    private static object MutateEnum(System.Enum e)
    {
        System.Array enumOptions = System.Enum.GetValues(e.GetType());
        return enumOptions.GetValue(UnityEngine.Random.Range(0, enumOptions.Length));
    }

    private static float MutateScalar(float scalar)
    {
        return scalar + (scalar * Utilities.RandomGaussian());
    }

    private static int MutateScalar(int scalar)
    {
        return scalar + Mathf.RoundToInt(scalar * Utilities.RandomGaussian());
    }

    private static bool Chance(float probability)
    {
        return Random.value <= probability * MutationParameters.Multiplier;
    }
}
