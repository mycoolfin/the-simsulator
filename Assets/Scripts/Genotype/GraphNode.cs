using UnityEngine;

public class GraphNode
{
    public Vector3 dimensions;
    public JointType jointType;
    public float[] jointLimits;
    public int recursiveLimit;
    public NeuronBase[] neurons;
    public GraphConnection[] connections;
}
