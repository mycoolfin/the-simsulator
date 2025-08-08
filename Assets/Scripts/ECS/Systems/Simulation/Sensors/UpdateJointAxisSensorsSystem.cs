using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.Systems.Simulation.Sensors
{
    using Components.Phenotype;

    [BurstCompile]
    [UpdateInGroup(typeof(UpdateSensorsSystemGroup))]
    public partial struct UpdateJointAxisSensorsSystem : ISystem
    {
        private ComponentLookup<LocalTransform> localTransformLookup;
        private BufferLookup<EmitterState> emitterStatesLookup;

        public void OnCreate(ref SystemState state)
        {
            localTransformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
            emitterStatesLookup = state.GetBufferLookup<EmitterState>(isReadOnly: false);

            EntityQuery entityQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                ComponentType.ReadOnly<PhysicsConstrainedBodyPair>(),
                ComponentType.ReadOnly<PhysicsJoint>(),
                ComponentType.ReadOnly<RootPhenotypeEntity>()
            },
                Any = new[]
                {
                ComponentType.ReadOnly<JointAxisX>(),
                ComponentType.ReadOnly<JointAxisY>(),
                ComponentType.ReadOnly<JointAxisZ>()
            }
            });
            state.RequireForUpdate(entityQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            localTransformLookup.Update(ref state);
            emitterStatesLookup.Update(ref state);

            UpdateJointAxisXSensorJob xJob = new()
            {
                LocalTransformLookup = localTransformLookup,
                EmitterStateBuffers = emitterStatesLookup
            };
            state.Dependency = xJob.ScheduleParallel(state.Dependency);

            UpdateJointAxisYSensorJob yJob = new()
            {
                LocalTransformLookup = localTransformLookup,
                EmitterStateBuffers = emitterStatesLookup
            };
            state.Dependency = yJob.ScheduleParallel(state.Dependency);

            UpdateJointAxisZSensorJob zJob = new()
            {
                LocalTransformLookup = localTransformLookup,
                EmitterStateBuffers = emitterStatesLookup
            };
            state.Dependency = zJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct UpdateJointAxisXSensorJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        [NativeDisableParallelForRestriction] public BufferLookup<EmitterState> EmitterStateBuffers;

        public void Execute(in PhysicsConstrainedBodyPair pair, in PhysicsJoint joint, in JointAxisX jointAxis, in RootPhenotypeEntity rootPhenotypeEntity)
        {
            JointAxisSensorJobCore.Axis axis = jointAxis.SwapXZ == 1 ? JointAxisSensorJobCore.Axis.Z : JointAxisSensorJobCore.Axis.X;
            JointAxisSensorJobCore.ExecuteAxis(pair, joint, rootPhenotypeEntity, axis, jointAxis.SensorEmitterIndex, jointAxis.AngleLimit, ref LocalTransformLookup, ref EmitterStateBuffers);
        }
    }

    [BurstCompile]
    public partial struct UpdateJointAxisYSensorJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        [NativeDisableParallelForRestriction] public BufferLookup<EmitterState> EmitterStateBuffers;

        public void Execute(in PhysicsConstrainedBodyPair pair, in PhysicsJoint joint, in JointAxisY jointAxis, in RootPhenotypeEntity rootPhenotypeEntity)
        {
            JointAxisSensorJobCore.ExecuteAxis(pair, joint, rootPhenotypeEntity, JointAxisSensorJobCore.Axis.Y, jointAxis.SensorEmitterIndex, jointAxis.AngleLimit, ref LocalTransformLookup, ref EmitterStateBuffers);
        }
    }

    [BurstCompile]
    public partial struct UpdateJointAxisZSensorJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        [NativeDisableParallelForRestriction] public BufferLookup<EmitterState> EmitterStateBuffers;

        public void Execute(in PhysicsConstrainedBodyPair pair, in PhysicsJoint joint, in JointAxisZ jointAxis, in RootPhenotypeEntity rootPhenotypeEntity)
        {
            JointAxisSensorJobCore.Axis axis = jointAxis.SwapXZ == 1 ? JointAxisSensorJobCore.Axis.X : JointAxisSensorJobCore.Axis.Z;
            JointAxisSensorJobCore.ExecuteAxis(pair, joint, rootPhenotypeEntity, axis, jointAxis.SensorEmitterIndex, jointAxis.AngleLimit, ref LocalTransformLookup, ref EmitterStateBuffers);
        }
    }

    [BurstCompile]
    public static class JointAxisSensorJobCore
    {
        public enum Axis : byte { X, Y, Z }

        [BurstCompile]
        public static void ExecuteAxis(
            in PhysicsConstrainedBodyPair pair,
            in PhysicsJoint joint,
            in RootPhenotypeEntity rootPhenotypeEntity,
            in Axis axis,
            ushort emitterIndex,
            float angleLimit,
            ref ComponentLookup<LocalTransform> localTransformLookup,
            ref BufferLookup<EmitterState> emitterStateBuffers
        )
        {
            if (!localTransformLookup.HasComponent(pair.EntityA) || !localTransformLookup.HasComponent(pair.EntityB))
                return;

            if (!emitterStateBuffers.HasBuffer(rootPhenotypeEntity.Value))
                return;

            DynamicBuffer<EmitterState> buffer = emitterStateBuffers[rootPhenotypeEntity.Value];
            if (emitterIndex >= (ushort)buffer.Length)
                return;

            LocalTransform transformA = localTransformLookup[pair.EntityA];
            LocalTransform transformB = localTransformLookup[pair.EntityB];

            quaternion worldRotA = transformA.Rotation;
            quaternion worldRotB = transformB.Rotation;

            float angle = GetJointAngle(
                worldRotA,
                worldRotB,
                joint.BodyAFromJoint,
                joint.BodyBFromJoint,
                axis
            );
            float emitterValue = math.abs(angleLimit) < 1e-3f ? 0f : math.clamp(angle / angleLimit, -1f, 1f);
            buffer[emitterIndex] = new EmitterState { Value = emitterValue };
        }

        /// <summary>
        /// Extracts signed angle (in radians) between EntityA and EntityB around the joint frame defined by BodyAFromJoint, for the specified axis.
        /// </summary>
        /// <param name="worldRotA">Parent (entity A) world rotation</param>
        /// <param name="worldRotB">Child (entity B) world rotation</param>
        /// <param name="bodyAFromJoint">The BodyAFromJoint frame</param>
        /// <param name="bodyBFromJoint">The BodyBFromJoint frame</param>
        /// <param name="axis">Axis around which to compute the angle</param>
        /// <returns>float: angle around the specified axis of the joint frame, in radians</returns>
        [BurstCompile]
        public static float GetJointAngle(
            in quaternion worldRotA,
            in quaternion worldRotB,
            in BodyFrame bodyAFromJoint,
            in BodyFrame bodyBFromJoint,
            Axis axis
        )
        {
            float3 axisLocal, refLocalA, refLocalB;
            switch (axis)
            {
                case Axis.X:
                    axisLocal = -bodyAFromJoint.Axis;
                    refLocalA = bodyAFromJoint.PerpendicularAxis;
                    refLocalB = bodyBFromJoint.PerpendicularAxis;
                    break;
                case Axis.Y:
                    axisLocal = -bodyAFromJoint.PerpendicularAxis;
                    // the “tertiary” axis = cross(perp, primary)
                    refLocalA = math.cross(bodyAFromJoint.PerpendicularAxis, bodyAFromJoint.Axis);
                    refLocalB = math.cross(bodyBFromJoint.PerpendicularAxis, bodyBFromJoint.Axis);
                    break;
                default: // Z
                    axisLocal = math.normalize(math.cross(bodyAFromJoint.PerpendicularAxis, bodyAFromJoint.Axis));
                    refLocalA = bodyAFromJoint.Axis;
                    refLocalB = bodyBFromJoint.Axis;
                    break;
            }

            float3 worldAxis = math.rotate(worldRotA, axisLocal);
            float3 worldRefA = math.rotate(worldRotA, refLocalA);
            float3 worldRefB = math.rotate(worldRotB, refLocalB);

            float cos = math.dot(worldRefA, worldRefB);
            float sin = math.dot(math.cross(worldRefA, worldRefB), worldAxis);
            return math.atan2(sin, cos);
        }
    }
}
