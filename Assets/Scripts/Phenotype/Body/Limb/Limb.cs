using UnityEngine;

public class Limb : MonoBehaviour
{
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

    public NeuronBase[] neurons;

    public static Limb CreateLimb()
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Limb" + Random.Range(0, 100).ToString();
        box.AddComponent<Rigidbody>();
        Limb limb = box.AddComponent<Limb>();
        return limb;
    }

    public Limb AddChildLimb(int parentFace, Vector2 parentFacePlacement, Vector3 limbRotations, Vector3 limbDimensions, JointType jointType, float[] dofAngleLimits)
    {
        // Find the local position and rotation that attaches the back face of the new limb to the chosen face of the parent limb.
        int perpendicularAxis = parentFace % 3;
        Vector3 localParentFaceNormal = Vector3.zero;
        localParentFaceNormal[perpendicularAxis] = parentFace > 2 ? 1f : -1f;

        Vector3 localParentAnchorPoint = Vector3.one;
        localParentAnchorPoint[perpendicularAxis] *= parentFace > 2 ? 0.5f : -0.5f;
        localParentAnchorPoint[(perpendicularAxis + 1) % 3] *= 0.5f * parentFacePlacement[0];
        localParentAnchorPoint[(perpendicularAxis + 2) % 3] *= 0.5f * parentFacePlacement[1];

        // Apply limb rotations.
        Quaternion localChildRotation = Quaternion.AngleAxis(limbRotations.z, Vector3.forward)
         * Quaternion.AngleAxis(limbRotations.y, Vector3.up)
         * Quaternion.AngleAxis(limbRotations.x, Vector3.right)
         * Quaternion.FromToRotation(Vector3.forward, localParentFaceNormal);

        Quaternion childRotation = transform.rotation * localChildRotation;
        Vector3 childPosition = transform.TransformPoint(localParentAnchorPoint) + (childRotation * Vector3.forward * limbDimensions.z / 2f);

        // If the new limb intersects with an existing limb that is not its parent, abandon creation.
        foreach (Collider col in Physics.OverlapBox(childPosition, limbDimensions / 2, childRotation))
        {
            Limb collidedLimb = col.gameObject.GetComponent<Limb>();
            if (collidedLimb != null && collidedLimb != this)
            {
                Debug.Log("New limb was ignored as it would have intersected with " + col.gameObject.name + ".");
                return null;
            }
        }

        // Create the limb.
        Limb childLimb = CreateLimb();
        childLimb.transform.parent = transform.parent;
        childLimb.transform.position = childPosition;
        childLimb.transform.rotation = childRotation;
        childLimb.Dimensions = limbDimensions;
        BoxCollider childLimbCollider = childLimb.GetComponent<BoxCollider>();
        childLimbCollider.size = Vector3.Scale(childLimbCollider.size, new Vector3(Mathf.Sign(limbDimensions.x), Mathf.Sign(limbDimensions.y), Mathf.Sign(limbDimensions.z)));

        // Add and configure the joint between the child and the parent.
        float childCrossSectionalArea = childLimb.transform.localScale.x * childLimb.transform.localScale.y;
        float parentCrossSectionalArea = transform.localScale[(perpendicularAxis + 1) % 3] * transform.localScale[(perpendicularAxis + 2) % 3];
        float maximumJointStrength = Mathf.Min(childCrossSectionalArea, parentCrossSectionalArea);

        childLimb.joint = JointBase.CreateJoint(jointType, childLimb.gameObject, GetComponent<Rigidbody>(), maximumJointStrength, dofAngleLimits);

        // Connect up neurons, sensors and effectors.
        JointAngleSensor[] jointAngleSensors = childLimb.joint.GetJointAngleSensors();
        JointAngleEffector[] jointAngleEffectors = childLimb.joint.GetJointAngleEffectors();

        return childLimb;
    }
}
