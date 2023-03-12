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
    public List<List<float>> jointEffectorInputPreferences;

    public List<NeuronBase> neurons;
    public List<List<float>> neuronInputPreferences;

    public Limb parentLimb;
    public List<Limb> childLimbs;

    public static Limb CreateLimb(LimbNode limbNode)
    {
        Limb limb = Instantiate(ResourceManager.Instance.limbPrefab).GetComponent<Limb>();

        // Add the limb neurons, but don't wire them up until later (when we have a complete morphology to reference).
        limb.neurons = new List<NeuronBase>();
        limb.neuronInputPreferences = new List<List<float>>();
        for (int i = 0; i < limbNode.neuronDefinitions.Count; i++)
        {
            limb.neurons.Add(NeuronBase.CreateNeuron(limbNode.neuronDefinitions[i].type, limbNode.neuronDefinitions[i].inputWeights));
            limb.neuronInputPreferences.Add(limbNode.neuronDefinitions[i].inputPreferences);
        }

        Vector3 clampedDimensions = ClampDimensions(limbNode.dimensions);
        limb.unscaledDimensions = clampedDimensions;
        limb.Dimensions = clampedDimensions;

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

        Vector3 existingScaling = Vector3.Scale(transform.localScale, new Vector3(1f / unscaledDimensions.x, 1f / unscaledDimensions.y, 1f / unscaledDimensions.z));
        Vector3 childDimensions = ClampDimensions(Vector3.Scale(Vector3.Scale(childLimbNode.dimensions, existingScaling), limbConnection.scale));

        Quaternion childRotation = transform.rotation * localChildRotation;
        Vector3 childPosition = transform.TransformPoint(localParentAnchorPoint) + (childRotation * Vector3.forward * childDimensions.z / 2f);

        // If the new limb intersects with an existing limb on this creature, abandon creation.
        Collider[] looseCheck = Physics.OverlapBox(childPosition, childDimensions / 4f, childRotation);
        if (looseCheck.Any(col =>
        {
            Limb collidedLimb = col.gameObject.GetComponent<Limb>();
            return collidedLimb != null && collidedLimb.transform.parent == transform.parent;
        }))
            return null;
        Collider[] strictCheck = Physics.OverlapBox(childPosition, childDimensions / 2f, childRotation);
        if (strictCheck.Any(col =>
        {
            Limb collidedLimb = col.gameObject.GetComponent<Limb>();
            return collidedLimb != null && collidedLimb.transform.parent == transform.parent && collidedLimb != this;
        }))
            return null;

        // Create the limb.
        Limb childLimb = CreateLimb(childLimbNode);
        childLimbs.Add(childLimb);
        childLimb.parentLimb = this;
        childLimb.transform.parent = transform.parent;
        childLimb.transform.position = childPosition;
        childLimb.transform.rotation = childRotation;
        childLimb.Dimensions = childDimensions;

        // Add and configure the joint between the child and the parent.
        float childCrossSectionalArea = childLimb.transform.localScale.x * childLimb.transform.localScale.y;
        float parentCrossSectionalArea = transform.localScale[(perpendicularAxis + 1) % 3] * transform.localScale[(perpendicularAxis + 2) % 3];
        float maximumJointStrength = Mathf.Min(childCrossSectionalArea, parentCrossSectionalArea);
        childLimb.joint = JointBase.CreateJoint(childLimbNode.jointDefinition.type, childLimb.gameObject, GetComponent<Rigidbody>(), maximumJointStrength, childLimbNode.jointDefinition.limits.ToList());
        for (int i = 0; i < childLimb.joint.effectors.Count; i++)
            childLimb.joint.effectors[i].Weights = childLimbNode.jointDefinition.effectorInputWeights[i].ToList();
        childLimb.jointEffectorInputPreferences = childLimbNode.jointDefinition.effectorInputPreferences.Select(x => x.ToList()).ToList();

        Physics.SyncTransforms(); // Ensure that colliders are positioned correctly for potential children.

        return childLimb;
    }

    private static Vector3 ClampDimensions(Vector3 dimensions)
    {
        return new Vector3(
            Mathf.Clamp(dimensions.x, LimbParameters.MinScale, LimbParameters.MaxScale),
            Mathf.Clamp(dimensions.y, LimbParameters.MinScale, LimbParameters.MaxScale),
            Mathf.Clamp(dimensions.z, LimbParameters.MinScale, LimbParameters.MaxScale)
        );
    }
}
