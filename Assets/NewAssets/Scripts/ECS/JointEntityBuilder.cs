using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

public static class JointEntityBuilder
{
    private const float SPRING_FREQUENCY = 20f;
    private const float DAMPING_RATIO = 0.8f;
    private const float BASE_MAX_MOTOR_IMPULSE = 10f;

    public static EntityArchetype CreateJointArchetype(ref SystemState state)
    {
        return state.EntityManager.CreateArchetype(
            typeof(PhysicsWorldIndex),
            typeof(PhysicsConstrainedBodyPair),
            typeof(PhysicsJoint),
            typeof(PhysicsJointCompanion)
        );
    }

    // BUG: Some joint configurations cause the attached limb to do a 360deg flip when the simulation starts.
    // NOTE: DOTS Physics 1.3 only appears to support up to three constraints per joint.
    // Until this changes, we have to create two joint entities for more complex joints.
    public static void CreateJointEntities(
        EntityCommandBuffer.ParallelWriter ecb,
        int sortKey,
        EntityArchetype jointArchetype,
        mycoolfin.TheSimsulator.Sims.JointType jointType,
        Entity referenceLimbEntity,
        Entity attachedLimbEntity,
        uint physicsWorldIndex,
        float4x4 referenceLimbLocalTransform,
        float4x4 attachedLimbLocalTransform,
        float3 referenceLimbSpaceAnchor,
        float3 referenceLimbSpaceXAxis,
        float3 referenceLimbSpaceYAxis,
        float3 referenceLimbSpaceZAxis,
        float3 angleLimitsDegrees,
        float maxMotorImpulseScaleFactor
    )
    {
        float3 angleLimits = math.radians(angleLimitsDegrees);

        Entity jointEntity1 = ecb.CreateEntity(sortKey, jointArchetype);
        Entity jointEntity2 = Entity.Null;

        PhysicsJoint CreatePhysicsJoint(bool swapXZ = false) => CreateUnconstrainedJoint(
            referenceLimbSpaceAnchor,
            swapXZ ? referenceLimbSpaceZAxis : referenceLimbSpaceXAxis,
            referenceLimbSpaceYAxis,
            swapXZ ? referenceLimbSpaceXAxis : referenceLimbSpaceZAxis,
            referenceLimbLocalTransform,
            attachedLimbLocalTransform
        );
        Constraint CreateConstraint(ConstraintType type, bool3 constrainedAxes, float angleLimit)
        {
            float maxImpulseOfMotor = BASE_MAX_MOTOR_IMPULSE * maxMotorImpulseScaleFactor;
            return new()
            {
                ConstrainedAxes = constrainedAxes,
                Type = type,
                Min = -angleLimit,
                Max = angleLimit,
                SpringFrequency = SPRING_FREQUENCY,
                DampingRatio = DAMPING_RATIO,
                MaxImpulse = new float3(maxImpulseOfMotor, maxImpulseOfMotor, maxImpulseOfMotor),
                Target = float3.zero
            };
        }

        PhysicsJoint physicsJoint1;
        PhysicsJoint physicsJoint2;
        switch (jointType)
        {
            case mycoolfin.TheSimsulator.Sims.JointType.Rigid:
                physicsJoint1 = CreatePhysicsJoint();
                physicsJoint1.SetConstraints(new()
                {
                    CreateConstraint(ConstraintType.Angular,        new bool3(true, true, true),    0f),
                    CreateConstraint(ConstraintType.Linear,         new bool3(true, true, true),    0f),
                });
                ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);
                break;
            case mycoolfin.TheSimsulator.Sims.JointType.Revolute:
                physicsJoint1 = CreatePhysicsJoint();
                physicsJoint1.SetConstraints(new()
                {
                    CreateConstraint(ConstraintType.RotationMotor,  new bool3(true, false, false),  angleLimits.x),
                    CreateConstraint(ConstraintType.Angular,        new bool3(false, true, true),   0f),
                    CreateConstraint(ConstraintType.Linear,         new bool3(true, true, true),    0f)
                });
                ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);
                break;
            case mycoolfin.TheSimsulator.Sims.JointType.Twist:
                physicsJoint1 = CreatePhysicsJoint();
                physicsJoint1.SetConstraints(new()
                {
                    CreateConstraint(ConstraintType.RotationMotor,  new bool3(false, false, true),  angleLimits.x),
                    CreateConstraint(ConstraintType.Angular,        new bool3(true, true, false),   0f),
                    CreateConstraint(ConstraintType.Linear,         new bool3(true, true, true),    0f)
                });
                ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);
                break;
            case mycoolfin.TheSimsulator.Sims.JointType.BendTwist:
                // Fixed constraints.
                physicsJoint1 = CreatePhysicsJoint();
                physicsJoint1.SetConstraints(new()
                {
                    CreateConstraint(ConstraintType.Angular,        new bool3(false, true, false),  0f),
                    CreateConstraint(ConstraintType.Linear,         new bool3(true, true, true),    0f)
                });
                ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);
                // Motor constraints.
                jointEntity2 = ecb.CreateEntity(sortKey, jointArchetype);
                physicsJoint2 = CreatePhysicsJoint();
                physicsJoint2.SetConstraints(new()
                {
                    CreateConstraint(ConstraintType.RotationMotor,  new bool3(true, false, false),  angleLimits.x),
                    CreateConstraint(ConstraintType.RotationMotor,  new bool3(false, false, true),  angleLimits.z)
                });
                ecb.SetComponent(sortKey, jointEntity2, physicsJoint2);
                break;
            case mycoolfin.TheSimsulator.Sims.JointType.TwistBend:
                // Fixed constraints.
                physicsJoint1 = CreatePhysicsJoint(swapXZ: true);
                physicsJoint1.SetConstraints(new()
                {
                    CreateConstraint(ConstraintType.Angular,        new bool3(false, true, false),  0f),
                    CreateConstraint(ConstraintType.Linear,         new bool3(true, true, true),    0f)
                });
                ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);
                // Motor constraints.
                jointEntity2 = ecb.CreateEntity(sortKey, jointArchetype);
                physicsJoint2 = CreatePhysicsJoint(swapXZ: true);
                physicsJoint2.SetConstraints(new()
                {
                    CreateConstraint(ConstraintType.RotationMotor,  new bool3(false, false, true),  angleLimits.z),
                    CreateConstraint(ConstraintType.RotationMotor,  new bool3(true, false, false),  angleLimits.x)
                });
                ecb.SetComponent(sortKey, jointEntity2, physicsJoint2);
                break;
            case mycoolfin.TheSimsulator.Sims.JointType.Universal:
                // Fixed constraints.
                physicsJoint1 = CreatePhysicsJoint();
                physicsJoint1.SetConstraints(new()
                {
                    CreateConstraint(ConstraintType.Angular,        new bool3(false, false, true),  0f),
                    CreateConstraint(ConstraintType.Linear,         new bool3(true, true, true),    0f)
                });
                ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);
                // Motor constraints.
                jointEntity2 = ecb.CreateEntity(sortKey, jointArchetype);
                physicsJoint2 = CreatePhysicsJoint();
                physicsJoint2.SetConstraints(new()
                {
                    CreateConstraint(ConstraintType.RotationMotor,  new bool3(true, false, false),  angleLimits.x),
                    CreateConstraint(ConstraintType.RotationMotor,  new bool3(false, true, false),  angleLimits.y)
                });
                ecb.SetComponent(sortKey, jointEntity2, physicsJoint2);
                break;
            case mycoolfin.TheSimsulator.Sims.JointType.Spherical:
                // Fixed constraints.
                physicsJoint1 = CreatePhysicsJoint();
                physicsJoint1.SetConstraints(new()
                {
                    CreateConstraint(ConstraintType.Linear,         new bool3(true, true, true),    0f)
                });
                ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);
                // Motor constraints.
                jointEntity2 = ecb.CreateEntity(sortKey, jointArchetype);
                physicsJoint2 = CreatePhysicsJoint();
                physicsJoint2.SetConstraints(new()
                {
                    CreateConstraint(ConstraintType.RotationMotor,  new bool3(true, false, false),  angleLimits.x),
                    CreateConstraint(ConstraintType.RotationMotor,  new bool3(false, true, false),  angleLimits.y),
                    CreateConstraint(ConstraintType.RotationMotor,  new bool3(false, false, true),  angleLimits.z)
                });
                ecb.SetComponent(sortKey, jointEntity2, physicsJoint2);
                break;
            default:
                break;
        }

        ecb.SetSharedComponent(sortKey, jointEntity1, new PhysicsWorldIndex(physicsWorldIndex));
        ecb.SetComponent(sortKey, jointEntity1, new PhysicsConstrainedBodyPair(referenceLimbEntity, attachedLimbEntity, false));
        if (jointEntity2 != Entity.Null)
        {
            ecb.SetSharedComponent(sortKey, jointEntity2, new PhysicsWorldIndex(physicsWorldIndex));
            ecb.SetComponent(sortKey, jointEntity2, new PhysicsConstrainedBodyPair(referenceLimbEntity, attachedLimbEntity, false));

            ecb.AppendToBuffer(sortKey, jointEntity1, new PhysicsJointCompanion() { JointEntity = jointEntity2 });
            ecb.AppendToBuffer(sortKey, jointEntity2, new PhysicsJointCompanion() { JointEntity = jointEntity1 });
        }
    }

    private static PhysicsJoint CreateUnconstrainedJoint(
        float3 referenceLimbSpaceAnchor,
        float3 referenceLimbSpaceXAxis,
        float3 referenceLimbSpaceYAxis,
        float3 referenceLimbSpaceZAxis,
        float4x4 referenceLimbLocalTransform,
        float4x4 attachedLimbLocalTransform
    )
    {
        bool flippedHandedness = math.dot(math.cross(referenceLimbSpaceXAxis, referenceLimbSpaceYAxis), referenceLimbSpaceZAxis) < 0f;

        BodyFrame bodyAFromJoint = new()
        {
            Position = referenceLimbSpaceAnchor,
            Axis = referenceLimbSpaceXAxis * (flippedHandedness ? -1f : 1f),
            PerpendicularAxis = referenceLimbSpaceYAxis * (flippedHandedness ? -1f : 1f),
        };

        float4x4 T_B_from_A = math.mul(math.inverse(attachedLimbLocalTransform), referenceLimbLocalTransform);

        BodyFrame bodyBFromJoint = new()
        {
            Position = math.transform(T_B_from_A, bodyAFromJoint.Position),
            Axis = math.mul(T_B_from_A, new float4(bodyAFromJoint.Axis, 0)).xyz,
            PerpendicularAxis = math.mul(T_B_from_A, new float4(bodyAFromJoint.PerpendicularAxis, 0)).xyz,
        };

        return new()
        {
            BodyAFromJoint = bodyAFromJoint,
            BodyBFromJoint = bodyBFromJoint
        };
    }
}
