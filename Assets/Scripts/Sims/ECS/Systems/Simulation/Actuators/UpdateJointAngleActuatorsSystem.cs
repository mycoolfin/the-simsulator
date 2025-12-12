using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Actuators
{
    using Core.ECS.Components.Shared;
    using Components.Phenotype;
    using UnityEngine;

    [BurstCompile]
    [UpdateInGroup(typeof(UpdateActuatorsSystemGroup))]
    public partial struct UpdateJointAxisActuatorsSystem : ISystem
    {
        private BufferLookup<EmitterState> emitterStatesLookup;

        public void OnCreate(ref SystemState state)
        {
            emitterStatesLookup = state.GetBufferLookup<EmitterState>(isReadOnly: true);

            state.RequireForUpdate<PhysicsJoint>();
            state.RequireForUpdate<RootPhenotypeEntity>();
            state.RequireForUpdate<JointAngleActuators>();
        }

        public void OnUpdate(ref SystemState state)
        {
            emitterStatesLookup.Update(ref state);

            state.Dependency = new UpdateJointAngleActuatorsJob()
            {
                EmitterStateBuffers = emitterStatesLookup
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct UpdateJointAngleActuatorsJob : IJobEntity
    {
        [ReadOnly] public BufferLookup<EmitterState> EmitterStateBuffers;

        private const float MAX_ANGULAR_VELOCITY = 2 * Mathf.PI;

        public void Execute(ref PhysicsJoint joint, in RootPhenotypeEntity rootPhenotypeEntity, ref JointAngleActuators a)
        {
            if (!EmitterStateBuffers.HasBuffer(rootPhenotypeEntity.Value))
                return;

            DynamicBuffer<EmitterState> buffer = EmitterStateBuffers[rootPhenotypeEntity.Value];
            FixedList512Bytes<Constraint> constraints = joint.GetConstraints();

            float primaryTargetVelocity = 0f;
            float secondaryTargetVelocity = 0f;
            float tertiaryTargetVelocity = 0f;
            if (a.PrimaryMotorConstraintIndex >= 0)
            {
                CalculateTargetVelocity(ref buffer, a.PrimaryActuatorNeuronEmitterIndex, out primaryTargetVelocity);
                ref Constraint c = ref constraints.ElementAt(a.PrimaryMotorConstraintIndex);
                c.Target = primaryTargetVelocity;
            }
            if (a.SecondaryMotorConstraintIndex >= 0)
            {
                CalculateTargetVelocity(ref buffer, a.SecondaryActuatorNeuronEmitterIndex, out secondaryTargetVelocity);
                ref Constraint c = ref constraints.ElementAt(a.SecondaryMotorConstraintIndex);
                c.Target = secondaryTargetVelocity;
            }
            if (a.TertiaryMotorConstraintIndex >= 0)
            {
                CalculateTargetVelocity(ref buffer, a.TertiaryActuatorNeuronEmitterIndex, out tertiaryTargetVelocity);
                ref Constraint c = ref constraints.ElementAt(a.TertiaryMotorConstraintIndex);
                c.Target = tertiaryTargetVelocity;
            }
            
            a.TargetAngularVelocities = new(primaryTargetVelocity, secondaryTargetVelocity, tertiaryTargetVelocity);

            joint.SetConstraints(constraints);
        }

        [BurstCompile]
        public static void CalculateTargetVelocity(ref DynamicBuffer<EmitterState> buffer, ushort emitterIndex, out float targetVelocity)
        {
            if (emitterIndex >= (ushort)buffer.Length)
            {
                targetVelocity = 0f;
                return;
            }

            // Get the raw neural signal and convert to target angle.
            float rawSignal = math.clamp(buffer[emitterIndex].Value, -1f, 1f);
            targetVelocity = rawSignal * MAX_ANGULAR_VELOCITY;
        }
    }
}
