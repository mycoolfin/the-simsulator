using UnityEngine;

[System.Serializable]
public struct LimbNode
{
    public readonly Vector3 dimensions;
    public readonly JointDefinition jointDefinition;
    public readonly int recursiveLimit;
    public readonly NeuronDefinition[] neuronDefinitions;
    public readonly LimbConnection[] connections;

    public LimbNode(Vector3 dimensions, JointDefinition jointDefinition, int recursiveLimit, NeuronDefinition[] neuronDefinitions, LimbConnection[] connections)
    {
        this.dimensions = new Vector3(
            Mathf.Clamp(dimensions.x, LimbNodeParameters.MinSize, LimbNodeParameters.MaxSize),
            Mathf.Clamp(dimensions.y, LimbNodeParameters.MinSize, LimbNodeParameters.MaxSize),
            Mathf.Clamp(dimensions.z, LimbNodeParameters.MinSize, LimbNodeParameters.MaxSize)
        );
        this.jointDefinition = jointDefinition;
        this.recursiveLimit = recursiveLimit;
        this.neuronDefinitions = neuronDefinitions == null ? new NeuronDefinition[0] : (NeuronDefinition[]) neuronDefinitions.Clone();
        this.connections = connections == null ? new LimbConnection[0] : (LimbConnection[]) connections.Clone();
    }

    public LimbNode CreateCopy(LimbConnection[] newConnections)
    {
        return new LimbNode(
            dimensions,
            jointDefinition,
            recursiveLimit,
            neuronDefinitions,
            newConnections ?? (LimbConnection[]) connections.Clone()
        );
    }

    public static LimbNode CreateRandom(LimbConnection[] connections)
    {
        Vector3 dimensions = new(
            Random.Range(LimbNodeGenerationParameters.MinSize, LimbNodeGenerationParameters.MaxSize),
            Random.Range(LimbNodeGenerationParameters.MinSize, LimbNodeGenerationParameters.MaxSize),
            Random.Range(LimbNodeGenerationParameters.MinSize, LimbNodeGenerationParameters.MaxSize)
        );

        JointDefinition jointDefinition = JointDefinition.CreateRandom();

        int recursiveLimit = Random.Range(LimbNodeGenerationParameters.MinRecursiveLimit, LimbNodeGenerationParameters.MaxRecursiveLimit);

        int numberOfNeurons = Random.Range(LimbNodeGenerationParameters.MinNeurons, LimbNodeGenerationParameters.MaxNeurons);
        NeuronDefinition[] neuronDefinitions = new NeuronDefinition[numberOfNeurons];
        for (int i = 0; i < numberOfNeurons; i++)
            neuronDefinitions[i] = NeuronDefinition.CreateRandom();

        return new LimbNode(dimensions, jointDefinition, recursiveLimit, neuronDefinitions, connections);
    }
}
