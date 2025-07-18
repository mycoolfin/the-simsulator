using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

public struct FreezeRootLimbsRequest : IComponentData
{
    public byte Value;
}

[BurstCompile]
[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
public partial struct FreezeRootLimbsSystem : ISystem
{
    public readonly void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FreezeRootLimbsRequest>();
    }

    public void OnUpdate(ref SystemState state)
    {
        new FreezeRootLimbsJob().ScheduleParallel(state.Dependency).Complete();
        // Request entity is manually destroyed.
    }
}

[BurstCompile]
public partial struct FreezeRootLimbsJob : IJobEntity
{
    public void Execute(in LimbIndex limbIndex, ref PhysicsVelocity velocity)
    {
        if (limbIndex.Value == 0)
        {
            velocity.Linear = float3.zero;
            velocity.Angular = float3.zero;
        }
    }
}
