using UnityEngine;

public abstract class JointBase : MonoBehaviour
{
    protected abstract JointType TypeOfJoint { get; }
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
    public bool useDebugExcitations;
    [Range(-1f, 1f)]
    public float debugPrimaryExcitation;
    [Range(-1f, 1f)]
    public float debugSecondaryExcitation;
    [Range(-1f, 1f)]
    public float debugTertiaryExcitation;
    public bool resetExcitations;
    public Vector3 primaryStrengthDisplay;
    public Vector3 secondaryStrengthDisplay;
    public Vector3 tertiaryStrengthDisplay;

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
            case JointType.Universal: // broken
                // joint = gameObject.AddComponent<UniversalJoint>();
                joint = gameObject.AddComponent<RevoluteJoint>();
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

        float primaryExcitation = useDebugExcitations ? debugPrimaryExcitation : (float.IsNaN(excitations[0]) ? 0f : excitations[0]);
        float secondaryExcitation = useDebugExcitations ? debugSecondaryExcitation : (float.IsNaN(excitations[1]) ? 0f : excitations[1]);
        float tertiaryExcitation = useDebugExcitations ? debugTertiaryExcitation : (float.IsNaN(excitations[2]) ? 0f : excitations[2]);

        // Vector3 primaryTorque = maximumJointStrength * primaryExcitation * primaryAxis;
        // Vector3 secondaryTorque = maximumJointStrength * secondaryExcitation * secondaryAxis;
        // Vector3 tertiaryTorque = maximumJointStrength * tertiaryExcitation * tertiaryAxis;

        // GetComponent<Rigidbody>().AddTorque((primaryTorque + secondaryTorque + tertiaryTorque) * JointParameters.TorqueMultiplier);

        joint.targetAngularVelocity = new Vector3(
            primaryExcitation * JointParameters.AngularVelocityMultiplier,
            secondaryExcitation * JointParameters.AngularVelocityMultiplier,
            tertiaryExcitation * JointParameters.AngularVelocityMultiplier
        );

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

        primaryStrengthDisplay = joint.targetAngularVelocity.x * primaryAxis;
        secondaryStrengthDisplay = joint.targetAngularVelocity.y * secondaryAxis;
        tertiaryStrengthDisplay = joint.targetAngularVelocity.z * tertiaryAxis;
    }

    protected void InitialiseDOFs(float[] dofAngleLimits)
    {
        int dof = TypeOfJoint.DegreesOfFreedom();
        if (dofAngleLimits?.Length != dof)
            throw new System.Exception("Cannot initialise joint - number of DOF Angle Limits must equal " + dof.ToString());

        this.dofAngleLimits = dofAngleLimits;
        sensors = new JointAngleSensor[dof];
        for (int i = 0; i < dof; i++)
        {
            sensors[i] = new JointAngleSensor();
        }
        effectors = new JointAngleEffector[dof];
        for (int i = 0; i < dof; i++)
        {
            effectors[i] = new JointAngleEffector();
        }
    }

    protected void ApplyCommonJointSettings(Rigidbody connectedBody, float maximumJointStrength)
    {
        joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = connectedBody;
        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        // joint.projectionDistance = 0.01f;
        // joint.projectionAngle = 5f;
        // joint.enablePreprocessing = false;
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        joint.slerpDrive = new JointDrive
        {
            positionSpring = 0.01f,
            positionDamper = 100000f,
            maximumForce = maximumJointStrength * JointParameters.StrengthMultiplier
        };
    }

    private void UpdateSensors()
    {
        if (sensors != null)
        {
            for (int dofIndex = 0; dofIndex < sensors.Length; dofIndex++)
            {
                sensors[dofIndex].OutputValue = dofAngleLimits[dofIndex] == 0 ? 0 : angles[dofIndex] / dofAngleLimits[dofIndex];
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

        Debug.DrawLine(transform.TransformPoint(joint.anchor), transform.TransformPoint(joint.anchor) + primaryStrengthDisplay / maximumJointStrength, Color.magenta);
        Debug.DrawLine(transform.TransformPoint(joint.anchor), transform.TransformPoint(joint.anchor) + secondaryStrengthDisplay / maximumJointStrength, Color.yellow);
        Debug.DrawLine(transform.TransformPoint(joint.anchor), transform.TransformPoint(joint.anchor) + tertiaryStrengthDisplay / maximumJointStrength, Color.cyan);
    }
}
