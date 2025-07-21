using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.Systems.Simulation.Limbs
{
    using Components.Phenotype;

    public struct ZeroAllLimbVelocitiesRequest : IComponentData
    {
        public byte Value;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
    public partial struct ZeroAllLimbVelocitiesSystem : ISystem
    {
        public readonly void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ZeroAllLimbVelocitiesRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            new ZeroAllLimbVelocitiesJob().ScheduleParallel(state.Dependency).Complete();
            state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<ZeroAllLimbVelocitiesRequest>());
        }
    }

    [BurstCompile]
    [WithAll(typeof(LimbIndex), typeof(PhysicsVelocity))]
    public partial struct ZeroAllLimbVelocitiesJob : IJobEntity
    {
        public void Execute(ref PhysicsVelocity velocity)
        {
            velocity.Linear = float3.zero;
            velocity.Angular = float3.zero;
        }
    }
}
