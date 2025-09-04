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

            if (!xQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new UpdateJointAxisXActuatorJob()
                {
                    EmitterStateBuffers = emitterStatesLookup
                }.ScheduleParallel(state.Dependency);
            }
            if (!zQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new UpdateJointAxisZActuatorJob()
                {
                    EmitterStateBuffers = emitterStatesLookup
                }.ScheduleParallel(state.Dependency);
            }
            if (!xzQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new UpdateJointAxisXZActuatorsJob()
                {
                    EmitterStateBuffers = emitterStatesLookup
                }.ScheduleParallel(state.Dependency);
            }
            if (!xyQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new UpdateJointAxisXYActuatorsJob()
                {
                    EmitterStateBuffers = emitterStatesLookup
                }.ScheduleParallel(state.Dependency);
            }
            if (!xyzQuery.IsEmptyIgnoreFilter)
            {
                state.Dependency = new UpdateJointAxisXYZActuatorsJob()
                {
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
        [ReadOnly] public BufferLookup<EmitterState> EmitterStateBuffers;

        public void Execute(ref PhysicsJoint joint, in RootPhenotypeEntity rootPhenotypeEntity, in JointAxisX jointAxisX)
        {
            if (!EmitterStateBuffers.HasBuffer(rootPhenotypeEntity.Value))
                return;

            DynamicBuffer<EmitterState> buffer = EmitterStateBuffers[rootPhenotypeEntity.Value];
            FixedList512Bytes<Constraint> constraints = joint.GetConstraints();

            JointActuatorUpdateHelper.SetConstraint(ref constraints, 0, ref buffer, jointAxisX.ActuatorNeuronEmitterIndex, jointAxisX.AngleLimit);

            joint.SetConstraints(constraints);
        }
    }

    [BurstCompile]
    [WithAll(typeof(JointAxisZ))]
    [WithNone(typeof(JointAxisX), typeof(JointAxisY))]
    public partial struct UpdateJointAxisZActuatorJob : IJobEntity
    {
        [ReadOnly] public BufferLookup<EmitterState> EmitterStateBuffers;

        public void Execute(ref PhysicsJoint joint, in RootPhenotypeEntity rootPhenotypeEntity, in JointAxisZ jointAxisZ)
        {
            if (!EmitterStateBuffers.HasBuffer(rootPhenotypeEntity.Value))
                return;

            DynamicBuffer<EmitterState> buffer = EmitterStateBuffers[rootPhenotypeEntity.Value];
            FixedList512Bytes<Constraint> constraints = joint.GetConstraints();

            JointActuatorUpdateHelper.SetConstraint(ref constraints, 0, ref buffer, jointAxisZ.ActuatorNeuronEmitterIndex, jointAxisZ.AngleLimit);

            joint.SetConstraints(constraints);
        }
    }

    [BurstCompile]
    [WithAll(typeof(JointAxisX), typeof(JointAxisZ))]
    [WithNone(typeof(JointAxisY))]
    public partial struct UpdateJointAxisXZActuatorsJob : IJobEntity
    {
        [ReadOnly] public BufferLookup<EmitterState> EmitterStateBuffers;

        public void Execute(ref PhysicsJoint joint, in RootPhenotypeEntity rootPhenotypeEntity, in JointAxisX jointAxisX, in JointAxisZ jointAxisZ)
        {
            if (!EmitterStateBuffers.HasBuffer(rootPhenotypeEntity.Value))
                return;

            DynamicBuffer<EmitterState> buffer = EmitterStateBuffers[rootPhenotypeEntity.Value];
            FixedList512Bytes<Constraint> constraints = joint.GetConstraints();

            JointActuatorUpdateHelper.SetConstraint(ref constraints, jointAxisX.SwapXZ == 1 ? 1 : 0, ref buffer, jointAxisX.ActuatorNeuronEmitterIndex, jointAxisX.AngleLimit);
            JointActuatorUpdateHelper.SetConstraint(ref constraints, jointAxisZ.SwapXZ == 1 ? 0 : 1, ref buffer, jointAxisZ.ActuatorNeuronEmitterIndex, jointAxisZ.AngleLimit);

            joint.SetConstraints(constraints);
        }
    }

    [BurstCompile]
    [WithAll(typeof(JointAxisX), typeof(JointAxisY))]
    [WithNone(typeof(JointAxisZ))]
    public partial struct UpdateJointAxisXYActuatorsJob : IJobEntity
    {
        [ReadOnly] public BufferLookup<EmitterState> EmitterStateBuffers;

        public void Execute(ref PhysicsJoint joint, in RootPhenotypeEntity rootPhenotypeEntity, in JointAxisX jointAxisX, in JointAxisY jointAxisY)
        {
            if (!EmitterStateBuffers.HasBuffer(rootPhenotypeEntity.Value))
                return;

            DynamicBuffer<EmitterState> buffer = EmitterStateBuffers[rootPhenotypeEntity.Value];
            FixedList512Bytes<Constraint> constraints = joint.GetConstraints();

            JointActuatorUpdateHelper.SetConstraint(ref constraints, 0, ref buffer, jointAxisX.ActuatorNeuronEmitterIndex, jointAxisX.AngleLimit);
            JointActuatorUpdateHelper.SetConstraint(ref constraints, 1, ref buffer, jointAxisY.ActuatorNeuronEmitterIndex, jointAxisY.AngleLimit);

            joint.SetConstraints(constraints);
        }
    }

    [BurstCompile]
    [WithAll(typeof(JointAxisX), typeof(JointAxisY), typeof(JointAxisZ))]
    public partial struct UpdateJointAxisXYZActuatorsJob : IJobEntity
    {
        [ReadOnly] public BufferLookup<EmitterState> EmitterStateBuffers;

        public void Execute(ref PhysicsJoint joint, in RootPhenotypeEntity rootPhenotypeEntity, in JointAxisX jointAxisX, in JointAxisY jointAxisY, in JointAxisZ jointAxisZ)
        {
            if (!EmitterStateBuffers.HasBuffer(rootPhenotypeEntity.Value))
                return;

            DynamicBuffer<EmitterState> buffer = EmitterStateBuffers[rootPhenotypeEntity.Value];
            FixedList512Bytes<Constraint> constraints = joint.GetConstraints();

            JointActuatorUpdateHelper.SetConstraint(ref constraints, 0, ref buffer, jointAxisX.ActuatorNeuronEmitterIndex, jointAxisX.AngleLimit);
            JointActuatorUpdateHelper.SetConstraint(ref constraints, 1, ref buffer, jointAxisY.ActuatorNeuronEmitterIndex, jointAxisY.AngleLimit);
            JointActuatorUpdateHelper.SetConstraint(ref constraints, 2, ref buffer, jointAxisZ.ActuatorNeuronEmitterIndex, jointAxisZ.AngleLimit);

            joint.SetConstraints(constraints);
        }
    }

    [BurstCompile]
    public static class JointActuatorUpdateHelper
    {
        [BurstCompile]
        public static void SetConstraint(ref FixedList512Bytes<Constraint> constraints, int constraintIndex, ref DynamicBuffer<EmitterState> buffer, ushort emitterIndex, float angleLimit)
        {
            if (emitterIndex >= (ushort)buffer.Length)
                return;
            ref Constraint constraint = ref constraints.ElementAt(constraintIndex);
            constraint.Target = math.clamp(buffer[emitterIndex].Value, -1f, 1f) * angleLimit; // [-1, 1] to [-angleLimit, angleLimit].
        }
    }
}
