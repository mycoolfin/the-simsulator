using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[System.Serializable]
public class LimbNode : IDefinition
{
    [SerializeField] private Vector3 dimensions;
    [SerializeField] private JointDefinition jointDefinition;
    [SerializeField] private int recursiveLimit;
    [SerializeField] private List<NeuronDefinition> neuronDefinitions;
    [SerializeField] private List<LimbConnection> connections;

    public Vector3 Dimensions => dimensions;
    public JointDefinition JointDefinition => jointDefinition;
    public int RecursiveLimit => recursiveLimit;
    public ReadOnlyCollection<NeuronDefinition> NeuronDefinitions => neuronDefinitions.AsReadOnly();
    public ReadOnlyCollection<LimbConnection> Connections => connections.AsReadOnly();

    public LimbNode(Vector3 dimensions, JointDefinition jointDefinition, int recursiveLimit, IList<NeuronDefinition> neuronDefinitions, IList<LimbConnection> connections)
    {
        ValidateDimensions(dimensions);
        ValidateJointDefinition(jointDefinition);
        ValidateRecursiveLimit(recursiveLimit);
        ValidateNeuronDefinitions(neuronDefinitions);
        ValidateConnections(connections);

        this.dimensions = dimensions;
        this.jointDefinition = jointDefinition;
        this.recursiveLimit = recursiveLimit;
        this.neuronDefinitions = neuronDefinitions == null ? new List<NeuronDefinition>() : neuronDefinitions.ToList();
        this.connections = connections == null ? new List<LimbConnection>() : connections.ToList();
    }

    private static void ValidateDimensions(Vector3 dimensions)
    {
        bool validDimensions = dimensions.x >= LimbNodeParameters.MinSize && dimensions.x <= LimbNodeParameters.MaxSize
                             && dimensions.y >= LimbNodeParameters.MinSize && dimensions.y <= LimbNodeParameters.MaxSize
                             && dimensions.z >= LimbNodeParameters.MinSize && dimensions.z <= LimbNodeParameters.MaxSize;
        if (!validDimensions)
            throw new System.ArgumentException("Dimensions out of bounds. Specified: " + dimensions);
    }

    private static void ValidateJointDefinition(JointDefinition jointDefinition)
    {
        jointDefinition.Validate();
    }

    private static void ValidateRecursiveLimit(int recursiveLimit)
    {
        bool validRecursiveLimit = recursiveLimit >= 0 && recursiveLimit <= LimbNodeParameters.MaxRecursiveLimit;
        if (!validRecursiveLimit)
            throw new System.ArgumentException("Recursive limit must be between 0 and " + LimbNodeParameters.MaxRecursiveLimit + ". Specified: " + recursiveLimit);
    }

    private static void ValidateNeuronDefinitions(IList<NeuronDefinition> neuronDefinitions)
    {
        bool validNeuronDefinitionsCount = neuronDefinitions.Count >= LimbNodeParameters.MinNeurons && neuronDefinitions.Count <= LimbNodeParameters.MaxNeurons;
        if (!validNeuronDefinitionsCount)
            throw new System.ArgumentException("The number of neuron definitions must be between " + LimbNodeParameters.MinNeurons
            + " and " + LimbNodeParameters.MaxNeurons + ". Specified: " + neuronDefinitions.Count);

        foreach (NeuronDefinition neuronDefinition in neuronDefinitions)
            neuronDefinition.Validate();
    }

    private static void ValidateConnections(IList<LimbConnection> connections)
    {
        if (connections == null) return;
        foreach (LimbConnection connection in connections)
            connection.Validate();
    }

    public void Validate()
    {
        ValidateDimensions(dimensions);
        ValidateJointDefinition(jointDefinition);
        ValidateRecursiveLimit(recursiveLimit);
        ValidateNeuronDefinitions(neuronDefinitions);
        ValidateConnections(connections);
    }

    public LimbNode CreateCopy(IList<LimbConnection> newConnections)
    {
        return new LimbNode(
            dimensions,
            jointDefinition,
            recursiveLimit,
            neuronDefinitions,
            newConnections ?? connections
        );
    }

    public static LimbNode CreateRandom(IList<LimbConnection> connections)
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

        return new LimbNode(dimensions, jointDefinition, recursiveLimit, neuronDefinitions, connections);
    }
}
