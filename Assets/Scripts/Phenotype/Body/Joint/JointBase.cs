using System.Collections.Generic;
using UnityEngine;

public abstract class JointBase : MonoBehaviour
{
    protected abstract JointType TypeOfJoint { get; }
    protected ConfigurableJoint joint;
    protected float maximumJointStrength;
    protected List<float> dofAngleLimits;
    public List<JointAngleSensor> sensors;
    public List<JointAngleEffector> effectors;

    public Vector3 angles;
    public Vector3 limits;
    private Vector3 excitations;
    private Vector3 previousAngularVelocity;

    public abstract void ApplySpecificJointSettings(List<float> dofAngleLimits);

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

    public static JointBase CreateJoint(JointType jointType, GameObject gameObject, Rigidbody connectedBody, float maximumJointStrength, List<float> dofAngleLimits)
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
                joint = gameObject.AddComponent<RigidJoint>();
                dofAngleLimits = new List<float> { };
                break;
            case JointType.BendTwist:
                joint = gameObject.AddComponent<BendTwistJoint>();
                break;
            case JointType.TwistBend:
                joint = gameObject.AddComponent<TwistBendJoint>();
                break;
            case JointType.Spherical: // broken
                // joint = gameObject.AddComponent<SphericalJoint>();
                joint = gameObject.AddComponent<RigidJoint>();
                dofAngleLimits = new List<float> { };
                break;
            default:
                throw new System.ArgumentException("Unknown joint type '" + jointType + "'");
        }

        joint.maximumJointStrength = maximumJointStrength;
        joint.InitialiseDOFs(dofAngleLimits);
        joint.InitialiseJoint(connectedBody, maximumJointStrength);
        joint.ApplySpecificJointSettings(dofAngleLimits);

        return joint;
    }

    private void Start()
    {
        intialOriginRotation = Quaternion.Inverse(transform.rotation) * joint.connectedBody.transform.rotation;

        for (int i = 0; i < dofAngleLimits.Count; i++)
            limits[i] = dofAngleLimits[i];
    }

    private void FixedUpdate()
    {
        Quaternion updatedOriginRotation = joint.connectedBody.transform.rotation * Quaternion.Inverse(intialOriginRotation);

        // Calculate axes and axis angles.
        Vector3 primaryAxis = updatedOriginRotation * joint.axis; // Stays fixed with parent.
        Vector3 secondaryAxis = transform.rotation * joint.secondaryAxis;
        Vector3 tertiaryAxis = transform.rotation * Vector3.Cross(joint.axis, joint.secondaryAxis);

        Quaternion diff = updatedOriginRotation * Quaternion.Inverse(transform.rotation);
        float angle;
        Vector3 rotationAxis;
        diff.ToAngleAxis(out angle, out rotationAxis);
        Vector3 projX = Vector3.Project(rotationAxis.normalized, primaryAxis.normalized);
        Vector3 projY = Vector3.Project(rotationAxis.normalized, secondaryAxis.normalized);
        Vector3 projZ = Vector3.Project(rotationAxis.normalized, tertiaryAxis.normalized);
        angles[0] = angle * projX.magnitude * -Mathf.Sign(Vector3.Dot(projX, primaryAxis));
        angles[1] = angle * projY.magnitude * -Mathf.Sign(Vector3.Dot(projY, secondaryAxis));
        angles[2] = angle * projZ.magnitude * -Mathf.Sign(Vector3.Dot(projZ, tertiaryAxis));

        // Use excitation values to set joint torques.
        Vector3 e = new Vector3(
            useDebugExcitations ? debugPrimaryExcitation : (float.IsNaN(excitations[0]) ? 0f : excitations[0]),
            useDebugExcitations ? debugSecondaryExcitation : (float.IsNaN(excitations[1]) ? 0f : excitations[1]),
            useDebugExcitations ? debugTertiaryExcitation : (float.IsNaN(excitations[2]) ? 0f : excitations[2])
        );

        float maxVelocity = 10f * JointParameters.AngularVelocityMultiplier;
        joint.targetAngularVelocity = Vector3.Lerp(previousAngularVelocity, e * maxVelocity, Time.deltaTime * JointParameters.SmoothingMultiplier);
        previousAngularVelocity = joint.targetAngularVelocity;

        UpdateSensors();
        ApplyEffectors();

        // Debug.
        if (resetExcitations)
        {
            debugPrimaryExcitation = 0f;
            debugSecondaryExcitation = 0f;
            debugTertiaryExcitation = 0f;
        }

        primaryStrengthDisplay = e[0] * primaryAxis;
        secondaryStrengthDisplay = e[1] * secondaryAxis;
        tertiaryStrengthDisplay = e[2] * tertiaryAxis;
    }

    protected void InitialiseDOFs(List<float> dofAngleLimits)
    {
        int dof = TypeOfJoint.DegreesOfFreedom();
        if (dofAngleLimits?.Count != dof)
            throw new System.Exception("Cannot initialise joint - number of DOF Angle Limits must equal " + dof.ToString());

        this.dofAngleLimits = dofAngleLimits;
        sensors = new List<JointAngleSensor>();
        for (int i = 0; i < dof; i++)
        {
            sensors.Add(new JointAngleSensor());
        }
        effectors = new List<JointAngleEffector>();
        for (int i = 0; i < dof; i++)
        {
            effectors.Add(new JointAngleEffector());
        }
    }

    protected void InitialiseJoint(Rigidbody connectedBody, float maximumJointStrength)
    {
        joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = connectedBody;
        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        joint.enablePreprocessing = false;
        joint.rotationDriveMode = RotationDriveMode.XYAndZ;
        joint.angularXDrive = new JointDrive
        {
            positionSpring = 10f,
            positionDamper = maximumJointStrength * JointParameters.StrengthMultiplier * 10f,
            maximumForce = maximumJointStrength * JointParameters.StrengthMultiplier * 100f
        };
        joint.angularYZDrive = new JointDrive
        {
            positionSpring = 10f,
            positionDamper = maximumJointStrength * JointParameters.StrengthMultiplier * 10f,
            maximumForce = maximumJointStrength * JointParameters.StrengthMultiplier * 100f
        };
    }

    private void UpdateSensors()
    {
        if (sensors != null)
        {
            for (int dofIndex = 0; dofIndex < sensors.Count; dofIndex++)
            {
                sensors[dofIndex].OutputValue = dofAngleLimits[dofIndex] == 0 ? 0 : angles[dofIndex] / dofAngleLimits[dofIndex];
            }
        }
    }

    private void ApplyEffectors()
    {
        if (effectors != null)
        {
            for (int dofIndex = 0; dofIndex < effectors.Count; dofIndex++)
            {
                float excitation = effectors[dofIndex].GetExcitation();
                excitations[dofIndex] = float.IsNaN(excitation) ? 0f : excitation;
            }
        }
    }

    // private float GetWeaknessFactor(int dofIndex, float signOfDriveDirection)
    // {
    //     float weaknessZoneSize = 10f;
    //     float facingLimit = signOfDriveDirection * dofAngleLimits[dofIndex];
    //     float weaknessZoneStart = facingLimit - signOfDriveDirection * Mathf.Min(Mathf.Abs(facingLimit), weaknessZoneSize);
    //     float howCloseToEdge = Mathf.InverseLerp(weaknessZoneStart, facingLimit, angles[dofIndex]);
    //     float weaknessFactor = 1f - Mathf.Pow(howCloseToEdge, 3);
    //     return weaknessFactor;
    // }

    private float GetAngleAroundAxis(Vector3 planeNormal, Vector3 a, Vector3 b)
    {
        Vector3 projectionA = Vector3.ProjectOnPlane(a, planeNormal);
        Vector3 projectionB = Vector3.ProjectOnPlane(b, planeNormal);
        return Vector3.Angle(projectionA, projectionB);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(transform.TransformPoint(joint.anchor), Mathf.Min(transform.localScale.x / 10, transform.localScale.y / 10));

        Debug.DrawLine(transform.TransformPoint(joint.anchor), transform.TransformPoint(joint.anchor) + primaryStrengthDisplay, Color.red);
        Debug.DrawLine(transform.TransformPoint(joint.anchor), transform.TransformPoint(joint.anchor) + secondaryStrengthDisplay, Color.green);
        Debug.DrawLine(transform.TransformPoint(joint.anchor), transform.TransformPoint(joint.anchor) + tertiaryStrengthDisplay, Color.blue);
    }
}
