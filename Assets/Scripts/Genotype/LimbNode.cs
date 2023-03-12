using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[System.Serializable]
public struct LimbNode
{
    public readonly Vector3 dimensions;
    public readonly JointDefinition jointDefinition;
    public readonly int recursiveLimit;
    public readonly ReadOnlyCollection<NeuronDefinition> neuronDefinitions;
    public readonly ReadOnlyCollection<LimbConnection> connections;

    public LimbNode(Vector3 dimensions, JointDefinition jointDefinition, int recursiveLimit, ReadOnlyCollection<NeuronDefinition> neuronDefinitions, ReadOnlyCollection<LimbConnection> connections)
    {
        this.dimensions = new Vector3(
            Mathf.Clamp(dimensions.x, LimbNodeParameters.MinSize, LimbNodeParameters.MaxSize),
            Mathf.Clamp(dimensions.y, LimbNodeParameters.MinSize, LimbNodeParameters.MaxSize),
            Mathf.Clamp(dimensions.z, LimbNodeParameters.MinSize, LimbNodeParameters.MaxSize)
        );
        this.jointDefinition = jointDefinition;
        this.recursiveLimit = recursiveLimit;
        this.neuronDefinitions = neuronDefinitions == null ? new List<NeuronDefinition>().AsReadOnly() : neuronDefinitions;
        this.connections = connections == null ? new List<LimbConnection>().AsReadOnly() : connections;
    }

    public LimbNode CreateCopy(ReadOnlyCollection<LimbConnection> newConnections)
    {
        return new LimbNode(
            dimensions,
            jointDefinition,
            recursiveLimit,
            neuronDefinitions,
            newConnections ?? connections
        );
    }

    public static LimbNode CreateRandom(ReadOnlyCollection<LimbConnection> connections)
    {
        Vector3 dimensions = new(
            Random.Range(LimbNodeGenerationParameters.MinSize, LimbNodeGenerationParameters.MaxSize),
            Random.Range(LimbNodeGenerationParameters.MinSize, LimbNodeGenerationParameters.MaxSize),
            Random.Range(LimbNodeGenerationParameters.MinSize, LimbNodeGenerationParameters.MaxSize)
        );

        JointDefinition jointDefinition = JointDefinition.CreateRandom();

        int recursiveLimit = Random.Range(LimbNodeGenerationParameters.MinRecursiveLimit, LimbNodeGenerationParameters.MaxRecursiveLimit);

        int numberOfNeurons = Random.Range(LimbNodeGenerationParameters.MinNeurons, LimbNodeGenerationParameters.MaxNeurons);
        List<NeuronDefinition> neuronDefinitions = new List<NeuronDefinition>();
        for (int i = 0; i < numberOfNeurons; i++)
            neuronDefinitions.Add(NeuronDefinition.CreateRandom());

        return new LimbNode(dimensions, jointDefinition, recursiveLimit, neuronDefinitions.AsReadOnly(), connections);
    }
}
