using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Sensors
{
    using Core.ECS.Components.Shared;
    using Components.Phenotype;

    public struct ContactContribution
    {
        public Entity Limb;
        public float Load;
        public float3 LocalNormal;
        public float Slip;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(UpdateSensorsSystemGroup))]
    public partial struct UpdateContactSensorsSystem : ISystem
    {
        private BufferLookup<EmitterState> emitterStatesLookup;
        private NativeList<ContactContribution> contributions;

        public void OnCreate(ref SystemState state)
        {
            emitterStatesLookup = state.GetBufferLookup<EmitterState>(isReadOnly: false);

            contributions = new NativeList<ContactContribution>(128, Allocator.Persistent);

            state.RequireForUpdate<ContactSensors>();
            state.RequireForUpdate<RootPhenotypeEntity>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            PhenotypeEntitiesMetadata metadata = SystemAPI.GetSingleton<PhenotypeEntitiesMetadata>();
            int requiredCapacity = metadata.TotalLimbCount * 4; // High estimate of number of contact collisions.
            if (requiredCapacity > contributions.Capacity)
                contributions.Capacity = requiredCapacity;

            contributions.Clear();

            emitterStatesLookup.Update(ref state);

            PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            SimulationSingleton simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();

            // Reset contact sensor values to zero.
            state.Dependency = new ResetContactSensorsJob().ScheduleParallel(state.Dependency);

            // Collect collision events.
            state.Dependency = new CollectContactsJob()
            {
                PhysicsWorld = physicsWorld,
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true),
                PhysicsVelocityLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(isReadOnly: true),
                PhysicsMassLookup = SystemAPI.GetComponentLookup<PhysicsMass>(isReadOnly: true),
                ContactSensorsLookup = SystemAPI.GetComponentLookup<ContactSensors>(isReadOnly: true),
                Contributions = contributions.AsParallelWriter()
            }.Schedule(simulationSingleton, state.Dependency);

            // Accumulate contact contributions and write to sensor components.
            state.Dependency = new ApplyContactsJob
            {
                ContactSensorsLookup = SystemAPI.GetComponentLookup<ContactSensors>(isReadOnly: false),
                Contributions = contributions
            }.Schedule(state.Dependency);

            // Finalize sensor values and write to emitter states.
            state.Dependency = new FinaliseContactsJob()
            {
                EmitterStateBuffers = emitterStatesLookup
            }.ScheduleParallel(state.Dependency);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (contributions.IsCreated)
                contributions.Dispose();
        }
    }

    [BurstCompile]
    public partial struct ResetContactSensorsJob : IJobEntity
    {
        public void Execute(ref ContactSensors contactSensors)
        {
            contactSensors.TotalLoad = 0f;
            contactSensors.Slip = 0f;
            contactSensors.DirectionalLoad = float3.zero;
        }
    }

    [BurstCompile]
    public partial struct CollectContactsJob : ICollisionEventsJob
    {
        [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
        [ReadOnly] public ComponentLookup<PhysicsVelocity> PhysicsVelocityLookup;
        [ReadOnly] public ComponentLookup<PhysicsMass> PhysicsMassLookup;
        [ReadOnly] public ComponentLookup<ContactSensors> ContactSensorsLookup;

        public NativeList<ContactContribution>.ParallelWriter Contributions;

        public void Execute(CollisionEvent e)
        {
            Entity entityA = PhysicsWorld.Bodies[e.BodyIndexA].Entity;
            Entity entityB = PhysicsWorld.Bodies[e.BodyIndexB].Entity;

            bool aIsLimb = ContactSensorsLookup.HasComponent(entityA);
            bool bIsLimb = ContactSensorsLookup.HasComponent(entityB);

            if (!aIsLimb && !bIsLimb) return;

            CollisionEvent.Details details = e.CalculateDetails(ref PhysicsWorld.PhysicsWorld);
            float load = math.max(0f, details.EstimatedImpulse);
            float3 worldNormal = e.Normal; // B -> A


            if (aIsLimb && LocalToWorldLookup.HasComponent(entityA))
            {
                float3 localNormalA = math.rotate(math.inverse(LocalToWorldLookup[entityA].Rotation), worldNormal);
                float slipA = ComputeSlip(entityA, entityB, details, worldNormal);

                Contributions.AddNoResize(new ContactContribution
                {
                    Limb = entityA,
                    Load = load,
                    LocalNormal = localNormalA,
                    Slip = slipA
                });
            }

            if (bIsLimb && LocalToWorldLookup.HasComponent(entityB))
            {
                float3 localNormalB = math.rotate(math.inverse(LocalToWorldLookup[entityB].Rotation), -worldNormal);
                float slipB = ComputeSlip(entityB, entityA, details, -worldNormal);

                Contributions.AddNoResize(new ContactContribution
                {
                    Limb = entityB,
                    Load = load,
                    LocalNormal = localNormalB,
                    Slip = slipB
                });
            }
        }

        [BurstCompile]
        private float ComputeSlip(Entity limb, Entity other, CollisionEvent.Details d, float3 n_ws_forLimb)
        {
            float3 p = d.AverageContactPointPosition;
            float3 vL = 0, wL = 0, comL = LocalToWorldLookup.HasComponent(limb) ? LocalToWorldLookup[limb].Position : float3.zero;
            float3 vO = 0, wO = 0, comO = LocalToWorldLookup.HasComponent(other) ? LocalToWorldLookup[other].Position : float3.zero;

            if (PhysicsVelocityLookup.HasComponent(limb)) { var v = PhysicsVelocityLookup[limb]; vL = v.Linear; wL = v.Angular; }
            if (PhysicsVelocityLookup.HasComponent(other)) { var v = PhysicsVelocityLookup[other]; vO = v.Linear; wO = v.Angular; }

            if (PhysicsMassLookup.HasComponent(limb) && LocalToWorldLookup.HasComponent(limb))
                comL = math.transform(LocalToWorldLookup[limb].Value, PhysicsMassLookup[limb].CenterOfMass);

            if (PhysicsMassLookup.HasComponent(other) && LocalToWorldLookup.HasComponent(other))
                comO = math.transform(LocalToWorldLookup[other].Value, PhysicsMassLookup[other].CenterOfMass);

            float3 vLp = vL + math.cross(wL, p - comL);
            float3 vOp = vO + math.cross(wO, p - comO);
            float3 vrel = vLp - vOp;

            float3 vtan = vrel - math.dot(vrel, n_ws_forLimb) * n_ws_forLimb;
            return math.length(vtan);
        }
    }

    [BurstCompile]
    public struct ApplyContactsJob : IJob
    {
        public ComponentLookup<ContactSensors> ContactSensorsLookup;
        [ReadOnly] public NativeList<ContactContribution> Contributions;

        public void Execute()
        {
            for (int i = 0; i < Contributions.Length; i++)
            {
                var c = Contributions[i];

                if (!ContactSensorsLookup.HasComponent(c.Limb))
                    continue;

                ref ContactSensors s = ref ContactSensorsLookup.GetRefRW(c.Limb).ValueRW;
                s.TotalLoad += c.Load;
                s.DirectionalLoad += c.Load * c.LocalNormal;
                s.Slip += c.Load * c.Slip;
            }
        }
    }

    [BurstCompile]
    public partial struct FinaliseContactsJob : IJobEntity
    {
        [NativeDisableParallelForRestriction] public BufferLookup<EmitterState> EmitterStateBuffers;

        public const float loadScale = 1.0f;
        public const float slipScale = 0.1f;
        private const float epsilon = 1e-5f;

        public void Execute(ref ContactSensors contactSensors, in RootPhenotypeEntity rootPhenotypeEntity)
        {
            float totalLoad = math.saturate(contactSensors.TotalLoad * loadScale);

            float slip = (contactSensors.TotalLoad > epsilon) ? (contactSensors.Slip / contactSensors.TotalLoad) : 0f;
            slip = math.saturate(slip * slipScale);

            float xAxisLoad = math.saturate(contactSensors.DirectionalLoad.x);
            float yAxisLoad = math.saturate(contactSensors.DirectionalLoad.y);
            float zAxisLoad = math.saturate(contactSensors.DirectionalLoad.z);

            contactSensors.TotalLoad = totalLoad;
            contactSensors.Slip = slip;
            contactSensors.DirectionalLoad = new float3(xAxisLoad, yAxisLoad, zAxisLoad);

            if (!EmitterStateBuffers.HasBuffer(rootPhenotypeEntity.Value))
                return;

            DynamicBuffer<EmitterState> buffer = EmitterStateBuffers[rootPhenotypeEntity.Value];

            if (contactSensors.TotalLoadSensorEmitterIndex < (ushort)buffer.Length)
                buffer[contactSensors.TotalLoadSensorEmitterIndex] = new EmitterState { Value = totalLoad };
            if (contactSensors.SlipSensorEmitterIndex < (ushort)buffer.Length)
                buffer[contactSensors.SlipSensorEmitterIndex] = new EmitterState { Value = slip };
            if (contactSensors.XAxisLoadSensorEmitterIndex < (ushort)buffer.Length)
                buffer[contactSensors.XAxisLoadSensorEmitterIndex] = new EmitterState { Value = xAxisLoad };
            if (contactSensors.YAxisLoadSensorEmitterIndex < (ushort)buffer.Length)
                buffer[contactSensors.YAxisLoadSensorEmitterIndex] = new EmitterState { Value = yAxisLoad };
            if (contactSensors.ZAxisLoadSensorEmitterIndex < (ushort)buffer.Length)
                buffer[contactSensors.ZAxisLoadSensorEmitterIndex] = new EmitterState { Value = zAxisLoad };
        }
    }
}
