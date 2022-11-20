using System.Collections.Generic;
using UnityEngine;

public static class Mutation
{
    public static Genotype Mutate(Genotype genotype)
    {
        return Genotype.RemoveUnconnectedNodes(new Genotype(
            MutateNeuronDefinitions(genotype.brainNeuronDefinitions),
            MutateLimbNodes(genotype.limbNodes)
        ));
    }

    private static NeuronDefinition[] MutateNeuronDefinitions(NeuronDefinition[] neuronDefinitions)
    {
        List<NeuronDefinition> newNeuronDefinitions = new List<NeuronDefinition>();

        foreach (NeuronDefinition neuronDefinition in neuronDefinitions)
            newNeuronDefinitions.Add(MutateNeuronDefinition(neuronDefinition));

        if (newNeuronDefinitions.Count > 0 && Chance(MutationParameters.RemoveNeuron))
            newNeuronDefinitions.RemoveAt(Random.Range(0, newNeuronDefinitions.Count));

        if (Chance(MutationParameters.AddNeuron))
            newNeuronDefinitions.Add(NeuronDefinition.CreateRandom());

        return newNeuronDefinitions.ToArray();
    }

    private static NeuronDefinition MutateNeuronDefinition(NeuronDefinition neuronDefinition)
    {
        NeuronType type = Chance(MutationParameters.ChangeNeuronType) ? (NeuronType)MutateEnum(neuronDefinition.type) : neuronDefinition.type;
        int numberOfInputs = type.NumberOfInputs();

        float[] inputPreferences = new float[numberOfInputs];
        float[] inputWeights = new float[numberOfInputs];
        for (int i = 0; i < numberOfInputs; i++)
        {
            inputPreferences[i] = i < neuronDefinition.inputPreferences.Length && Chance(MutationParameters.ChangeNeuronInputPreference)
            ? MutateScalar(neuronDefinition.inputPreferences[i])
            : Random.Range(0f, 1f);
            inputWeights[i] = i < neuronDefinition.inputWeights.Length && Chance(MutationParameters.ChangeNeuronInputWeight)
            ? MutateScalar(neuronDefinition.inputWeights[i])
            : Random.Range(NeuronDefinitionGenerationParameters.MinWeight, NeuronDefinitionGenerationParameters.MaxWeight);
        }

        return new NeuronDefinition(
            type,
            inputPreferences,
            inputWeights
        );
    }

    private static LimbNode[] MutateLimbNodes(LimbNode[] limbNodes)
    {
        LimbNode[] newLimbNodes = new LimbNode[limbNodes.Length + 1];

        // Always add a new node. It will be garbage collected unless an existing node
        // mutates a connection to it.
        newLimbNodes[newLimbNodes.Length - 1] = LimbNode.CreateRandom(null);

        for (int i = 0; i < limbNodes.Length; i++)
            newLimbNodes[i] = MutateLimbNode(limbNodes[i], newLimbNodes.Length);

        return newLimbNodes;
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

        NeuronDefinition[] neuronDefinitions = MutateNeuronDefinitions(limbNode.neuronDefinitions);
        LimbConnection[] connections = MutateLimbConnections(limbNode.connections, numLimbNodes);

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

        float[] limits = new float[degreesOfFreedom];
        for (int i = 0; i < degreesOfFreedom; i++)
        {
            limits[i] = i < jointDefinition.limits.Length && Chance(MutationParameters.ChangeJointLimit)
            ? Mathf.Abs(MutateScalar(jointDefinition.limits[i]))
            : Random.Range(JointDefinitionGenerationParameters.MinAngle, JointDefinitionGenerationParameters.MaxAngle);
        }

        float[][] effectorInputPreferences = new float[degreesOfFreedom][];
        float[][] effectorInputWeights = new float[degreesOfFreedom][];
        for (int i = 0; i < degreesOfFreedom; i++)
        {
            float[] inputPreferences = new float[1] {
                i < jointDefinition.effectorInputPreferences.Length && Chance(MutationParameters.ChangeJointEffectorInputPreference)
                ? MutateScalar(jointDefinition.effectorInputPreferences[i][0])
                : Random.Range(0f, 1f)
            };
            float[] inputWeights = new float[1] {
                i < jointDefinition.effectorInputWeights.Length && Chance(MutationParameters.ChangeJointEffectorInputWeight)
                ? MutateScalar(jointDefinition.effectorInputWeights[i][0])
                : Random.Range(JointDefinitionGenerationParameters.MinWeight, JointDefinitionGenerationParameters.MaxWeight)
            };
            effectorInputPreferences[i] = inputPreferences;
            effectorInputWeights[i] = inputWeights;
        }

        return new JointDefinition(
            type,
            limits,
            effectorInputPreferences,
            effectorInputWeights
        );
    }

    private static LimbConnection[] MutateLimbConnections(LimbConnection[] limbConnections, int numLimbNodes)
    {
        List<LimbConnection> newLimbConnections = new List<LimbConnection>();

        foreach (LimbConnection limbConnection in limbConnections)
            newLimbConnections.Add(MutateLimbConnection(limbConnection, numLimbNodes));

        if (newLimbConnections.Count > 0 && Chance(MutationParameters.RemoveLimbConnection))
            newLimbConnections.RemoveAt(Random.Range(0, newLimbConnections.Count));

        if (Chance(MutationParameters.AddLimbConnection))
            newLimbConnections.Add(LimbConnection.CreateRandom(Random.Range(0, numLimbNodes)));

        return newLimbConnections.ToArray();
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