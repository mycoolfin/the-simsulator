using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Builders
{
    using TheSimsulator.Sims.Genotype;
    using TheSimsulator.Sims.Phenotype;
    using Core.ECS.Components.Shared;
    using Components.Phenotype;
    using NeuralNetwork;
    using Systems.Initialisation;

    public static class JointEntityBuilder
    {
        public static EntityArchetype CreateJointArchetype(ref SystemState state)
        {
            return state.EntityManager.CreateArchetype(
                typeof(RootPhenotypeEntity),
                typeof(PhysicsWorldIndex),
                typeof(PhysicsConstrainedBodyPair),
                typeof(PhysicsJoint),
                typeof(PhysicsJointCompanion),
                typeof(JointBreakDistance)
            );
        }

        // Can't schedule IJobEntity from here.
    }

    [BurstCompile]
    public partial struct CreateJointEntityJob : IJobEntity
    {
        // These are only used for motor-type constraints.
        private const float SPRING_FREQUENCY = 10f;
        private const float DAMPING_RATIO = 0.9f;
        private const float BASE_MAX_MOTOR_IMPULSE = 2f;

        public EntityCommandBuffer.ParallelWriter Ecb;
        public EntityArchetype JointArchetype;

        [ReadOnly] public NativeParallelHashMap<PhenotypeLimbKey, Entity>.ReadOnly LimbEntityLookup;
        [ReadOnly] public NativeParallelHashMap<PhenotypeLimbKey, LocalTransform>.ReadOnly LimbLocalTransformLookup;
        [ReadOnly] public NativeParallelHashMap<ulong, Entity>.ReadOnly RootPhenotypeEntityLookup;
        [ReadOnly] public NativeParallelHashMap<ulong, NeuralGraphRef>.ReadOnly NeuralGraphLookup;

        private const int INSTANTIATION_KEY = 1;
        private const int DISPOSAL_KEY = 2;

        public void Execute(Entity requestEntity, ref JointEntityCreationRequest requestData)
        {
            PhenotypeLimbKey refKey = new(requestData.PhenotypeGid, requestData.ReferenceLimbIndex);
            PhenotypeLimbKey attKey = new(requestData.PhenotypeGid, requestData.AttachedLimbIndex);

            if (!LimbEntityLookup.TryGetValue(refKey, out Entity referenceLimbEntity)) return;
            if (!LimbEntityLookup.TryGetValue(attKey, out Entity attachedLimbEntity)) return;
            if (!RootPhenotypeEntityLookup.TryGetValue(requestData.PhenotypeGid, out Entity rootPhenotypeEntity)) return;
            if (!NeuralGraphLookup.TryGetValue(requestData.PhenotypeGid, out NeuralGraphRef neuralGraphRef)) return;

            CreateJointEntity(
                ref Ecb,
                INSTANTIATION_KEY,
                JointArchetype,
                requestData,
                referenceLimbEntity,
                attachedLimbEntity,
                LimbLocalTransformLookup[refKey].ToMatrix(),
                LimbLocalTransformLookup[attKey].ToMatrix(),
                rootPhenotypeEntity,
                neuralGraphRef,
                requestData.AttachedLimbIndex
            );

            Ecb.DestroyEntity(DISPOSAL_KEY, requestEntity);
        }

        // NOTE: DOTS Physics currently supports up to three constraints per joint.
        // For more complex joints we create multiple joint entities.
        [BurstCompile]
        public static void CreateJointEntity(
            ref EntityCommandBuffer.ParallelWriter ecb,
            int sortKey,
            EntityArchetype jointArchetype,
            in JointEntityCreationRequest request,
            in Entity referenceLimbEntity,
            in Entity attachedLimbEntity,
            in float4x4 referenceLimbLocalTransform,
            in float4x4 attachedLimbLocalTransform,
            in Entity rootPhenotypeEntity,
            in NeuralGraphRef neuralGraphRef,
            int limbIndex
        )
        {
            ref CompiledNeuralGraph neuralGraph = ref neuralGraphRef.Value.Value;

            Entity jointEntity1 = ecb.CreateEntity(sortKey, jointArchetype);
            Entity jointEntity2 = Entity.Null;
            Entity jointEntity3 = Entity.Null;

            PhysicsJoint physicsJoint1, physicsJoint2, physicsJoint3;
            float3 primaryAxis, secondaryAxis, tertiaryAxis;
            float primaryAxisLimit = 0f;
            float secondaryAxisLimit = 0f;
            float tertiaryAxisLimit = 0f;
            Constraint primaryAngularConstraint, secondaryAngularConstraint, tertiaryAngularConstraint;
            Constraint primaryMotorConstraint, secondaryMotorConstraint, tertiaryMotorConstraint;
            sbyte primaryMotorConstraintIndex = -1;
            sbyte secondaryMotorConstraintIndex = -1;
            sbyte tertiaryMotorConstraintIndex = -1;

            // All joints get a ball-and-socket linear constraint.
            CreateStaticConstraint(request, ConstraintType.Linear, new bool3(true, true, true), 0f, out Constraint linearConstraint);

            switch (request.JointType)
            {
                case JointType.Rigid:
                    // Rigid joint - lock all rotation.
                    primaryAxis = request.ReferenceLimbSpaceXAxis;
                    secondaryAxis = request.ReferenceLimbSpaceYAxis;
                    tertiaryAxis = request.ReferenceLimbSpaceZAxis;

                    CreateStaticConstraint(request, ConstraintType.Angular, new bool3(true, true, true), 0f, out Constraint rigidAngularConstraint);

                    CreatePhysicsJoint(
                        request.ReferenceLimbSpaceAnchor,
                        primaryAxis,
                        secondaryAxis,
                        tertiaryAxis,
                        referenceLimbLocalTransform,
                        attachedLimbLocalTransform,
                        out physicsJoint1
                    );

                    physicsJoint1.SetConstraints(new() { linearConstraint, rigidAngularConstraint });

                    ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);
                    break;
                case JointType.Revolute:
                case JointType.Twist:
                    // Single-axis rotation joints.
                    // Revolute: rotate around X-axis.
                    // Twist:    rotate around Z-axis.
                    bool isRevolute = request.JointType == JointType.Revolute;

                    primaryAxis = isRevolute ? request.ReferenceLimbSpaceZAxis : request.ReferenceLimbSpaceXAxis;
                    secondaryAxis = request.ReferenceLimbSpaceYAxis;
                    tertiaryAxis = isRevolute ? request.ReferenceLimbSpaceXAxis : request.ReferenceLimbSpaceZAxis;

                    primaryAxisLimit = 0f;
                    secondaryAxisLimit = 0f;
                    tertiaryAxisLimit = isRevolute ? request.AngleLimits.x : request.AngleLimits.z;

                    CreateStaticConstraint(request, ConstraintType.Angular, new bool3(true, false, false), primaryAxisLimit, out primaryAngularConstraint);
                    CreateStaticConstraint(request, ConstraintType.Angular, new bool3(false, true, false), secondaryAxisLimit, out secondaryAngularConstraint);
                    CreateStaticConstraint(request, ConstraintType.Angular, new bool3(false, false, true), tertiaryAxisLimit, out tertiaryAngularConstraint);

                    CreateMotorConstraint(request, ConstraintType.AngularVelocityMotor, new bool3(false, false, true), tertiaryAxisLimit, out tertiaryMotorConstraint);

                    CreatePhysicsJoint(
                        request.ReferenceLimbSpaceAnchor,
                        primaryAxis,
                        secondaryAxis,
                        tertiaryAxis,
                        referenceLimbLocalTransform,
                        attachedLimbLocalTransform,
                        out physicsJoint1
                    );

                    jointEntity2 = ecb.CreateEntity(sortKey, jointArchetype);
                    CreatePhysicsJoint(
                        request.ReferenceLimbSpaceAnchor,
                        primaryAxis,
                        secondaryAxis,
                        tertiaryAxis,
                        referenceLimbLocalTransform,
                        attachedLimbLocalTransform,
                        out physicsJoint2
                    );

                    physicsJoint1.SetConstraints(new() { tertiaryMotorConstraint, linearConstraint });
                    physicsJoint2.SetConstraints(new() { primaryAngularConstraint, secondaryAngularConstraint, tertiaryAngularConstraint, });

                    tertiaryMotorConstraintIndex = 0;

                    ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);
                    ecb.SetComponent(sortKey, jointEntity2, physicsJoint2);
                    break;
                case JointType.BendTwist:
                case JointType.TwistBend:
                    // Two-axis joints: rotate around X and Z axes.
                    bool isBendTwist = request.JointType == JointType.BendTwist;

                    primaryAxis = isBendTwist ? request.ReferenceLimbSpaceZAxis : request.ReferenceLimbSpaceXAxis;
                    secondaryAxis = request.ReferenceLimbSpaceYAxis;
                    tertiaryAxis = isBendTwist ? request.ReferenceLimbSpaceXAxis : request.ReferenceLimbSpaceZAxis;

                    primaryAxisLimit = isBendTwist ? request.AngleLimits.z : request.AngleLimits.x;
                    secondaryAxisLimit = 0f;
                    tertiaryAxisLimit = isBendTwist ? request.AngleLimits.x : request.AngleLimits.z;

                    CreateStaticConstraint(request, ConstraintType.Angular, new bool3(true, false, false), primaryAxisLimit, out primaryAngularConstraint);
                    CreateStaticConstraint(request, ConstraintType.Angular, new bool3(false, true, false), secondaryAxisLimit, out secondaryAngularConstraint);
                    CreateStaticConstraint(request, ConstraintType.Angular, new bool3(false, false, true), tertiaryAxisLimit, out tertiaryAngularConstraint);

                    CreateMotorConstraint(request, ConstraintType.AngularVelocityMotor, new bool3(true, false, false), primaryAxisLimit, out primaryMotorConstraint);
                    CreateMotorConstraint(request, ConstraintType.AngularVelocityMotor, new bool3(false, false, true), tertiaryAxisLimit, out tertiaryMotorConstraint);

                    CreatePhysicsJoint(
                        request.ReferenceLimbSpaceAnchor,
                        primaryAxis,
                        secondaryAxis,
                        tertiaryAxis,
                        referenceLimbLocalTransform,
                        attachedLimbLocalTransform,
                        out physicsJoint1
                    );

                    jointEntity2 = ecb.CreateEntity(sortKey, jointArchetype);
                    CreatePhysicsJoint(
                        request.ReferenceLimbSpaceAnchor,
                        primaryAxis,
                        secondaryAxis,
                        tertiaryAxis,
                        referenceLimbLocalTransform,
                        attachedLimbLocalTransform,
                        out physicsJoint2
                    );

                    physicsJoint1.SetConstraints(new() { primaryMotorConstraint, tertiaryMotorConstraint, linearConstraint });
                    physicsJoint2.SetConstraints(new() { tertiaryAngularConstraint, primaryAngularConstraint, secondaryAngularConstraint });

                    primaryMotorConstraintIndex = 0;
                    tertiaryMotorConstraintIndex = 1;

                    ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);
                    ecb.SetComponent(sortKey, jointEntity2, physicsJoint2);
                    break;
                case JointType.Universal:
                    // Universal joint: rotate around X and Y axes.
                    primaryAxis = request.ReferenceLimbSpaceZAxis;
                    secondaryAxis = request.ReferenceLimbSpaceYAxis;
                    tertiaryAxis = request.ReferenceLimbSpaceXAxis;

                    primaryAxisLimit = 0f;
                    secondaryAxisLimit = request.AngleLimits.y;
                    tertiaryAxisLimit = request.AngleLimits.x;

                    CreateStaticConstraint(request, ConstraintType.Angular, new bool3(true, false, false), primaryAxisLimit, out primaryAngularConstraint);
                    CreateStaticConstraint(request, ConstraintType.Angular, new bool3(false, true, false), secondaryAxisLimit, out secondaryAngularConstraint);
                    CreateStaticConstraint(request, ConstraintType.Angular, new bool3(false, false, true), tertiaryAxisLimit, out tertiaryAngularConstraint);

                    CreateMotorConstraint(request, ConstraintType.AngularVelocityMotor, new bool3(false, true, false), secondaryAxisLimit, out secondaryMotorConstraint);
                    CreateMotorConstraint(request, ConstraintType.AngularVelocityMotor, new bool3(false, false, true), tertiaryAxisLimit, out tertiaryMotorConstraint);

                    CreatePhysicsJoint(
                        request.ReferenceLimbSpaceAnchor,
                        primaryAxis,
                        secondaryAxis,
                        tertiaryAxis,
                        referenceLimbLocalTransform,
                        attachedLimbLocalTransform,
                        out physicsJoint1
                    );

                    jointEntity2 = ecb.CreateEntity(sortKey, jointArchetype);
                    CreatePhysicsJoint(
                        request.ReferenceLimbSpaceAnchor,
                        primaryAxis,
                        secondaryAxis,
                        tertiaryAxis,
                        referenceLimbLocalTransform,
                        attachedLimbLocalTransform,
                        out physicsJoint2
                    );

                    physicsJoint1.SetConstraints(new() { secondaryMotorConstraint, tertiaryMotorConstraint, linearConstraint });
                    physicsJoint2.SetConstraints(new() { tertiaryAngularConstraint, primaryAngularConstraint, secondaryAngularConstraint });

                    secondaryMotorConstraintIndex = 0;
                    tertiaryMotorConstraintIndex = 1;

                    ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);
                    ecb.SetComponent(sortKey, jointEntity2, physicsJoint2);
                    break;
                case JointType.Spherical:
                    // Spherical joint: rotate around all three axes X, Y, Z.
                    primaryAxis = request.ReferenceLimbSpaceXAxis;
                    secondaryAxis = request.ReferenceLimbSpaceYAxis;
                    tertiaryAxis = request.ReferenceLimbSpaceZAxis;

                    primaryAxisLimit = request.AngleLimits.z;
                    secondaryAxisLimit = request.AngleLimits.y;
                    tertiaryAxisLimit = request.AngleLimits.x;

                    CreateStaticConstraint(request, ConstraintType.Angular, new bool3(true, false, false), primaryAxisLimit, out primaryAngularConstraint);
                    CreateStaticConstraint(request, ConstraintType.Angular, new bool3(false, true, false), secondaryAxisLimit, out secondaryAngularConstraint);
                    CreateStaticConstraint(request, ConstraintType.Angular, new bool3(false, false, true), tertiaryAxisLimit, out tertiaryAngularConstraint);

                    CreateMotorConstraint(request, ConstraintType.AngularVelocityMotor, new bool3(true, false, false), primaryAxisLimit, out primaryMotorConstraint);
                    CreateMotorConstraint(request, ConstraintType.AngularVelocityMotor, new bool3(false, true, false), secondaryAxisLimit, out secondaryMotorConstraint);
                    CreateMotorConstraint(request, ConstraintType.AngularVelocityMotor, new bool3(false, false, true), tertiaryAxisLimit, out tertiaryMotorConstraint);

                    CreatePhysicsJoint(
                        request.ReferenceLimbSpaceAnchor,
                        primaryAxis,
                        secondaryAxis,
                        tertiaryAxis,
                        referenceLimbLocalTransform,
                        attachedLimbLocalTransform,
                        out physicsJoint1
                    );

                    jointEntity2 = ecb.CreateEntity(sortKey, jointArchetype);
                    CreatePhysicsJoint(
                        request.ReferenceLimbSpaceAnchor,
                        primaryAxis,
                        secondaryAxis,
                        tertiaryAxis,
                        referenceLimbLocalTransform,
                        attachedLimbLocalTransform,
                        out physicsJoint2
                    );

                    jointEntity3 = ecb.CreateEntity(sortKey, jointArchetype);
                    CreatePhysicsJoint(
                        request.ReferenceLimbSpaceAnchor,
                        primaryAxis,
                        secondaryAxis,
                        tertiaryAxis,
                        referenceLimbLocalTransform,
                        attachedLimbLocalTransform,
                        out physicsJoint3
                    );

                    physicsJoint1.SetConstraints(new() { primaryMotorConstraint, secondaryMotorConstraint, tertiaryMotorConstraint });
                    physicsJoint2.SetConstraints(new() { primaryAngularConstraint, secondaryAngularConstraint, tertiaryAngularConstraint });
                    physicsJoint3.SetConstraints(new() { linearConstraint });

                    primaryMotorConstraintIndex = 0;
                    secondaryMotorConstraintIndex = 1;
                    tertiaryMotorConstraintIndex = 2;

                    ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);
                    ecb.SetComponent(sortKey, jointEntity2, physicsJoint2);
                    ecb.SetComponent(sortKey, jointEntity3, physicsJoint3);
                    break;
                default:
                    break;
            }

            // Shared bookkeeping for all created joint entities.
            ecb.SetComponent(sortKey, jointEntity1, new RootPhenotypeEntity { Value = rootPhenotypeEntity });
            ecb.SetSharedComponent(sortKey, jointEntity1, new PhysicsWorldIndex(0));
            ecb.SetComponent(sortKey, jointEntity1, new PhysicsConstrainedBodyPair(referenceLimbEntity, attachedLimbEntity, false));
            JointBreakDistance breakDistance = new() { DistanceSquared = request.MinCrossSectionalArea };
            ecb.SetComponent(sortKey, jointEntity1, breakDistance);
            ecb.AddComponent(sortKey, jointEntity1, new JointAngleSensors
            {
                Angles = float3.zero,
                AngleLimits = new float3(primaryAxisLimit, secondaryAxisLimit, tertiaryAxisLimit),
                PrimarySensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(limbIndex, SensorType.JointAngle, 0),
                SecondarySensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(limbIndex, SensorType.JointAngle, 1),
                TertiarySensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(limbIndex, SensorType.JointAngle, 2)
            });
            ecb.AddComponent(sortKey, jointEntity1, new JointAngleActuators
            {
                TargetAngularVelocities = float3.zero,
                PrimaryMotorConstraintIndex = primaryMotorConstraintIndex,
                SecondaryMotorConstraintIndex = secondaryMotorConstraintIndex,
                TertiaryMotorConstraintIndex = tertiaryMotorConstraintIndex,
                PrimaryActuatorNeuronEmitterIndex = neuralGraph.GetEmitterIndexOfActuatorNeuron(limbIndex, ActuatorType.JointAngle, 0),
                SecondaryActuatorNeuronEmitterIndex = neuralGraph.GetEmitterIndexOfActuatorNeuron(limbIndex, ActuatorType.JointAngle, 1),
                TertiaryActuatorNeuronEmitterIndex = neuralGraph.GetEmitterIndexOfActuatorNeuron(limbIndex, ActuatorType.JointAngle, 2)
            });

            if (jointEntity2 != Entity.Null)
            {
                ecb.SetComponent(sortKey, jointEntity2, new RootPhenotypeEntity { Value = rootPhenotypeEntity });
                ecb.SetSharedComponent(sortKey, jointEntity2, new PhysicsWorldIndex(0));
                ecb.SetComponent(sortKey, jointEntity2, new PhysicsConstrainedBodyPair(referenceLimbEntity, attachedLimbEntity, false));
                ecb.SetComponent(sortKey, jointEntity2, breakDistance);
                ecb.AppendToBuffer(sortKey, jointEntity1, new PhysicsJointCompanion { JointEntity = jointEntity2 });
                ecb.AppendToBuffer(sortKey, jointEntity2, new PhysicsJointCompanion { JointEntity = jointEntity1 });
            }

            if (jointEntity3 != Entity.Null)
            {
                ecb.SetComponent(sortKey, jointEntity3, new RootPhenotypeEntity { Value = rootPhenotypeEntity });
                ecb.SetSharedComponent(sortKey, jointEntity3, new PhysicsWorldIndex(0));
                ecb.SetComponent(sortKey, jointEntity3, new PhysicsConstrainedBodyPair(referenceLimbEntity, attachedLimbEntity, false));
                ecb.SetComponent(sortKey, jointEntity3, breakDistance);
                ecb.AppendToBuffer(sortKey, jointEntity1, new PhysicsJointCompanion { JointEntity = jointEntity3 });
                ecb.AppendToBuffer(sortKey, jointEntity2, new PhysicsJointCompanion { JointEntity = jointEntity3 });
                ecb.AppendToBuffer(sortKey, jointEntity3, new PhysicsJointCompanion { JointEntity = jointEntity1 });
                ecb.AppendToBuffer(sortKey, jointEntity3, new PhysicsJointCompanion { JointEntity = jointEntity2 });
            }

            ecb.SetComponent(sortKey, attachedLimbEntity, new ParentLimb { Value = referenceLimbEntity });
        }

        [BurstCompile]
        private static void CreatePhysicsJoint(
            in float3 referenceLimbSpaceAnchor,
            in float3 primaryAxis,
            in float3 secondaryAxis,
            in float3 tertiaryAxis,
            in float4x4 referenceLimbLocalTransform,
            in float4x4 attachedLimbLocalTransform,
            out PhysicsJoint joint
        )
        {
            bool flippedHandedness = math.dot(math.cross(primaryAxis, secondaryAxis), tertiaryAxis) < 0f;

            BodyFrame bodyAFromJoint = new()
            {
                Position = referenceLimbSpaceAnchor,
                Axis = primaryAxis * (flippedHandedness ? -1f : 1f),
                PerpendicularAxis = secondaryAxis * (flippedHandedness ? -1f : 1f)
            };

            float4x4 T_B_from_A = math.mul(math.inverse(attachedLimbLocalTransform), referenceLimbLocalTransform);

            BodyFrame bodyBFromJoint = new()
            {
                Position = math.transform(T_B_from_A, bodyAFromJoint.Position),
                Axis = math.normalize(math.mul(T_B_from_A, new float4(bodyAFromJoint.Axis, 0f)).xyz),
                PerpendicularAxis = math.normalize(math.mul(T_B_from_A, new float4(bodyAFromJoint.PerpendicularAxis, 0f)).xyz)
            };

            joint = new PhysicsJoint
            {
                BodyAFromJoint = bodyAFromJoint,
                BodyBFromJoint = bodyBFromJoint
            };
        }

        [BurstCompile]
        private static void CreateStaticConstraint(
            in JointEntityCreationRequest request,
            ConstraintType type,
            in bool3 constrainedAxes,
            float limit,
            out Constraint constraint
        )
        {
            constraint = new Constraint
            {
                ConstrainedAxes = constrainedAxes,
                Type = type,
                Min = -limit,
                Max = limit,
                SpringFrequency = 50f,
                DampingRatio = 1f,
                MaxImpulse = new float3(BASE_MAX_MOTOR_IMPULSE * request.MinCrossSectionalArea),
                Target = float3.zero
            };
        }

        [BurstCompile]
        private static void CreateMotorConstraint(
            in JointEntityCreationRequest request,
            ConstraintType type,
            in bool3 constrainedAxes,
            float angleLimit,
            out Constraint constraint
        )
        {
            constraint = new Constraint
            {
                ConstrainedAxes = constrainedAxes,
                Type = type,
                Min = -angleLimit,
                Max = angleLimit,
                SpringFrequency = SPRING_FREQUENCY,
                DampingRatio = DAMPING_RATIO,
                MaxImpulse = new float3(BASE_MAX_MOTOR_IMPULSE * request.MinCrossSectionalArea),
                Target = float3.zero
            };
        }
    }
}
