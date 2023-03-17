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
        bool validDimensions = dimensions.x >= LimbNodeParameters.MinSize && dimensions.x <= LimbNodeParameters.MaxSize
                             && dimensions.y >= LimbNodeParameters.MinSize && dimensions.y <= LimbNodeParameters.MaxSize
                             && dimensions.z >= LimbNodeParameters.MinSize && dimensions.z <= LimbNodeParameters.MaxSize;
        if (!validDimensions)
            throw new System.ArgumentException("Dimensions out of bounds. Specified: " + dimensions);

        bool validRecursiveLimit = recursiveLimit >= 0 && recursiveLimit <= LimbNodeParameters.MaxRecursiveLimit;
        if (!validRecursiveLimit)
            throw new System.ArgumentException("Recursive limit must be between 0 and " + LimbNodeParameters.MaxRecursiveLimit + ". Specified: " + recursiveLimit);

        bool validNeuronDefinitions = neuronDefinitions.Count >= LimbNodeParameters.MinNeurons && neuronDefinitions.Count <= LimbNodeParameters.MaxNeurons;
        if (!validNeuronDefinitions)
            throw new System.ArgumentException("The number of neuron definitions must be between " + LimbNodeParameters.MinNeurons
            + " and " + LimbNodeParameters.MaxNeurons + ". Specified: " + neuronDefinitions.Count);

        this.dimensions = dimensions;
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
            Random.Range(LimbNodeParameters.MinSize, LimbNodeParameters.MaxSize),
            Random.Range(LimbNodeParameters.MinSize, LimbNodeParameters.MaxSize),
            Random.Range(LimbNodeParameters.MinSize, LimbNodeParameters.MaxSize)
        );

        JointDefinition jointDefinition = JointDefinition.CreateRandom();

        int recursiveLimit = Random.Range(0, LimbNodeParameters.MaxRecursiveLimit);

        int numberOfNeurons = Random.Range(LimbNodeParameters.MinNeurons, LimbNodeParameters.MaxNeurons);
        List<NeuronDefinition> neuronDefinitions = new List<NeuronDefinition>();
        for (int i = 0; i < numberOfNeurons; i++)
            neuronDefinitions.Add(NeuronDefinition.CreateRandom());

        return new LimbNode(dimensions, jointDefinition, recursiveLimit, neuronDefinitions.AsReadOnly(), connections);
    }
}
