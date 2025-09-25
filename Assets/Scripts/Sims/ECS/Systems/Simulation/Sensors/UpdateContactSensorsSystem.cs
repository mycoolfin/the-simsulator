using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Sensors
{
    using Core.ECS.Components.Shared;
    using Components.Phenotype;

    [BurstCompile]
    [UpdateInGroup(typeof(UpdateSensorsSystemGroup))]
    public partial struct UpdateContactSensorsSystem : ISystem
    {
        private BufferLookup<EmitterState> emitterStatesLookup;

        public void OnCreate(ref SystemState state)
        {
            emitterStatesLookup = state.GetBufferLookup<EmitterState>(isReadOnly: false);

            state.RequireForUpdate<LocalTransform>();
            state.RequireForUpdate<ContactSensors>();
        }

        public void OnUpdate(ref SystemState state)
        {
            emitterStatesLookup.Update(ref state);


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
        public NativeStream.Writer Writer;
        [NativeSetThreadIndex] private int threadIndex;

        public void Execute(CollisionEvent e)
        {
            Entity entityA = PhysicsWorld.Bodies[e.BodyIndexA].Entity;
            Entity entityB = PhysicsWorld.Bodies[e.BodyIndexB].Entity;

            bool aIsLimb = ContactSensorsLookup.HasComponent(entityA);
            bool bIsLimb = ContactSensorsLookup.HasComponent(entityB);
            if (!aIsLimb && !bIsLimb) return;

            CollisionEvent.Details details = e.CalculateDetails(ref PhysicsWorld.PhysicsWorld);
            float load = math.max(0f, details.EstimatedImpulse);
            float3 worldNormal = e.Normal; // Points from B -> A.

            Writer.BeginForEachIndex(threadIndex);

            if (aIsLimb && LocalToWorldLookup.HasComponent(entityA))
            {
                float3 localNormalA = math.rotate(math.inverse(LocalToWorldLookup[entityA].Rotation), worldNormal);
                float slipA = ComputeSlip(entityA, entityB, details, worldNormal);
                Writer.Write(entityA);
                Writer.Write(load);
                Writer.Write(localNormalA);
                Writer.Write(slipA);
            }

            if (bIsLimb && LocalToWorldLookup.HasComponent(entityB))
            {
                // Flip the normal so it points into B.
                float3 localNormalB = math.rotate(math.inverse(LocalToWorldLookup[entityB].Rotation), -worldNormal);
                float slipB = ComputeSlip(entityB, entityA, details, -worldNormal);
                Writer.Write(entityB);
                Writer.Write(load);
                Writer.Write(localNormalB);
                Writer.Write(slipB);
            }

            Writer.EndForEachIndex();
        }

        [BurstCompile]
        private float ComputeSlip(Entity limb, Entity other, CollisionEvent.Details d, float3 n_ws_forLimb)
        {
            float3 p = d.AverageContactPointPosition;
            float3 vL = 0, wL = 0, comL = LocalToWorldLookup.HasComponent(limb) ? LocalToWorldLookup[limb].Position : float3.zero;
            float3 vO = 0, wO = 0, comO = LocalToWorldLookup.HasComponent(other) ? LocalToWorldLookup[other].Position : float3.zero;

            if (PhysicsVelocityLookup.HasComponent(limb)) { var v = PhysicsVelocityLookup[limb]; vL = v.Linear; wL = v.Angular; }
            if (PhysicsVelocityLookup.HasComponent(other)) { var v = PhysicsVelocityLookup[other]; vO = v.Linear; wO = v.Angular; }
            if (PhysicsMassLookup.HasComponent(limb)) comL = math.transform(LocalToWorldLookup[limb].Value, PhysicsMassLookup[limb].CenterOfMass);
            if (PhysicsMassLookup.HasComponent(other)) comO = math.transform(LocalToWorldLookup[other].Value, PhysicsMassLookup[other].CenterOfMass);

            float3 vLp = vL + math.cross(wL, p - comL);
            float3 vOp = vO + math.cross(wO, p - comO);
            float3 vrel = vLp - vOp;

            // Remove normal component; keep tangential speed.
            float3 vtan = vrel - math.dot(vrel, n_ws_forLimb) * n_ws_forLimb;
            return math.length(vtan);
        }
    }

    [BurstCompile]
    public struct ApplyContactsJob : IJob
    {
        public ComponentLookup<ContactSensors> ContactSensorsLookup;
        public NativeStream.Reader Reader;

        public void Execute()
        {
            for (int lane = 0; lane < Reader.ForEachCount; lane++)
            {
                int itemCount = Reader.BeginForEachIndex(lane);

                for (int i = 0; i < itemCount; i++)
                {
                    Entity limb = Reader.Read<Entity>();
                    float load = Reader.Read<float>();
                    float3 localNormal = Reader.Read<float3>();
                    float slip = Reader.Read<float>();

                    if (!ContactSensorsLookup.HasComponent(limb))
                        continue; // Skip if limb no longer exists / lacks component.

                    ref ContactSensors s = ref ContactSensorsLookup.GetRefRW(limb).ValueRW;

                    // Accumulate totals, normalise later.
                    s.TotalLoad += load;
                    s.DirectionalLoad += load * localNormal;
                    s.Slip += load * slip;
                }

                Reader.EndForEachIndex();
            }
        }
    }

    [BurstCompile]
    public partial struct FinaliseContactsJob : IJobEntity
    {
        [NativeDisableParallelForRestriction] public BufferLookup<EmitterState> EmitterStateBuffers;

        public float LoadScale;
        public float SlipScale;
        private const float epsilon = 1e-5f;

        public void Execute(ref ContactSensors contactSensors, in RootPhenotypeEntity rootPhenotypeEntity)
        {
            float totalLoad = math.saturate(contactSensors.TotalLoad * LoadScale);

            float slip = (contactSensors.TotalLoad > epsilon) ? (contactSensors.Slip / contactSensors.TotalLoad) : 0f;
            slip = math.saturate(slip * SlipScale);

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
