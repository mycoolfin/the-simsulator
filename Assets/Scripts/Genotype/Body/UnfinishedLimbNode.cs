using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public class UnfinishedLimbNode : ILimbNodeEssentialInfo
{
    public JointType jointType;
    public int neuronCount;

    private int recursiveLimit;
    private List<LimbConnection> connections;
    public int RecursiveLimit => recursiveLimit;
    public ReadOnlyCollection<LimbConnection> Connections => connections.AsReadOnly();
    public int SignalEmitterCount => jointType.DegreesOfFreedom() + neuronCount + 3;

    private UnfinishedLimbNode(JointType jointType, int neuronCount, int recursiveLimit, IList<LimbConnection> connections)
    {
        this.jointType = jointType;
        this.neuronCount = neuronCount;
        this.recursiveLimit = recursiveLimit;
        this.connections = connections == null ? new List<LimbConnection>() : connections.ToList();
    }

    public static UnfinishedLimbNode CreateRandom(IList<LimbConnection> connections)
    {
        return new(
            Utilities.RandomEnumValue<JointType>(),
            Random.Range(LimbNodeParameters.MinNeurons, LimbNodeParameters.MaxNeurons + 1),
            UnityEngine.Random.Range(0, LimbNodeParameters.MaxRecursiveLimit + 1),
            connections
        );
    }
}
