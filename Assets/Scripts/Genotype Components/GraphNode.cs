using UnityEngine;

public class GraphNode : MonoBehaviour
{
    public Vector3 dimensions;
    public JointType jointType;
    public int recursiveLimit;
    public NeuronBase[] neurons;
    public GraphConnection[] connections;

}
