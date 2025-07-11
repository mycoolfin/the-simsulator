using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

public struct ZeroAllLimbVelocitiesRequest : IComponentData { }

[BurstCompile]
[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateBefore(typeof(PhysicsInitializeGroup))]
public partial struct ZeroAllLimbVelocities : ISystem
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