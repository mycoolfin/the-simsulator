using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[Serializable]
public class LimbNode : ILimbNodeEssentialInfo
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
    public int SignalEmitterCount => jointDefinition.Type.DegreesOfFreedom() + neuronDefinitions.Count + 3;

    public LimbNode(Vector3 dimensions, JointDefinition jointDefinition, int recursiveLimit,
                     IList<NeuronDefinition> neuronDefinitions, IList<LimbConnection> connections)
    {
        this.dimensions = dimensions;
        this.jointDefinition = jointDefinition;
        this.recursiveLimit = recursiveLimit;
        this.neuronDefinitions = neuronDefinitions == null ? new List<NeuronDefinition>() : neuronDefinitions.ToList();
        this.connections = connections == null ? new List<LimbConnection>() : connections.ToList();
    }

    public void Validate(EmitterAvailabilityMap emitterAvailabilityMap)
    {
        bool validDimensions = dimensions.x >= LimbNodeParameters.MinSize && dimensions.x <= LimbNodeParameters.MaxSize
                     && dimensions.y >= LimbNodeParameters.MinSize && dimensions.y <= LimbNodeParameters.MaxSize
                     && dimensions.z >= LimbNodeParameters.MinSize && dimensions.z <= LimbNodeParameters.MaxSize;
        if (!validDimensions)
            throw new ArgumentException("Dimensions out of bounds. Specified: " + dimensions);

        jointDefinition.Validate(emitterAvailabilityMap);

        bool validRecursiveLimit = recursiveLimit >= 0 && recursiveLimit <= LimbNodeParameters.MaxRecursiveLimit;
        if (!validRecursiveLimit)
            throw new ArgumentException("Recursive limit must be between 0 and " + LimbNodeParameters.MaxRecursiveLimit + ". Specified: " + recursiveLimit);

        bool validNeuronDefinitionsCount = neuronDefinitions.Count >= LimbNodeParameters.MinNeurons && neuronDefinitions.Count <= LimbNodeParameters.MaxNeurons;
        if (!validNeuronDefinitionsCount)
            throw new ArgumentException("The number of neuron definitions must be between " + LimbNodeParameters.MinNeurons
            + " and " + LimbNodeParameters.MaxNeurons + ". Specified: " + neuronDefinitions.Count);

        foreach (NeuronDefinition neuronDefinition in neuronDefinitions)
            neuronDefinition.Validate(emitterAvailabilityMap);

        if (connections != null)
        {
            bool validLimbConnectionCount = connections.Count >= 0 && connections.Count <= LimbNodeParameters.MaxLimbConnections;
            if (!validLimbConnectionCount)
                throw new ArgumentException("Limb connection count must be between 0 and " + LimbNodeParameters.MaxLimbConnections + ". Specified: " + connections.Count);
            foreach (LimbConnection connection in connections)
                connection.Validate();
        }
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

    public static LimbNode CreateRandom(
        EmitterAvailabilityMap emitterAvailabilityMap,
        UnfinishedLimbNode unfinishedLimbNode
    )
    {
        Vector3 dimensions = new(
            UnityEngine.Random.Range(LimbNodeParameters.MinSize, LimbNodeParameters.MaxSize),
            UnityEngine.Random.Range(LimbNodeParameters.MinSize, LimbNodeParameters.MaxSize),
            UnityEngine.Random.Range(LimbNodeParameters.MinSize, LimbNodeParameters.MaxSize)
        );

        JointDefinition jointDefinition = JointDefinition.CreateRandom(emitterAvailabilityMap, unfinishedLimbNode.jointType);

        List<NeuronDefinition> neuronDefinitions = new List<NeuronDefinition>();
        for (int i = 0; i < unfinishedLimbNode.neuronCount; i++)
            neuronDefinitions.Add(NeuronDefinition.CreateRandom(emitterAvailabilityMap));

        return new(
            dimensions,
            jointDefinition,
            unfinishedLimbNode.RecursiveLimit,
            neuronDefinitions,
            unfinishedLimbNode.Connections
        );
    }
}
