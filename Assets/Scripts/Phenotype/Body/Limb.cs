using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Limb : MonoBehaviour
{
    private Vector3 unscaledDimensions;
    public Vector3 Dimensions
    {
        get { return transform.localScale; }
        set
        {
            transform.localScale = value;
            GetComponent<Rigidbody>().mass = value.x * value.y * value.z; // Mass is proportional to volume.
        }
    }

    public JointBase joint;
    private float[][] jointEffectorInputPreferences;

    public NeuronBase[] neurons;
    private float[][] neuronInputPreferences;

    public Limb parentLimb;
    public List<Limb> childLimbs;

    public static Limb CreateLimb(LimbNode limbNode)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Limb " + Random.Range(0, 100).ToString();
        box.AddComponent<Rigidbody>();
        Limb limb = box.AddComponent<Limb>();

        // Add the limb neurons, but don't wire them up until later (when we have a complete morphology to reference).
        limb.neurons = new NeuronBase[limbNode.neurons.Length];
        limb.neuronInputPreferences = new float[limbNode.neurons.Length][];
        for (int i = 0; i < limbNode.neurons.Length; i++)
        {
            limb.neurons[i] = NeuronBase.CreateNeuron(limbNode.neurons[i].type, limbNode.neurons[i].inputWeights);
            limb.neuronInputPreferences[i] = limbNode.neurons[i].inputPreferences;
        }

        limb.unscaledDimensions = limbNode.dimensions;
        limb.Dimensions = limbNode.dimensions;

        limb.childLimbs = new List<Limb>();

        return limb;
    }

    public Limb AddChildLimb(LimbNode childLimbNode, LimbConnection limbConnection)
    {
        // Find the local position and rotation that attaches the back face of the new limb to the chosen face of the parent limb.
        int parentFace = limbConnection.reflection ? (limbConnection.parentFace + 3) % 6 : limbConnection.parentFace;
        int perpendicularAxis = parentFace % 3;
        Vector3 localParentFaceNormal = Vector3.zero;
        localParentFaceNormal[perpendicularAxis] = parentFace > 2 ? 1f : -1f;

        Vector3 localParentAnchorPoint = Vector3.one;
        localParentAnchorPoint[perpendicularAxis] *= parentFace > 2 ? 0.5f : -0.5f;
        localParentAnchorPoint[(perpendicularAxis + 1) % 3] *= 0.5f * limbConnection.position[0];
        localParentAnchorPoint[(perpendicularAxis + 2) % 3] *= 0.5f * limbConnection.position[1];

        // Apply limb rotations.
        Quaternion localChildRotation = Quaternion.AngleAxis(limbConnection.orientation.z, Vector3.forward)
         * Quaternion.AngleAxis(limbConnection.orientation.y, Vector3.up)
         * Quaternion.AngleAxis(limbConnection.orientation.x, Vector3.right)
         * Quaternion.FromToRotation(Vector3.forward, localParentFaceNormal);

        Vector3 existingScaling = Vector3.Scale(transform.localScale, new Vector3(1/unscaledDimensions.x, 1/unscaledDimensions.y, 1/unscaledDimensions.z));
        Vector3 childDimensions = Vector3.Scale(Vector3.Scale(childLimbNode.dimensions, existingScaling), limbConnection.scale);
        Quaternion childRotation = transform.rotation * localChildRotation;
        Vector3 childPosition = transform.TransformPoint(localParentAnchorPoint) + (childRotation * Vector3.forward * childDimensions.z / 2f);

        // If the new limb intersects with an existing limb that is not its parent, abandon creation.
        foreach (Collider col in Physics.OverlapBox(childPosition, childDimensions / 2, childRotation))
        {
            Limb collidedLimb = col.gameObject.GetComponent<Limb>();
            if (collidedLimb != null && collidedLimb != this)
            {
                Debug.Log("New limb was ignored as it would have intersected with " + col.gameObject.name + ".");
                return null;
            }
        }

        // Create the limb.
        Limb childLimb = CreateLimb(childLimbNode);
        childLimbs.Add(childLimb);
        childLimb.parentLimb = this;
        childLimb.transform.parent = transform.parent;
        childLimb.transform.position = childPosition;
        childLimb.transform.rotation = childRotation;
        childLimb.Dimensions = childDimensions;
        BoxCollider childLimbCollider = childLimb.GetComponent<BoxCollider>();
        childLimbCollider.size = Vector3.Scale(childLimbCollider.size, new Vector3(Mathf.Sign(childDimensions.x), Mathf.Sign(childDimensions.y), Mathf.Sign(childDimensions.z)));

        // Add and configure the joint between the child and the parent.
        float childCrossSectionalArea = childLimb.transform.localScale.x * childLimb.transform.localScale.y;
        float parentCrossSectionalArea = transform.localScale[(perpendicularAxis + 1) % 3] * transform.localScale[(perpendicularAxis + 2) % 3];
        float maximumJointStrength = Mathf.Min(childCrossSectionalArea, parentCrossSectionalArea);
        childLimb.joint = JointBase.CreateJoint(childLimbNode.jointType, childLimb.gameObject, GetComponent<Rigidbody>(), maximumJointStrength, childLimbNode.jointLimits);
        for (int i = 0; i < childLimb.joint.effectors.Length; i++)
            childLimb.joint.effectors[i].Weights = childLimbNode.jointEffectorWeights[i];
        childLimb.jointEffectorInputPreferences = childLimbNode.jointEffectorInputPreferences;

        return childLimb;
    }

    public void ConfigureLimbNervousSystem(Brain brain)
    {
        // The pool of emitters to choose from.
        // Limb signal receivers can connect to signal emitters located in:
        // - This limb
        // - The parent limb
        // - A child limb
        // - The brain
        ISignalEmitter[] emitterPool = (parentLimb?.neurons ?? new ISignalEmitter[0])
        .Concat(neurons)
        .Concat(joint?.sensors ?? new ISignalEmitter[0])
        .Concat(childLimbs.Where(childLimb => childLimb.joint != null).SelectMany(childLimb => childLimb.neurons))
        .Concat(brain.neurons)
        .ToArray();

        // The receivers in this limb.
        ISignalReceiver[] limbSignalReceivers = neurons
        .Concat(joint?.effectors ?? new ISignalReceiver[0])
        .ToArray();

        // The input preferences of each receiver.
        float[][] inputPreferences = neuronInputPreferences
        .Concat(jointEffectorInputPreferences ?? new float[0][])
        .ToArray();

        NervousSystem.ConfigureSignalReceivers(limbSignalReceivers, inputPreferences, emitterPool);
    }
}
