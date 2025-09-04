using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Limbs
{
    using Core.ECS.Components.Shared;
    using Core.ECS.Systems.Simulation.Phenotypes;
    using Components.Phenotype;

    [UpdateInGroup(typeof(PhenotypeSystemGroup))]
    [UpdateAfter(typeof(RepositionPhenotypeEntitiesSystem))]
    public partial struct RepositionPhenotypeEntitiesSystem : ISystem
    {
        private ComponentLookup<RepositionPhenotypeEntitiesRequest> repositionRequestLookup;

        public void OnCreate(ref SystemState state)
        {
            repositionRequestLookup = state.GetComponentLookup<RepositionPhenotypeEntitiesRequest>(isReadOnly: true);

            state.RequireForUpdate<PhenotypeEntitiesMetadata>();
            state.RequireForUpdate<RepositionPhenotypeEntitiesRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            repositionRequestLookup.Update(ref state);

            JobHandle repositionLimbsJob = new RepositionLimbsJob
            {
                RepositionRequestLookup = repositionRequestLookup
            }.ScheduleParallel(state.Dependency);

            using EntityCommandBuffer ecb = new(Allocator.TempJob);

            new RemoveRequestComponentsJob
            {
                Ecb = ecb.AsParallelWriter()
            }.ScheduleParallel(repositionLimbsJob).Complete();

            ecb.Playback(state.EntityManager);
        }
    }

    [BurstCompile]
    public partial struct RepositionLimbsJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<RepositionPhenotypeEntitiesRequest> RepositionRequestLookup;

        public void Execute(RootPhenotypeEntity rootPhenotypeEntity, ref LocalTransform localTransform, ref PhysicsVelocity velocity)
        {
            if (!RepositionRequestLookup.HasComponent(rootPhenotypeEntity.Value))
                return;

            RepositionPhenotypeEntitiesRequest request = RepositionRequestLookup[rootPhenotypeEntity.Value];
            localTransform.Position += request.Translation;

            if (request.ZeroVelocities)
            {
                velocity.Linear = float3.zero;
                velocity.Angular = float3.zero;
            }
        }
    }

    [BurstCompile]
    public partial struct RemoveRequestComponentsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute([ChunkIndexInQuery] int index, RootPhenotypeEntity rootPhenotypeEntity)
        {
            Ecb.RemoveComponent<RepositionPhenotypeEntitiesRequest>(index, rootPhenotypeEntity.Value);
        }
    }
}
