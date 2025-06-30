using Unity.Collections;
using Unity.Mathematics;
using Unity.Physics;

public static class JointFactory
{
    private static readonly float springFrequency = 20f;
    private static readonly float dampingRatio = 0.8f;
    private static readonly float maxImpulseOfMotor = 10f; // TODO: Proportional to minimum cross-sectional area between parent and child limb.

    private static readonly Constraint linearConstraint = new()
    {
        ConstrainedAxes = new bool3(x: true, y: true, z: true),
        Type = ConstraintType.Linear,
        Min = 0f,
        Max = 0f,
        SpringFrequency = springFrequency,
        DampingRatio = dampingRatio,
        MaxImpulse = new float3(maxImpulseOfMotor, maxImpulseOfMotor, maxImpulseOfMotor),
        Target = float3.zero
    };

    public static PhysicsJoint CreateJoint(
        mycoolfin.TheSimsulator.Sims.JointType jointType,
        float4x4 referenceLimbLocalTransform,
        float4x4 attachedLimbLocalTransform,
        float3 referenceLimbSpaceAnchor,
        float3 referenceLimbSpaceXAxis,
        float3 referenceLimbSpaceYAxis,
        float3 referenceLimbSpaceZAxis,
        float3 angleLimits
    )
    {
        bool flippedHandedness = math.dot(math.cross(referenceLimbSpaceXAxis, referenceLimbSpaceYAxis), referenceLimbSpaceZAxis) < 0f;

        BodyFrame bodyAFromJoint = new()
        {
            Position = referenceLimbSpaceAnchor,
            Axis = referenceLimbSpaceXAxis * (flippedHandedness ? -1f : 1f), // TODO: Specific axes for different joint types.
            PerpendicularAxis = referenceLimbSpaceYAxis,
        };

        float4x4 T_B_from_A = math.mul(math.inverse(attachedLimbLocalTransform), referenceLimbLocalTransform);

        BodyFrame bodyBFromJoint = new()
        {
            Position = math.transform(T_B_from_A, bodyAFromJoint.Position),
            Axis = math.mul(T_B_from_A, new float4(bodyAFromJoint.Axis, 0)).xyz,
            PerpendicularAxis = math.mul(T_B_from_A, new float4(bodyAFromJoint.PerpendicularAxis, 0)).xyz,
        };

        PhysicsJoint joint = new()
        {
            BodyAFromJoint = bodyAFromJoint,
            BodyBFromJoint = bodyBFromJoint
        };

        FixedList512Bytes<Constraint> constraints = jointType switch
        {
            mycoolfin.TheSimsulator.Sims.JointType.Rigid => GetRigidJointConstraints(),
            mycoolfin.TheSimsulator.Sims.JointType.Revolute => GetRevoluteJointConstraints(angleLimits),
            _ => throw new System.ArgumentException($"Unsupported joint type: {jointType}"),
        };
        joint.SetConstraints(constraints);
        return joint;
    }

    public static FixedList512Bytes<Constraint> GetRigidJointConstraints()
    {
        Constraint angularConstraint = new()
        {
            ConstrainedAxes = new bool3(x: true, y: true, z: true),
            Type = ConstraintType.Angular,
            Min = 0f,
            Max = 0f,
            SpringFrequency = springFrequency,
            DampingRatio = dampingRatio,
            MaxImpulse = new float3(maxImpulseOfMotor, maxImpulseOfMotor, maxImpulseOfMotor),
            Target = float3.zero
        };
        return new()
        {
            linearConstraint,
            angularConstraint
        };
    }

    public static FixedList512Bytes<Constraint> GetRevoluteJointConstraints(float3 angleLimits)
    {
        Constraint primaryAxisMotorConstraint = new()
        {
            ConstrainedAxes = new bool3(x: true, y: false, z: false),
            Type = ConstraintType.RotationMotor,
            Min = -angleLimits.x,
            Max = angleLimits.x,
            SpringFrequency = springFrequency,
            DampingRatio = dampingRatio,
            MaxImpulse = new float3(maxImpulseOfMotor, maxImpulseOfMotor, maxImpulseOfMotor),
            Target = new float3(-0.01f, -0.01f, -0.01f)
        };
        Constraint fixYZAnglesConstraint = new()
        {
            ConstrainedAxes = new bool3(x: false, y: true, z: true),
            Type = ConstraintType.Angular,
            Min = 0f,
            Max = 0f,
            SpringFrequency = springFrequency,
            DampingRatio = dampingRatio,
            MaxImpulse = new float3(maxImpulseOfMotor, maxImpulseOfMotor, maxImpulseOfMotor),
            Target = new float3(-0.01f, -0.01f, -0.01f)
        };
        return new()
        {
            primaryAxisMotorConstraint,
            fixYZAnglesConstraint,
            linearConstraint,
        };
    }
}
