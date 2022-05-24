using UnityEngine;

public class LimbNode : MonoBehaviour
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

    public LimbNode AddChildLimb(int parentFace, float parentFaceU, float parentFaceV, float xRotation, float yRotation, float zRotation, Vector3 scale, JointType jointType)
    {
        LimbNode childLimb = LimbCreator.CreateLimb();

        childLimb.transform.parent = transform.parent;

        childLimb.Dimensions = scale;

        // Transform child so that it is attached to the parent.
        int perpendicularAxis = parentFace % 3;
        Vector3 parentFaceNormal = Vector3.zero;
        parentFaceNormal[perpendicularAxis] = parentFace > 2 ? 0.5f : -0.5f;
        parentFaceNormal = (transform.rotation * parentFaceNormal).normalized;

        Vector3 parentAnchorPoint = Vector3.one;
        parentAnchorPoint[perpendicularAxis] *= parentFace > 2 ? 0.5f : -0.5f;
        parentAnchorPoint[(perpendicularAxis + 1) % 3] *= 0.5f * parentFaceU;
        parentAnchorPoint[(perpendicularAxis + 2) % 3] *= 0.5f * parentFaceV;
        parentAnchorPoint = transform.TransformPoint(parentAnchorPoint);

        Vector3 childDesiredPosition = parentAnchorPoint + (parentFaceNormal * (childLimb.Dimensions.z / 2));
        childLimb.transform.position = childDesiredPosition;
        childLimb.transform.rotation = Quaternion.FromToRotation(transform.forward, parentFaceNormal) * transform.rotation;
        Vector3 xAxis = childLimb.transform.right;
        Vector3 yAxis = childLimb.transform.up;
        childLimb.transform.RotateAround(parentAnchorPoint, xAxis, xRotation);
        childLimb.transform.RotateAround(parentAnchorPoint, yAxis, yRotation);
        childLimb.transform.RotateAround(parentAnchorPoint, parentFaceNormal, zRotation);

        // Add and configure the joint between the child and the parent.
        float childCrossSectionalArea = childLimb.transform.localScale.x * childLimb.transform.localScale.y;
        float parentCrossSectionalArea = transform.localScale[(perpendicularAxis + 1) % 3] * transform.localScale[(perpendicularAxis + 2) % 3];
        childLimb.maximumJointStrength = Mathf.Min(childCrossSectionalArea, parentCrossSectionalArea);

        childLimb.joint = JointCreator.CreateJoint(childLimb, this, jointType);

        return childLimb;
    }
}
