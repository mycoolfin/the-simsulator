using UnityEngine;

public enum JointType
{
    Rigid,
    Revolute,
    Twist,
    Universal,
    BendTwist,
    TwistBend,
    Spherical
}

public abstract class JointBase : MonoBehaviour
{
    protected abstract int DegreesOfFreedom { get; }
    protected ConfigurableJoint joint;
    protected float maximumJointStrength;
    protected float[] dofAngleLimits;
    protected int[] dofAxisIndices;
    public JointAngleSensor[] sensors;
    public JointAngleEffector[] effectors;

    public Vector3 angles;
    private Vector3 excitations;

    public abstract void Initialise(Rigidbody connectedBody, float maximumJointStrength, float[] dofAngleLimits);

    private Quaternion intialOriginRotation;

    // Debug.
    [Range(-1f, 1f)]
    public float debugPrimaryExcitation;
    [Range(-1f, 1f)]
    public float debugSecondaryExcitation;
    [Range(-1f, 1f)]
    public float debugTertiaryExcitation;
    public bool resetExcitations;
    public Vector3 primaryTorqueDisplay;
    public Vector3 secondaryTorqueDisplay;
    public Vector3 tertiaryTorqueDisplay;

    public static JointBase CreateJoint(JointType jointType, GameObject gameObject, Rigidbody connectedBody, float maximumJointStrength, float[] dofAngleLimits)
    {
        JointBase joint;
        switch (jointType)
        {
            case JointType.Rigid:
                joint = gameObject.AddComponent<RigidJoint>();
                break;
            case JointType.Revolute:
                joint = gameObject.AddComponent<RevoluteJoint>();
                break;
            case JointType.Twist:
                joint = gameObject.AddComponent<TwistJoint>();
                break;
            case JointType.Universal:
                joint = gameObject.AddComponent<UniversalJoint>();
                break;
            case JointType.BendTwist:
                joint = gameObject.AddComponent<BendTwistJoint>();
                break;
            case JointType.TwistBend:
                joint = gameObject.AddComponent<TwistBendJoint>();
                break;
            case JointType.Spherical:
                joint = gameObject.AddComponent<SphericalJoint>();
                break;
            default:
                throw new System.ArgumentException("Unknown joint type '" + jointType + "'");
        }
        joint.Initialise(connectedBody, maximumJointStrength, dofAngleLimits);
        return joint;
    }

    private void Start()
    {
        intialOriginRotation = Quaternion.Inverse(transform.rotation) * joint.connectedBody.transform.rotation;
    }

    private void FixedUpdate()
    {
        Quaternion updatedOriginRotation = joint.connectedBody.transform.rotation * Quaternion.Inverse(intialOriginRotation);

        Vector3 primaryAxis = updatedOriginRotation * joint.axis; // Stays fixed with parent.
        Vector3 secondaryAxis = transform.rotation * joint.secondaryAxis;
        Vector3 tertiaryAxis = transform.rotation * Vector3.Cross(joint.axis, joint.secondaryAxis);

        Vector3 primaryTorque = maximumJointStrength * excitations[0] * primaryAxis;
        Vector3 secondaryTorque = maximumJointStrength * excitations[1] * secondaryAxis;
        Vector3 tertiaryTorque = maximumJointStrength * excitations[2] * tertiaryAxis;

        GetComponent<Rigidbody>().AddTorque(primaryTorque + secondaryTorque + tertiaryTorque);

        angles[0] = Vector3.SignedAngle(transform.rotation * primaryAxis, updatedOriginRotation * primaryAxis, primaryAxis);
        angles[1] = Vector3.SignedAngle(transform.rotation * secondaryAxis, updatedOriginRotation * secondaryAxis, secondaryAxis);
        angles[2] = Vector3.SignedAngle(transform.rotation * tertiaryAxis, updatedOriginRotation * tertiaryAxis, tertiaryAxis);

        UpdateSensors();
        ApplyEffectors();

        // Debug.
        if (resetExcitations)
        {
            debugPrimaryExcitation = 0f;
            debugSecondaryExcitation = 0f;
            debugTertiaryExcitation = 0f;
        }
        if (effectors != null && effectors.Length > 0)
            effectors[0].Excitation = debugPrimaryExcitation;
        if (effectors != null && effectors.Length > 1)
            effectors[1].Excitation = debugSecondaryExcitation;
        if (effectors != null && effectors.Length > 2)
            effectors[2].Excitation = debugTertiaryExcitation;

        primaryTorqueDisplay = primaryTorque;
        secondaryTorqueDisplay = secondaryTorque;
        tertiaryTorqueDisplay = tertiaryTorque;
    }

    protected void InitialiseDOFs(float[] dofAngleLimits)
    {
        if (dofAngleLimits?.Length != DegreesOfFreedom)
            throw new System.Exception("Cannot initialise joint - number of DOF Angle Limits must equal " + DegreesOfFreedom.ToString());

        this.dofAngleLimits = dofAngleLimits;
        sensors = new JointAngleSensor[DegreesOfFreedom];
        for (int i = 0; i < DegreesOfFreedom; i++)
        {
            sensors[i] = new JointAngleSensor();
        }
        effectors = new JointAngleEffector[DegreesOfFreedom];
        for (int i = 0; i < DegreesOfFreedom; i++)
        {
            effectors[i] = new JointAngleEffector();
        }
    }

    private void UpdateSensors()
    {
        if (sensors != null)
        {
            for (int dofIndex = 0; dofIndex < sensors.Length; dofIndex++)
            {
                sensors[dofIndex].OutputValue = angles[dofIndex] / dofAngleLimits[dofIndex];
            }
        }
    }

    private void ApplyEffectors()
    {
        if (effectors != null)
        {
            for (int dofIndex = 0; dofIndex < effectors.Length; dofIndex++)
            {
                excitations[dofIndex] = effectors[dofIndex].Excitation;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(transform.TransformPoint(joint.anchor), Mathf.Min(transform.localScale.x / 10, transform.localScale.y / 10));

        Debug.DrawLine(transform.TransformPoint(joint.anchor), transform.TransformPoint(joint.anchor) + primaryTorqueDisplay / maximumJointStrength, Color.magenta);
        Debug.DrawLine(transform.TransformPoint(joint.anchor), transform.TransformPoint(joint.anchor) + secondaryTorqueDisplay / maximumJointStrength, Color.yellow);
        Debug.DrawLine(transform.TransformPoint(joint.anchor), transform.TransformPoint(joint.anchor) + tertiaryTorqueDisplay / maximumJointStrength, Color.cyan);
    }
}
