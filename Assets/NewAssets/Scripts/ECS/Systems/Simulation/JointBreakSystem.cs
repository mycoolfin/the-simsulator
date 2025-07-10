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

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct JointBreakSystem : ISystem
{
    private ComponentLookup<LocalToWorld> localToWorldLookup;
    private ComponentLookup<LimbIndex> limbIndexLookup;

    public void OnCreate(ref SystemState state)
    {
        state.EntityManager.CreateSingleton(new JointBreakSystemSettings { Enabled = true });

        localToWorldLookup = state.GetComponentLookup<LocalToWorld>(isReadOnly: true);
        limbIndexLookup = state.GetComponentLookup<LimbIndex>(isReadOnly: true);

        state.RequireForUpdate<PhysicsConstrainedBodyPair>();
        state.RequireForUpdate<PhysicsJoint>();
        state.RequireForUpdate<RootPhenotypeEntity>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.GetSingleton<JointBreakSystemSettings>().Enabled)
            return;

        localToWorldLookup.Update(ref state);
        limbIndexLookup.Update(ref state);

        using EntityCommandBuffer ecb = new(Allocator.TempJob);
        new JointBreakJob
        {
            Ecb = ecb.AsParallelWriter(),
            LocalToWorldLookup = localToWorldLookup,
            LimbIndexLookup = limbIndexLookup
        }.ScheduleParallel(state.Dependency).Complete();
        ecb.Playback(state.EntityManager);
    }
}

[BurstCompile]
public partial struct JointBreakJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
    [ReadOnly] public ComponentLookup<LimbIndex> LimbIndexLookup;

    private const float MAX_DISTANCE = 0.25f;

    private const int INSTANTIATION_KEY = 1;
    private const int DISPOSAL_KEY = 2;

    public void Execute(Entity jointEntity, in PhysicsConstrainedBodyPair pair, in PhysicsJoint joint, in RootPhenotypeEntity rootPhenotypeEntity)
    {
        float3 worldA = math.transform(LocalToWorldLookup[pair.EntityA].Value, joint.BodyAFromJoint.Position);
        float3 worldB = math.transform(LocalToWorldLookup[pair.EntityB].Value, joint.BodyBFromJoint.Position);

        if (math.distancesq(worldA, worldB) > (MAX_DISTANCE * MAX_DISTANCE))
        {
            Ecb.DestroyEntity(DISPOSAL_KEY, jointEntity);

            Ecb.AppendToBuffer(INSTANTIATION_KEY, rootPhenotypeEntity.Value, new JointBrokenEvent
            {
                DetachedLimbEntity = pair.EntityB,
                LimbIndex = LimbIndexLookup[pair.EntityB].Value
            });
        }
    }
}
