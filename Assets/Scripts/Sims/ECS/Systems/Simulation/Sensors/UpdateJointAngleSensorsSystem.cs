using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Sensors
{
    using TheSimsulator.Sims.Phenotype;
    using Core.ECS.Components.Shared;
    using Components.Phenotype;

    [BurstCompile]
    [UpdateInGroup(typeof(UpdateSensorsSystemGroup))]
    public partial struct UpdateJointAngleSensorsSystem : ISystem
    {
        private ComponentLookup<LocalTransform> localTransformLookup;
        private BufferLookup<EmitterState> emitterStatesLookup;

        public void OnCreate(ref SystemState state)
        {
            localTransformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
            emitterStatesLookup = state.GetBufferLookup<EmitterState>(isReadOnly: false);

            state.RequireForUpdate<PhysicsConstrainedBodyPair>();
            state.RequireForUpdate<PhysicsJoint>();
            state.RequireForUpdate<RootPhenotypeEntity>();
            state.RequireForUpdate<JointAngleSensors>();
        }

        public void OnUpdate(ref SystemState state)
        {
            localTransformLookup.Update(ref state);
            emitterStatesLookup.Update(ref state);

            state.Dependency = new UpdateJointAngleSensorsJob()
            {
                LocalTransformLookup = localTransformLookup,
                EmitterStateBuffers = emitterStatesLookup
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct UpdateJointAngleSensorsJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        [NativeDisableParallelForRestriction] public BufferLookup<EmitterState> EmitterStateBuffers;

        [BurstCompile]
        public void Execute(in PhysicsConstrainedBodyPair pair, in PhysicsJoint joint, in RootPhenotypeEntity rootPhenotypeEntity, ref JointAngleSensors s)
        {
            if (!LocalTransformLookup.HasComponent(pair.EntityA) || !LocalTransformLookup.HasComponent(pair.EntityB))
                return;

            if (!EmitterStateBuffers.HasBuffer(rootPhenotypeEntity.Value))
                return;

            DynamicBuffer<EmitterState> buffer = EmitterStateBuffers[rootPhenotypeEntity.Value];

            LocalTransform localTransformA = LocalTransformLookup[pair.EntityA];
            LocalTransform localTransformB = LocalTransformLookup[pair.EntityB];

            float primaryAngle = GetJointAngle(localTransformA, localTransformB, joint, JointAxis.Primary);
            float secondaryAngle = GetJointAngle(localTransformA, localTransformB, joint, JointAxis.Secondary);
            float tertiaryAngle = GetJointAngle(localTransformA, localTransformB, joint, JointAxis.Tertiary);

            s.Angles = new float3(primaryAngle, secondaryAngle, tertiaryAngle);

            float primaryEmitterValue = ConvertAngleToEmitterValue(primaryAngle, s.AngleLimits.x);
            float secondaryEmitterValue = ConvertAngleToEmitterValue(secondaryAngle, s.AngleLimits.y);
            float tertiaryEmitterValue = ConvertAngleToEmitterValue(tertiaryAngle, s.AngleLimits.z);

            if (s.PrimarySensorEmitterIndex < (ushort)buffer.Length)
                buffer[s.PrimarySensorEmitterIndex] = new EmitterState { Value = primaryEmitterValue };
            if (s.SecondarySensorEmitterIndex < (ushort)buffer.Length)
                buffer[s.SecondarySensorEmitterIndex] = new EmitterState { Value = secondaryEmitterValue };
            if (s.TertiarySensorEmitterIndex < (ushort)buffer.Length)
                buffer[s.TertiarySensorEmitterIndex] = new EmitterState { Value = tertiaryEmitterValue };
        }

        /// <summary>
        /// Extracts signed angle (in radians) between EntityA and EntityB around the joint frame defined by BodyAFromJoint, for the specified axis.
        /// </summary>
        /// <param name="localTransformA">Parent (entity A) local transform</param>
        /// <param name="localTransformB">Child (entity B) local transform</param>
        /// <param name="joint">The PhysicsJoint</param>
        /// <param name="axis">Axis around which to compute the angle</param>
        /// <returns>float: angle around the specified axis of the joint frame, in radians</returns>
        [BurstCompile]
        public static float GetJointAngle(
            in LocalTransform localTransformA,
            in LocalTransform localTransformB,
            in PhysicsJoint joint,
            in JointAxis axis
        )
        {
            quaternion worldRotA = localTransformA.Rotation;
            quaternion worldRotB = localTransformB.Rotation;

            float3 axisLocal, refLocalA, refLocalB;
            switch (axis)
            {
                case JointAxis.Primary:
                    axisLocal = -joint.BodyAFromJoint.Axis;
                    refLocalA = joint.BodyAFromJoint.PerpendicularAxis;
                    refLocalB = joint.BodyBFromJoint.PerpendicularAxis;
                    break;
                case JointAxis.Secondary:
                    axisLocal = -joint.BodyAFromJoint.PerpendicularAxis;
                    refLocalA = math.cross(joint.BodyAFromJoint.PerpendicularAxis, joint.BodyAFromJoint.Axis);
                    refLocalB = math.cross(joint.BodyBFromJoint.PerpendicularAxis, joint.BodyBFromJoint.Axis);
                    break;
                default:
                    axisLocal = math.normalize(math.cross(joint.BodyAFromJoint.PerpendicularAxis, joint.BodyAFromJoint.Axis));
                    refLocalA = joint.BodyAFromJoint.Axis;
                    refLocalB = joint.BodyBFromJoint.Axis;
                    break;
            }

            float3 worldAxis = math.rotate(worldRotA, axisLocal);
            float3 worldRefA = math.rotate(worldRotA, refLocalA);
            float3 worldRefB = math.rotate(worldRotB, refLocalB);

            float cos = math.dot(worldRefA, worldRefB);
            float sin = math.dot(math.cross(worldRefA, worldRefB), worldAxis);
            return math.atan2(sin, cos);
        }

        [BurstCompile]
        public static float ConvertAngleToEmitterValue(float angle, float angleLimit)
        {
            if (math.abs(angleLimit) < 1e-3f)
                return 0f;

            return math.clamp(angle / angleLimit, -1f, 1f);
        }
    }
}
