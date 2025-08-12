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
        private const float SPRING_FREQUENCY = 10f;
        private const float DAMPING_RATIO = 0.9f;
        private const float BASE_MAX_MOTOR_IMPULSE = 10f;

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

        // BUG: Some joint configurations cause the attached limb to do a 360deg flip when the simulation starts.
        // NOTE: DOTS Physics 1.3 only appears to support up to three constraints per joint.
        // Until this changes, we have to create two joint entities for more complex joints.
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
            Entity jointEntity1 = ecb.CreateEntity(sortKey, jointArchetype);
            Entity jointEntity2 = Entity.Null;

            ref CompiledNeuralGraph neuralGraph = ref neuralGraphRef.Value.Value;

            PhysicsJoint physicsJoint1;
            PhysicsJoint physicsJoint2;
            switch (request.JointType)
            {
                case JointType.Rigid:
                    CreatePhysicsJoint(request, referenceLimbLocalTransform, attachedLimbLocalTransform, out physicsJoint1);
                    CreateConstraint(request, ConstraintType.Angular, new bool3(true, true, true), 0f, out var rigidAngularConstraint);
                    CreateConstraint(request, ConstraintType.Linear, new bool3(true, true, true), 0f, out var rigidLinearConstraint);
                    physicsJoint1.SetConstraints(new() { rigidAngularConstraint, rigidLinearConstraint });
                    ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);
                    break;
                case JointType.Revolute:
                    CreatePhysicsJoint(request, referenceLimbLocalTransform, attachedLimbLocalTransform, out physicsJoint1);
                    CreateConstraint(request, ConstraintType.RotationMotor, new bool3(true, false, false), request.AngleLimits.x, out var revoluteXConstraint);
                    CreateConstraint(request, ConstraintType.Angular, new bool3(false, true, true), 0f, out var revoluteAngularConstraint);
                    CreateConstraint(request, ConstraintType.Linear, new bool3(true, true, true), 0f, out var revoluteLinearConstraint);
                    physicsJoint1.SetConstraints(new() { revoluteXConstraint, revoluteAngularConstraint, revoluteLinearConstraint });
                    ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);
                    ecb.AddComponent(sortKey, jointEntity1, new JointAxisX
                    {
                        SensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(limbIndex, SensorType.JointAngle, 0),
                        ActuatorNeuronEmitterIndex = neuralGraph.GetEmitterIndexOfActuatorNeuron(limbIndex, ActuatorType.JointAngle, 0),
                        AngleLimit = request.AngleLimits.x,
                        SwapXZ = 0
                    });
                    break;
                case JointType.Twist:
                    CreatePhysicsJoint(request, referenceLimbLocalTransform, attachedLimbLocalTransform, out physicsJoint1);
                    CreateConstraint(request, ConstraintType.RotationMotor, new bool3(false, false, true), request.AngleLimits.z, out var twistZConstraint);
                    CreateConstraint(request, ConstraintType.Angular, new bool3(true, true, false), 0f, out var twistAngularConstraint);
                    CreateConstraint(request, ConstraintType.Linear, new bool3(true, true, true), 0f, out var twistLinearConstraint);
                    physicsJoint1.SetConstraints(new() { twistZConstraint, twistAngularConstraint, twistLinearConstraint });
                    ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);
                    ecb.AddComponent(sortKey, jointEntity1, new JointAxisZ
                    {
                        SensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(limbIndex, SensorType.JointAngle, 0),
                        ActuatorNeuronEmitterIndex = neuralGraph.GetEmitterIndexOfActuatorNeuron(limbIndex, ActuatorType.JointAngle, 0),
                        AngleLimit = request.AngleLimits.z,
                        SwapXZ = 0
                    });
                    break;
                case JointType.BendTwist:
                    CreatePhysicsJoint(request, referenceLimbLocalTransform, attachedLimbLocalTransform, out physicsJoint1);
                    CreateConstraint(request, ConstraintType.Angular, new bool3(false, true, false), 0f, out var bendTwistAngularConstraint);
                    CreateConstraint(request, ConstraintType.Linear, new bool3(true, true, true), 0f, out var bendTwistLinearConstraint);
                    physicsJoint1.SetConstraints(new() { bendTwistAngularConstraint, bendTwistLinearConstraint });
                    ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);

                    jointEntity2 = ecb.CreateEntity(sortKey, jointArchetype);
                    CreatePhysicsJoint(request, referenceLimbLocalTransform, attachedLimbLocalTransform, out physicsJoint2);
                    CreateConstraint(request, ConstraintType.RotationMotor, new bool3(true, false, false), request.AngleLimits.x, out var bendTwistXConstraint);
                    CreateConstraint(request, ConstraintType.RotationMotor, new bool3(false, false, true), request.AngleLimits.z, out var bendTwistZConstraint);
                    physicsJoint2.SetConstraints(new() { bendTwistXConstraint, bendTwistZConstraint });
                    ecb.SetComponent(sortKey, jointEntity2, physicsJoint2);
                    ecb.AddComponent(sortKey, jointEntity2, new JointAxisX
                    {
                        SensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(limbIndex, SensorType.JointAngle, 0),
                        ActuatorNeuronEmitterIndex = neuralGraph.GetEmitterIndexOfActuatorNeuron(limbIndex, ActuatorType.JointAngle, 0),
                        AngleLimit = request.AngleLimits.x,
                        SwapXZ = 0
                    });
                    ecb.AddComponent(sortKey, jointEntity2, new JointAxisZ
                    {
                        SensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(limbIndex, SensorType.JointAngle, 1),
                        ActuatorNeuronEmitterIndex = neuralGraph.GetEmitterIndexOfActuatorNeuron(limbIndex, ActuatorType.JointAngle, 1),
                        AngleLimit = request.AngleLimits.z,
                        SwapXZ = 0
                    });
                    break;
                case JointType.TwistBend:
                    CreatePhysicsJoint(request, referenceLimbLocalTransform, attachedLimbLocalTransform, out physicsJoint1, swapXZ: true);
                    CreateConstraint(request, ConstraintType.Angular, new bool3(false, true, false), 0f, out var twistBendAngularConstraint);
                    CreateConstraint(request, ConstraintType.Linear, new bool3(true, true, true), 0f, out var twistBendLinearConstraint);
                    physicsJoint1.SetConstraints(new() { twistBendAngularConstraint, twistBendLinearConstraint });
                    ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);

                    jointEntity2 = ecb.CreateEntity(sortKey, jointArchetype);
                    CreatePhysicsJoint(request, referenceLimbLocalTransform, attachedLimbLocalTransform, out physicsJoint2, swapXZ: true);
                    CreateConstraint(request, ConstraintType.RotationMotor, new bool3(true, false, false), request.AngleLimits.z, out var twistBendZConstraint);
                    CreateConstraint(request, ConstraintType.RotationMotor, new bool3(false, false, true), request.AngleLimits.x, out var twistBendXConstraint);
                    physicsJoint2.SetConstraints(new() { twistBendZConstraint, twistBendXConstraint });
                    ecb.SetComponent(sortKey, jointEntity2, physicsJoint2);
                    ecb.AddComponent(sortKey, jointEntity2, new JointAxisX
                    {
                        SensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(limbIndex, SensorType.JointAngle, 0),
                        ActuatorNeuronEmitterIndex = neuralGraph.GetEmitterIndexOfActuatorNeuron(limbIndex, ActuatorType.JointAngle, 0),
                        AngleLimit = request.AngleLimits.z,
                        SwapXZ = 1
                    });
                    ecb.AddComponent(sortKey, jointEntity2, new JointAxisZ
                    {
                        SensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(limbIndex, SensorType.JointAngle, 1),
                        ActuatorNeuronEmitterIndex = neuralGraph.GetEmitterIndexOfActuatorNeuron(limbIndex, ActuatorType.JointAngle, 1),
                        AngleLimit = request.AngleLimits.x,
                        SwapXZ = 1
                    });
                    break;
                case JointType.Universal:
                    CreatePhysicsJoint(request, referenceLimbLocalTransform, attachedLimbLocalTransform, out physicsJoint1);
                    CreateConstraint(request, ConstraintType.Angular, new bool3(false, false, true), 0f, out var universalAngularConstraint);
                    CreateConstraint(request, ConstraintType.Linear, new bool3(true, true, true), 0f, out var universalLinearConstraint);
                    physicsJoint1.SetConstraints(new() { universalAngularConstraint, universalLinearConstraint });
                    ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);

                    jointEntity2 = ecb.CreateEntity(sortKey, jointArchetype);
                    CreatePhysicsJoint(request, referenceLimbLocalTransform, attachedLimbLocalTransform, out physicsJoint2);
                    CreateConstraint(request, ConstraintType.RotationMotor, new bool3(true, false, false), request.AngleLimits.x, out var universalXConstraint);
                    CreateConstraint(request, ConstraintType.RotationMotor, new bool3(false, true, false), request.AngleLimits.y, out var universalYConstraint);
                    physicsJoint2.SetConstraints(new() { universalXConstraint, universalYConstraint });
                    ecb.SetComponent(sortKey, jointEntity2, physicsJoint2);
                    ecb.AddComponent(sortKey, jointEntity2, new JointAxisX
                    {
                        SensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(limbIndex, SensorType.JointAngle, 0),
                        ActuatorNeuronEmitterIndex = neuralGraph.GetEmitterIndexOfActuatorNeuron(limbIndex, ActuatorType.JointAngle, 0),
                        AngleLimit = request.AngleLimits.x,
                        SwapXZ = 0
                    });
                    ecb.AddComponent(sortKey, jointEntity2, new JointAxisY
                    {
                        SensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(limbIndex, SensorType.JointAngle, 1),
                        ActuatorNeuronEmitterIndex = neuralGraph.GetEmitterIndexOfActuatorNeuron(limbIndex, ActuatorType.JointAngle, 1),
                        AngleLimit = request.AngleLimits.y
                    });
                    break;
                case JointType.Spherical:
                    CreatePhysicsJoint(request, referenceLimbLocalTransform, attachedLimbLocalTransform, out physicsJoint1);
                    CreateConstraint(request, ConstraintType.Linear, new bool3(true, true, true), 0f, out var sphericalLinearConstraint);
                    physicsJoint1.SetConstraints(new() { sphericalLinearConstraint });
                    ecb.SetComponent(sortKey, jointEntity1, physicsJoint1);

                    jointEntity2 = ecb.CreateEntity(sortKey, jointArchetype);
                    CreatePhysicsJoint(request, referenceLimbLocalTransform, attachedLimbLocalTransform, out physicsJoint2);
                    CreateConstraint(request, ConstraintType.RotationMotor, new bool3(true, false, false), request.AngleLimits.x, out var sphericalXConstraint);
                    CreateConstraint(request, ConstraintType.RotationMotor, new bool3(false, true, false), request.AngleLimits.y, out var sphericalYConstraint);
                    CreateConstraint(request, ConstraintType.RotationMotor, new bool3(false, false, true), request.AngleLimits.z, out var sphericalZConstraint);
                    physicsJoint2.SetConstraints(new() { sphericalXConstraint, sphericalYConstraint, sphericalZConstraint });
                    ecb.SetComponent(sortKey, jointEntity2, physicsJoint2);
                    ecb.AddComponent(sortKey, jointEntity2, new JointAxisX
                    {
                        SensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(limbIndex, SensorType.JointAngle, 0),
                        ActuatorNeuronEmitterIndex = neuralGraph.GetEmitterIndexOfActuatorNeuron(limbIndex, ActuatorType.JointAngle, 0),
                        AngleLimit = request.AngleLimits.x,
                        SwapXZ = 0
                    });
                    ecb.AddComponent(sortKey, jointEntity2, new JointAxisY
                    {
                        SensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(limbIndex, SensorType.JointAngle, 1),
                        ActuatorNeuronEmitterIndex = neuralGraph.GetEmitterIndexOfActuatorNeuron(limbIndex, ActuatorType.JointAngle, 1),
                        AngleLimit = request.AngleLimits.y
                    });
                    ecb.AddComponent(sortKey, jointEntity2, new JointAxisZ
                    {
                        SensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(limbIndex, SensorType.JointAngle, 2),
                        ActuatorNeuronEmitterIndex = neuralGraph.GetEmitterIndexOfActuatorNeuron(limbIndex, ActuatorType.JointAngle, 2),
                        AngleLimit = request.AngleLimits.z,
                        SwapXZ = 0
                    });
                    break;
                default:
                    break;
            }

            ecb.SetComponent(sortKey, jointEntity1, new RootPhenotypeEntity { Value = rootPhenotypeEntity });
            ecb.SetSharedComponent(sortKey, jointEntity1, new PhysicsWorldIndex(0));
            ecb.SetComponent(sortKey, jointEntity1, new PhysicsConstrainedBodyPair(referenceLimbEntity, attachedLimbEntity, false));
            JointBreakDistance breakDistance = new() { DistanceSquared = request.MinCrossSectionalArea * request.MinCrossSectionalArea };
            ecb.SetComponent(sortKey, jointEntity1, breakDistance);
            if (jointEntity2 != Entity.Null)
            {
                ecb.SetComponent(sortKey, jointEntity2, new RootPhenotypeEntity { Value = rootPhenotypeEntity });
                ecb.SetSharedComponent(sortKey, jointEntity2, new PhysicsWorldIndex(0));
                ecb.SetComponent(sortKey, jointEntity2, new PhysicsConstrainedBodyPair(referenceLimbEntity, attachedLimbEntity, false));
                ecb.SetComponent(sortKey, jointEntity2, breakDistance);

                ecb.AppendToBuffer(sortKey, jointEntity1, new PhysicsJointCompanion() { JointEntity = jointEntity2 });
                ecb.AppendToBuffer(sortKey, jointEntity2, new PhysicsJointCompanion() { JointEntity = jointEntity1 });
            }

            ecb.SetComponent(sortKey, attachedLimbEntity, new ParentLimb { Value = referenceLimbEntity });
        }

        [BurstCompile]
        private static void CreatePhysicsJoint(
            in JointEntityCreationRequest request,
            in float4x4 referenceLimbLocalTransform,
            in float4x4 attachedLimbLocalTransform,
            out PhysicsJoint joint,
            bool swapXZ = false
        )
        {
            CreateUnconstrainedJoint(
                request.ReferenceLimbSpaceAnchor,
                swapXZ ? request.ReferenceLimbSpaceZAxis : request.ReferenceLimbSpaceXAxis,
                request.ReferenceLimbSpaceYAxis,
                swapXZ ? request.ReferenceLimbSpaceXAxis : request.ReferenceLimbSpaceZAxis,
                referenceLimbLocalTransform,
                attachedLimbLocalTransform,
                out joint
            );
        }

        [BurstCompile]
        private static void CreateUnconstrainedJoint(
            in float3 referenceLimbSpaceAnchor,
            in float3 referenceLimbSpaceXAxis,
            in float3 referenceLimbSpaceYAxis,
            in float3 referenceLimbSpaceZAxis,
            in float4x4 referenceLimbLocalTransform,
            in float4x4 attachedLimbLocalTransform,
            out PhysicsJoint joint
        )
        {
            bool flippedHandedness = math.dot(math.cross(referenceLimbSpaceXAxis, referenceLimbSpaceYAxis), referenceLimbSpaceZAxis) < 0f;

            BodyFrame bodyAFromJoint = new()
            {
                Position = referenceLimbSpaceAnchor,
                Axis = referenceLimbSpaceXAxis * (flippedHandedness ? -1f : 1f),
                PerpendicularAxis = referenceLimbSpaceYAxis * (flippedHandedness ? -1f : 1f)
            };

            float4x4 T_B_from_A = math.mul(math.inverse(attachedLimbLocalTransform), referenceLimbLocalTransform);

            BodyFrame bodyBFromJoint = new()
            {
                Position = math.transform(T_B_from_A, bodyAFromJoint.Position),
                Axis = math.normalize(math.mul(T_B_from_A, new float4(bodyAFromJoint.Axis, 0)).xyz),
                PerpendicularAxis = math.normalize(math.mul(T_B_from_A, new float4(bodyAFromJoint.PerpendicularAxis, 0)).xyz)
            };

            joint = new()
            {
                BodyAFromJoint = bodyAFromJoint,
                BodyBFromJoint = bodyBFromJoint
            };
        }

        [BurstCompile]
        private static void CreateConstraint(in JointEntityCreationRequest request, ConstraintType type, in bool3 constrainedAxes, float angleLimit, out Constraint constraint)
        {
            float maxImpulseOfMotor = BASE_MAX_MOTOR_IMPULSE * request.MinCrossSectionalArea;
            float springFrequency = type == ConstraintType.RotationMotor ? SPRING_FREQUENCY : 50f;
            float dampingRatio = type == ConstraintType.RotationMotor ? DAMPING_RATIO : 1f;
            float limit = math.abs(angleLimit);
            constraint = new()
            {
                ConstrainedAxes = constrainedAxes,
                Type = type,
                Min = -limit,
                Max = limit,
                SpringFrequency = springFrequency,
                DampingRatio = dampingRatio,
                MaxImpulse = new float3(maxImpulseOfMotor, maxImpulseOfMotor, maxImpulseOfMotor),
                Target = float3.zero
            };
        }
    }
}
