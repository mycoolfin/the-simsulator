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

        public void OnCreate(ref SystemState state)
        {
            emitterStatesLookup = state.GetBufferLookup<EmitterState>(isReadOnly: true);

            state.RequireForUpdate<PhysicsJoint>();
            state.RequireForUpdate<RootPhenotypeEntity>();
            state.RequireForUpdate<JointAngleActuators>();
        }

        public void OnUpdate(ref SystemState state)
        {
            FixedStepSimulationSystemGroup fixedStepGroup = state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            float deltaTime = fixedStepGroup.World.Time.DeltaTime;

            emitterStatesLookup.Update(ref state);

            state.Dependency = new UpdateJointAngleActuatorsJob()
            {
                DeltaTime = deltaTime,
                EmitterStateBuffers = emitterStatesLookup
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct UpdateJointAngleActuatorsJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public BufferLookup<EmitterState> EmitterStateBuffers;

        private const float MAX_ANGULAR_VELOCITY = 15.0f; // Radians per second.
        private const float FILTER_ALPHA = 0.8f; // Low-pass filter coefficient (higher = more responsive).

        public void Execute(ref PhysicsJoint joint, in RootPhenotypeEntity rootPhenotypeEntity, ref JointAngleActuators a)
        {
            if (!EmitterStateBuffers.HasBuffer(rootPhenotypeEntity.Value))
                return;

            DynamicBuffer<EmitterState> buffer = EmitterStateBuffers[rootPhenotypeEntity.Value];
            FixedList512Bytes<Constraint> constraints = joint.GetConstraints();

            float primaryTargetAngle = 0f;
            float secondaryTargetAngle = 0f;
            float tertiaryTargetAngle = 0f;
            if (a.PrimaryMotorConstraintIndex >= 0)
            {
                ref Constraint c = ref constraints.ElementAt(a.PrimaryMotorConstraintIndex);
                CalculateTargetAngle(ref buffer, a.PrimaryActuatorNeuronEmitterIndex, DeltaTime, a.AngleLimits.x, c.Target.x, out primaryTargetAngle);
                c.Target = primaryTargetAngle;
            }
            if (a.SecondaryMotorConstraintIndex >= 0)
            {
                ref Constraint c = ref constraints.ElementAt(a.SecondaryMotorConstraintIndex);
                CalculateTargetAngle(ref buffer, a.SecondaryActuatorNeuronEmitterIndex, DeltaTime, a.AngleLimits.y, c.Target.y, out secondaryTargetAngle);
                c.Target = secondaryTargetAngle;
            }
            if (a.TertiaryMotorConstraintIndex >= 0)
            {
                ref Constraint c = ref constraints.ElementAt(a.TertiaryMotorConstraintIndex);
                CalculateTargetAngle(ref buffer, a.TertiaryActuatorNeuronEmitterIndex, DeltaTime, a.AngleLimits.z, c.Target.z, out tertiaryTargetAngle);
                c.Target = tertiaryTargetAngle;
            }
            
            a.TargetAngularVelocities = new(primaryTargetAngle, secondaryTargetAngle, tertiaryTargetAngle);

            joint.SetConstraints(constraints);
        }

        [BurstCompile]
        public static void CalculateTargetAngle(ref DynamicBuffer<EmitterState> buffer, ushort emitterIndex, in float deltaTime, in float angleLimit, in float currentTargetAngle, out float targetAngle)
        {
            if (emitterIndex >= (ushort)buffer.Length)
            {
                targetAngle = 0f;
                return;
            }

            // Get the raw neural signal and convert to target angle.
            float rawSignal = math.clamp(buffer[emitterIndex].Value, -1f, 1f);
            float desiredTargetAngle = rawSignal * angleLimit;

            // Apply rate limiting - prevent changes faster than MAX_ANGULAR_VELOCITY.
            float maxAngleChange = MAX_ANGULAR_VELOCITY * deltaTime;
            float angleDifference = desiredTargetAngle - currentTargetAngle;
            float rateLimitedAngleDifference = math.clamp(angleDifference, -maxAngleChange, maxAngleChange);
            float rateLimitedTarget = currentTargetAngle + rateLimitedAngleDifference;

            // Apply low-pass filter to smooth out remaining high-frequency components.
            float filteredTarget = math.lerp(currentTargetAngle, rateLimitedTarget, FILTER_ALPHA);

            targetAngle = filteredTarget;
        }
    }
}
