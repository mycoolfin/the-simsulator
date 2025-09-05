using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Actuators
{
    using Core.ECS.Components.Shared;
    using Components.Phenotype;

    [BurstCompile]
    [UpdateInGroup(typeof(UpdateActuatorsSystemGroup))]
    public partial struct UpdateJointAxisActuatorsSystem : ISystem
    {
        private BufferLookup<EmitterState> emitterStatesLookup;
        private EntityQuery xQuery;
        private EntityQuery zQuery;
        private EntityQuery xzQuery;
        private EntityQuery xyQuery;
        private EntityQuery xyzQuery;

        public void OnCreate(ref SystemState state)
        {
            emitterStatesLookup = state.GetBufferLookup<EmitterState>(isReadOnly: true);

            EntityQuery commonEntityQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                ComponentType.ReadWrite<PhysicsJoint>(),
                ComponentType.ReadOnly<RootPhenotypeEntity>()
            }
            });

            ComponentType jointAxisXType = ComponentType.ReadOnly<JointAxisX>();
            ComponentType jointAxisYType = ComponentType.ReadOnly<JointAxisY>();
            ComponentType jointAxisZType = ComponentType.ReadOnly<JointAxisZ>();
            xQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { jointAxisXType },
                None = new[] { jointAxisYType, jointAxisZType }
            });
            zQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { jointAxisZType },
                None = new[] { jointAxisXType, jointAxisYType }
            });
            xzQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { jointAxisXType, jointAxisZType },
                None = new[] { jointAxisYType }
            });
            xyQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { jointAxisXType, jointAxisYType },
                None = new[] { jointAxisZType }
            });
            xyzQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { jointAxisXType, jointAxisYType, jointAxisZType }
            });

            state.RequireForUpdate(commonEntityQuery);
            state.RequireAnyForUpdate(xQuery, zQuery, xzQuery, xyQuery, xyzQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            emitterStatesLookup.Update(ref state);

            FixedStepSimulationSystemGroup fixedStepGroup = state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            float deltaTime = fixedStepGroup.World.Time.DeltaTime;

            if (!xQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new UpdateJointAxisXActuatorJob()
                {
                    DeltaTime = deltaTime,
                    EmitterStateBuffers = emitterStatesLookup
                }.ScheduleParallel(state.Dependency);
            }
            if (!zQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new UpdateJointAxisZActuatorJob()
                {
                    DeltaTime = deltaTime,
                    EmitterStateBuffers = emitterStatesLookup
                }.ScheduleParallel(state.Dependency);
            }
            if (!xzQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new UpdateJointAxisXZActuatorsJob()
                {
                    DeltaTime = deltaTime,
                    EmitterStateBuffers = emitterStatesLookup
                }.ScheduleParallel(state.Dependency);
            }
            if (!xyQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new UpdateJointAxisXYActuatorsJob()
                {
                    DeltaTime = deltaTime,
                    EmitterStateBuffers = emitterStatesLookup
                }.ScheduleParallel(state.Dependency);
            }
            if (!xyzQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new UpdateJointAxisXYZActuatorsJob()
                {
                    DeltaTime = deltaTime,
                    EmitterStateBuffers = emitterStatesLookup
                }.ScheduleParallel(state.Dependency);
            }
        }
    }

    [BurstCompile]
    [WithAll(typeof(JointAxisX))]
    [WithNone(typeof(JointAxisY), typeof(JointAxisZ))]
    public partial struct UpdateJointAxisXActuatorJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public BufferLookup<EmitterState> EmitterStateBuffers;

        public void Execute(ref PhysicsJoint joint, in RootPhenotypeEntity rootPhenotypeEntity, in JointAxisX jointAxisX)
        {
            if (!EmitterStateBuffers.HasBuffer(rootPhenotypeEntity.Value))
                return;

            DynamicBuffer<EmitterState> buffer = EmitterStateBuffers[rootPhenotypeEntity.Value];
            FixedList512Bytes<Constraint> constraints = joint.GetConstraints();

            JointActuatorUpdateHelper.SetConstraint(ref constraints, 0, ref buffer, jointAxisX.ActuatorNeuronEmitterIndex, jointAxisX.AngleLimit, DeltaTime);

            joint.SetConstraints(constraints);
        }
    }

    [BurstCompile]
    [WithAll(typeof(JointAxisZ))]
    [WithNone(typeof(JointAxisX), typeof(JointAxisY))]
    public partial struct UpdateJointAxisZActuatorJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public BufferLookup<EmitterState> EmitterStateBuffers;

        public void Execute(ref PhysicsJoint joint, in RootPhenotypeEntity rootPhenotypeEntity, in JointAxisZ jointAxisZ)
        {
            if (!EmitterStateBuffers.HasBuffer(rootPhenotypeEntity.Value))
                return;

            DynamicBuffer<EmitterState> buffer = EmitterStateBuffers[rootPhenotypeEntity.Value];
            FixedList512Bytes<Constraint> constraints = joint.GetConstraints();

            JointActuatorUpdateHelper.SetConstraint(ref constraints, 0, ref buffer, jointAxisZ.ActuatorNeuronEmitterIndex, jointAxisZ.AngleLimit, DeltaTime);

            joint.SetConstraints(constraints);
        }
    }

    [BurstCompile]
    [WithAll(typeof(JointAxisX), typeof(JointAxisZ))]
    [WithNone(typeof(JointAxisY))]
    public partial struct UpdateJointAxisXZActuatorsJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public BufferLookup<EmitterState> EmitterStateBuffers;

        public void Execute(ref PhysicsJoint joint, in RootPhenotypeEntity rootPhenotypeEntity, in JointAxisX jointAxisX, in JointAxisZ jointAxisZ)
        {
            if (!EmitterStateBuffers.HasBuffer(rootPhenotypeEntity.Value))
                return;

            DynamicBuffer<EmitterState> buffer = EmitterStateBuffers[rootPhenotypeEntity.Value];
            FixedList512Bytes<Constraint> constraints = joint.GetConstraints();

            JointActuatorUpdateHelper.SetConstraint(ref constraints, jointAxisX.SwapXZ == 1 ? 1 : 0, ref buffer, jointAxisX.ActuatorNeuronEmitterIndex, jointAxisX.AngleLimit, DeltaTime);
            JointActuatorUpdateHelper.SetConstraint(ref constraints, jointAxisZ.SwapXZ == 1 ? 0 : 1, ref buffer, jointAxisZ.ActuatorNeuronEmitterIndex, jointAxisZ.AngleLimit, DeltaTime);

            joint.SetConstraints(constraints);
        }
    }

    [BurstCompile]
    [WithAll(typeof(JointAxisX), typeof(JointAxisY))]
    [WithNone(typeof(JointAxisZ))]
    public partial struct UpdateJointAxisXYActuatorsJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public BufferLookup<EmitterState> EmitterStateBuffers;

        public void Execute(ref PhysicsJoint joint, in RootPhenotypeEntity rootPhenotypeEntity, in JointAxisX jointAxisX, in JointAxisY jointAxisY)
        {
            if (!EmitterStateBuffers.HasBuffer(rootPhenotypeEntity.Value))
                return;

            DynamicBuffer<EmitterState> buffer = EmitterStateBuffers[rootPhenotypeEntity.Value];
            FixedList512Bytes<Constraint> constraints = joint.GetConstraints();

            JointActuatorUpdateHelper.SetConstraint(ref constraints, 0, ref buffer, jointAxisX.ActuatorNeuronEmitterIndex, jointAxisX.AngleLimit, DeltaTime);
            JointActuatorUpdateHelper.SetConstraint(ref constraints, 1, ref buffer, jointAxisY.ActuatorNeuronEmitterIndex, jointAxisY.AngleLimit, DeltaTime);

            joint.SetConstraints(constraints);
        }
    }

    [BurstCompile]
    [WithAll(typeof(JointAxisX), typeof(JointAxisY), typeof(JointAxisZ))]
    public partial struct UpdateJointAxisXYZActuatorsJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public BufferLookup<EmitterState> EmitterStateBuffers;

        public void Execute(ref PhysicsJoint joint, in RootPhenotypeEntity rootPhenotypeEntity, in JointAxisX jointAxisX, in JointAxisY jointAxisY, in JointAxisZ jointAxisZ)
        {
            if (!EmitterStateBuffers.HasBuffer(rootPhenotypeEntity.Value))
                return;

            DynamicBuffer<EmitterState> buffer = EmitterStateBuffers[rootPhenotypeEntity.Value];
            FixedList512Bytes<Constraint> constraints = joint.GetConstraints();

            JointActuatorUpdateHelper.SetConstraint(ref constraints, 0, ref buffer, jointAxisX.ActuatorNeuronEmitterIndex, jointAxisX.AngleLimit, DeltaTime);
            JointActuatorUpdateHelper.SetConstraint(ref constraints, 1, ref buffer, jointAxisY.ActuatorNeuronEmitterIndex, jointAxisY.AngleLimit, DeltaTime);
            JointActuatorUpdateHelper.SetConstraint(ref constraints, 2, ref buffer, jointAxisZ.ActuatorNeuronEmitterIndex, jointAxisZ.AngleLimit, DeltaTime);

            joint.SetConstraints(constraints);
        }
    }

    [BurstCompile]
    public static class JointActuatorUpdateHelper
    {
        private const float MAX_ANGULAR_VELOCITY = 15.0f; // Radians per second.
        private const float FILTER_ALPHA = 0.8f; // Low-pass filter coefficient (higher = more responsive).

        [BurstCompile]
        public static void SetConstraint(ref FixedList512Bytes<Constraint> constraints, int constraintIndex, ref DynamicBuffer<EmitterState> buffer, ushort emitterIndex, float angleLimit, float deltaTime)
        {
            if (emitterIndex >= (ushort)buffer.Length)
                return;
            ref Constraint constraint = ref constraints.ElementAt(constraintIndex);

            // Get the raw neural signal and convert to target angle.
            float rawSignal = math.clamp(buffer[emitterIndex].Value, -1f, 1f);
            float desiredTargetAngle = rawSignal * angleLimit;

            // Apply rate limiting - prevent changes faster than MAX_ANGULAR_VELOCITY.
            float maxAngleChange = MAX_ANGULAR_VELOCITY * deltaTime;

            // Get the current target angle for this constraint (assuming single-axis constraint)
            float currentTargetAngle = constraint.Target.x;
            float angleDifference = desiredTargetAngle - currentTargetAngle;
            float rateLimitedAngleDifference = math.clamp(angleDifference, -maxAngleChange, maxAngleChange);
            float rateLimitedTarget = currentTargetAngle + rateLimitedAngleDifference;

            // Apply low-pass filter to smooth out remaining high-frequency components.
            float filteredTarget = math.lerp(currentTargetAngle, rateLimitedTarget, FILTER_ALPHA);

            // Update the constraint target (keeping other components unchanged).
            constraint.Target = filteredTarget;
        }
    }
}
