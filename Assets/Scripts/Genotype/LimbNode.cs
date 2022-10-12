using UnityEngine;

public class LimbNode
{
    public readonly Vector3 dimensions;
    public readonly JointType jointType;
    public readonly float[] jointLimits;
    public readonly float[][] jointEffectorInputPreferences;
    public readonly float[][] jointEffectorWeights;
    public readonly int recursiveLimit;
    public readonly NeuronNode[] neurons;
    public LimbConnection[] connections;

    public LimbNode(Vector3 dimensions, JointType jointType, float[] jointLimits, float[][] jointEffectorInputPreferences, float[][] jointEffectorWeights, int recursiveLimit, NeuronNode[] neurons)
    {
        if (jointLimits?.Length != jointEffectorInputPreferences?.Length || jointLimits?.Length != jointEffectorWeights?.Length)
            throw new System.ArgumentException("Joint parameters are improperly specified");
        
        this.dimensions = dimensions;
        this.jointType = jointType;
        this.jointLimits = jointLimits;
        this.jointEffectorInputPreferences = jointEffectorInputPreferences;
        this.jointEffectorWeights = jointEffectorWeights;
        this.recursiveLimit = recursiveLimit;
        this.neurons = neurons == null ? new NeuronNode[0] : neurons;
        this.connections = new LimbConnection[0];
    }
}
