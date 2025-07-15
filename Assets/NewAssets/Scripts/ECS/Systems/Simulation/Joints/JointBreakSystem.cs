using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

public struct JointBreakSystemSettings : IComponentData
{
    public bool Enabled;
}

public struct JointBrokenEvent : IBufferElementData
{
    public Entity DetachedLimbEntity;
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct JointBreakSystem : ISystem
{
    private ComponentLookup<LocalTransform> localTransformLookup;
    private Entity eventBufferEntity;

    public void OnCreate(ref SystemState state)
    {
        eventBufferEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddBuffer<JointBrokenEvent>(eventBufferEntity);

        localTransformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);

        state.RequireForUpdate<PhysicsConstrainedBodyPair>();
        state.RequireForUpdate<PhysicsJoint>();
        state.RequireForUpdate<RootPhenotypeEntity>();
        state.RequireForUpdate<JointBreakSystemSettings>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.GetSingleton<JointBreakSystemSettings>().Enabled)
            return;

        localTransformLookup.Update(ref state);

        using EntityCommandBuffer ecb = new(Allocator.TempJob);
        new JointBreakJob
        {
            Ecb = ecb.AsParallelWriter(),
            LocalTransformLookup = localTransformLookup,
            EventBufferEntity = eventBufferEntity
        }.ScheduleParallel(state.Dependency).Complete();
        ecb.Playback(state.EntityManager);
    }
}

[BurstCompile]
[WithAll(typeof(PhysicsConstrainedBodyPair), typeof(PhysicsJoint), typeof(RootPhenotypeEntity))]
public partial struct JointBreakJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
    public Entity EventBufferEntity;

    private const float MAX_DISTANCE = 2f; // TODO: Base this on min. connected face dimensions.

    private const int INSTANTIATION_KEY = 1;
    private const int DISPOSAL_KEY = 2;

    public void Execute(Entity jointEntity, in PhysicsConstrainedBodyPair pair, in PhysicsJoint joint)
    {
        LocalTransform transformA = LocalTransformLookup[pair.EntityA];
        LocalTransform transformB = LocalTransformLookup[pair.EntityB];
        
        float3 worldA = math.transform(float4x4.TRS(transformA.Position, transformA.Rotation, transformA.Scale), joint.BodyAFromJoint.Position);
        float3 worldB = math.transform(float4x4.TRS(transformB.Position, transformB.Rotation, transformB.Scale), joint.BodyBFromJoint.Position);

        if (math.distancesq(worldA, worldB) > (MAX_DISTANCE * MAX_DISTANCE))
        {
            Ecb.DestroyEntity(DISPOSAL_KEY, jointEntity);

            Ecb.AppendToBuffer(INSTANTIATION_KEY, EventBufferEntity, new JointBrokenEvent
            {
                DetachedLimbEntity = pair.EntityB
            });
        }
    }
}
