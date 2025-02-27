﻿using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class JointBase : MonoBehaviour
{
    protected abstract JointType TypeOfJoint { get; }
    protected ConfigurableJoint joint;
    protected float maximumJointStrength;
    protected List<float> dofAngleLimits;
    protected bool switchPrimaryAndSecondaryAxes;
    public List<Sensor> sensors;
    public List<Effector> effectors;

    private float maxForce;
    private JointDrive jointDrive;

    [SerializeField] private Vector3 angles;
    private Vector3 angleTargets;
    private Vector3 smoothingVelocity;
    [SerializeField] private Vector3 smoothedAngleTargets;
    [SerializeField] private Vector3 limits;
    [SerializeField] private Vector3 excitations;

    protected abstract void ApplySpecificJointSettings();

    private Vector3 primaryAxis;
    private Vector3 secondaryAxis;
    private Vector3 tertiaryAxis;
    private Vector3 primaryAnglePlaneNormal;
    private Vector3 secondaryAnglePlaneNormal;
    private Vector3 tertiaryAnglePlaneNormal;
    private Quaternion initialRotationRelativeToConnectedBody;

    // Debug.
    public bool useDebugExcitations;
    [Range(-1f, 1f)]
    public float debugPrimaryExcitation;
    [Range(-1f, 1f)]
    public float debugSecondaryExcitation;
    [Range(-1f, 1f)]
    public float debugTertiaryExcitation;
    public bool resetExcitations;

    public static JointBase CreateJoint(JointDefinition jointDefinition, GameObject gameObject, Rigidbody connectedBody, float maximumJointStrength, List<float> dofAngleLimits, bool reflectedX, bool reflectedY, bool reflectedZ)
    {
        JointBase j;
        switch (jointDefinition.Type)
        {
            case JointType.Rigid:
                j = gameObject.AddComponent<RigidJoint>();
                break;
            case JointType.Revolute:
                j = gameObject.AddComponent<RevoluteJoint>();
                break;
            case JointType.Twist:
                j = gameObject.AddComponent<TwistJoint>();
                break;
            case JointType.Universal:
                j = gameObject.AddComponent<UniversalJoint>();
                break;
            case JointType.BendTwist:
                j = gameObject.AddComponent<BendTwistJoint>();
                break;
            case JointType.TwistBend:
                j = gameObject.AddComponent<TwistBendJoint>();
                break;
            case JointType.Spherical:
                j = gameObject.AddComponent<SphericalJoint>();
                break;
            default:
                throw new System.ArgumentException("Unknown joint type '" + jointDefinition.Type + "'");
        }

        j.maximumJointStrength = maximumJointStrength;

        // Constrain the degrees of freedom depending on the joint type.
        j.dofAngleLimits = dofAngleLimits.Take(j.TypeOfJoint.DegreesOfFreedom()).ToList();

        j.InitialiseSensors();
        j.InitialiseEffectors(jointDefinition.AxisDefinitions.Select(a => a.InputDefinition).ToList().AsReadOnly());
        j.InitialiseJoint(connectedBody, maximumJointStrength);
        j.ApplySpecificJointSettings();
        j.ApplyReflections(reflectedX, reflectedY, reflectedZ);

        return j;
    }

    private void Start()
    {
        initialRotationRelativeToConnectedBody = Quaternion.Inverse(joint.connectedBody.transform.rotation) * transform.rotation;

        for (int i = 0; i < dofAngleLimits.Count; i++)
            limits[i] = dofAngleLimits[i];

        primaryAxis = switchPrimaryAndSecondaryAxes ? joint.secondaryAxis : joint.axis;
        secondaryAxis = switchPrimaryAndSecondaryAxes ? joint.axis : joint.secondaryAxis;
        tertiaryAxis = Vector3.Cross(primaryAxis, secondaryAxis);
        primaryAnglePlaneNormal = Vector3.Cross(primaryAxis, secondaryAxis);
        secondaryAnglePlaneNormal = Vector3.Cross(tertiaryAxis, secondaryAxis);
        tertiaryAnglePlaneNormal = Vector3.Cross(primaryAxis, tertiaryAxis);
    }

    private void FixedUpdate()
    {
        if (!joint.connectedBody)
        {
            Detach();
            return;
        }

        Quaternion currentRotationRelativeToConnectedBody = Quaternion.Inverse(joint.connectedBody.transform.rotation) * transform.rotation;
        Quaternion relativeRotationFromOrigin = Quaternion.Inverse(initialRotationRelativeToConnectedBody) * currentRotationRelativeToConnectedBody;

        angles = GetCurrentAngles(relativeRotationFromOrigin);

        Vector3 e = new(
            useDebugExcitations ? debugPrimaryExcitation : (float.IsNaN(excitations[0]) ? 0f : excitations[0]),
            useDebugExcitations ? debugSecondaryExcitation : (float.IsNaN(excitations[1]) ? 0f : excitations[1]),
            useDebugExcitations ? debugTertiaryExcitation : (float.IsNaN(excitations[2]) ? 0f : excitations[2])
        );

        angleTargets = new(
            ConvertExcitationToAngle(e[0], limits[0]),
            ConvertExcitationToAngle(e[1], limits[1]),
            ConvertExcitationToAngle(e[2], limits[2])
        );

        smoothedAngleTargets = Vector3.SmoothDamp(smoothedAngleTargets, angleTargets, ref smoothingVelocity, ParameterManager.Instance.Joint.SmoothingFactor);

        joint.targetRotation = Quaternion.Euler(
            switchPrimaryAndSecondaryAxes ? smoothedAngleTargets[1] : smoothedAngleTargets[0],
            switchPrimaryAndSecondaryAxes ? smoothedAngleTargets[0] : smoothedAngleTargets[1],
            smoothedAngleTargets[2]
        );

        HandleDislocation();

        UpdateSensors();
        ApplyEffectors();

        // Debug.
        if (resetExcitations)
        {
            resetExcitations = false;
            debugPrimaryExcitation = 0f;
            debugSecondaryExcitation = 0f;
            debugTertiaryExcitation = 0f;
        }
    }

    private void OnJointBreak(float breakForce)
    {
        Detach();
    }

    private void Detach()
    {
        GetComponentInParent<Phenotype>().DetachLimb(GetComponent<Limb>());
        Destroy(this);
    }

    // A joint is 'dislocated' when its anchor becomes separated from its connected anchor.
    // This can generate angular momentum out of nowhere, resulting in physically unrealistic behaviour.
    // We can ameliorate the issue somewhat by reducing the joint maximum force if we detect dislocation.
    private void HandleDislocation()
    {
        Vector3 diff = transform.TransformPoint(joint.anchor) - joint.connectedBody.transform.TransformPoint(joint.connectedAnchor);

        float weaknessFactor = 1f - 100f * diff.magnitude / Mathf.Max(1f, Physics.gravity.magnitude);

        jointDrive.maximumForce = Mathf.Lerp(jointDrive.maximumForce, maxForce * Mathf.Max(0f, weaknessFactor), Time.fixedDeltaTime);

        if (joint.angularXDrive.maximumForce != jointDrive.maximumForce)
            joint.angularXDrive = jointDrive;
        if (joint.angularYZDrive.maximumForce != jointDrive.maximumForce)
            joint.angularYZDrive = jointDrive;
    }

    protected void InitialiseSensors()
    {
        sensors = new();
        for (int i = 0; i < dofAngleLimits.Count; i++)
            sensors.Add(new Sensor(SensorType.JointAngle));
    }

    protected void InitialiseEffectors(ReadOnlyCollection<InputDefinition> inputDefinitions)
    {
        effectors = new();
        for (int i = 0; i < dofAngleLimits.Count; i++)
            effectors.Add(new(EffectorType.JointAngle, inputDefinitions));
    }

    protected void InitialiseJoint(Rigidbody connectedBody, float maximumJointStrength)
    {
        joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = connectedBody;
        joint.enableCollision = true;
        joint.enablePreprocessing = true;
        joint.rotationDriveMode = RotationDriveMode.XYAndZ;
        maxForce = maximumJointStrength * ParameterManager.Instance.Joint.StrengthMultiplier;
        jointDrive = new JointDrive
        {
            positionSpring = 10000f * maxForce,
            positionDamper = 1000f * maxForce,
            maximumForce = maxForce
        };
        joint.angularXDrive = jointDrive;
        joint.angularYZDrive = jointDrive;

        // We want joints to break if they start to glitch out. These values may need to be fine tuned.
        joint.breakForce = 500f * maxForce;
        joint.breakTorque = 100f * maxForce;
    }

    protected void ApplyReflections(bool reflectedX, bool reflectedY, bool reflectedZ)
    {
        int reflectedCount = new List<bool>() { reflectedX, reflectedY, reflectedZ }.Count(x => x == true);
        if (reflectedCount == 1 || reflectedCount == 3)
        {
            joint.axis = Vector3.Reflect(joint.axis, Vector3.forward);
            joint.secondaryAxis = Vector3.Reflect(joint.secondaryAxis, Vector3.forward);
        }
    }

    private void UpdateSensors()
    {
        for (int dofIndex = 0; dofIndex < sensors?.Count; dofIndex++)
        {
            if (!sensors[dofIndex].Processor.Emitter.Disabled)
                sensors[dofIndex].Excitation = dofAngleLimits[dofIndex] == 0 ? 0 : angles[dofIndex] / dofAngleLimits[dofIndex];
        }
    }

    private void ApplyEffectors()
    {
        for (int dofIndex = 0; dofIndex < effectors?.Count; dofIndex++)
        {
            float excitation = effectors[dofIndex].Excitation;
            excitations[dofIndex] = float.IsNaN(excitation) ? 0f : excitation;
        }
    }

    private Vector3 GetCurrentAngles(Quaternion relativeRotationFromOrigin)
    {
        Vector3 angles = Vector3.zero;
        angles[0] = -Vector3.SignedAngle(primaryAnglePlaneNormal, Vector3.ProjectOnPlane(relativeRotationFromOrigin * primaryAnglePlaneNormal, primaryAxis), primaryAxis);
        angles[1] = -Vector3.SignedAngle(secondaryAnglePlaneNormal, Vector3.ProjectOnPlane(relativeRotationFromOrigin * secondaryAnglePlaneNormal, secondaryAxis), secondaryAxis);
        angles[2] = -Vector3.SignedAngle(tertiaryAnglePlaneNormal, Vector3.ProjectOnPlane(relativeRotationFromOrigin * tertiaryAnglePlaneNormal, tertiaryAxis), tertiaryAxis);
        return angles;
    }

    private float ConvertExcitationToAngle(float excitation, float angleLimit)
    {
        return Mathf.LerpAngle(0f, angleLimit, Mathf.Abs(excitation)) * Mathf.Sign(excitation);
    }

    private void OnDrawGizmosSelected()
    {
        if (!joint)
            return;

        Gizmos.DrawSphere(transform.TransformPoint(joint.anchor), Mathf.Min(transform.localScale.x / 10, transform.localScale.y / 10));

        Quaternion currentOriginRotationInWorldCoords = joint.connectedBody.transform.rotation * initialRotationRelativeToConnectedBody;

        Vector3 worldPrimaryAxis = currentOriginRotationInWorldCoords * primaryAxis;
        Vector3 worldSecondaryAxis = currentOriginRotationInWorldCoords * secondaryAxis;
        Vector3 worldTertiaryAxis = currentOriginRotationInWorldCoords * tertiaryAxis;

        Vector3 e = new Vector3(
            useDebugExcitations ? debugPrimaryExcitation : (float.IsNaN(excitations[0]) ? 0f : excitations[0]),
            useDebugExcitations ? debugSecondaryExcitation : (float.IsNaN(excitations[1]) ? 0f : excitations[1]),
            useDebugExcitations ? debugTertiaryExcitation : (float.IsNaN(excitations[2]) ? 0f : excitations[2])
        );
        Debug.DrawLine(transform.TransformPoint(joint.anchor), transform.TransformPoint(joint.anchor) + worldPrimaryAxis * e[0], Color.red);
        Debug.DrawLine(transform.TransformPoint(joint.anchor), transform.TransformPoint(joint.anchor) + worldSecondaryAxis * e[1], Color.green);
        Debug.DrawLine(transform.TransformPoint(joint.anchor), transform.TransformPoint(joint.anchor) + worldTertiaryAxis * e[2], Color.blue);
    }
}
