using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Limbs
{
    using Components.Phenotype;
    using RootPhenotypes;

    public struct RepositionLimbsRequest : IComponentData
    {
        public float GroundY;
    }

    [UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
    public partial struct RepositionLimbsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhenotypeEntitiesMetadata>();
            state.RequireForUpdate<RepositionLimbsRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity requestEntity = SystemAPI.GetSingletonEntity<RepositionLimbsRequest>();

            // Run the RootPhenotypeSystemGroup manually to ensure bounding boxes are ready.
            state.World.GetExistingSystemManaged<RootPhenotypeSystemGroup>().Update();

            float groundY = SystemAPI.GetComponent<RepositionLimbsRequest>(requestEntity).GroundY;
            int phenotypeCount = SystemAPI.GetSingleton<PhenotypeEntitiesMetadata>().TotalRootPhenotypeCount;
            using NativeParallelHashMap<Entity, float> limbYTranslations = new(phenotypeCount * 2, Allocator.TempJob);

            using EntityCommandBuffer ecb = new(Allocator.TempJob);

            JobHandle calculateLimbYTranslationsJob = new CalculateLimbYTranslationsJob
            {
                GroundY = groundY,
                limbYTranslations = limbYTranslations.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);

            new TranslateLimbsJob
            {
                LimbYTranslations = limbYTranslations.AsReadOnly()
            }.ScheduleParallel(calculateLimbYTranslationsJob).Complete();

            ecb.Playback(state.EntityManager);
            state.EntityManager.DestroyEntity(requestEntity);
        }
    }

    [BurstCompile]
    public partial struct CalculateLimbYTranslationsJob : IJobEntity
    {
        public float GroundY;
        public NativeParallelHashMap<Entity, float>.ParallelWriter limbYTranslations;

        public void Execute(Entity rootPhenotypeEntity, PhenotypeBoundingBox boundingBox)
        {
            // Calculate how much we need to move this phenotype's limbs by to ensure they are aligned with the ground.
            float smallOffset = 0.1f; // Prevent clipping.
            float bbMinBoundsY = boundingBox.Center.y - boundingBox.CurrentExtents.y;
            float limbYTranslation = GroundY - bbMinBoundsY + smallOffset;
            limbYTranslations.TryAdd(rootPhenotypeEntity, limbYTranslation);
        }
    }

    [BurstCompile]
    public partial struct TranslateLimbsJob : IJobEntity
    {
        [ReadOnly] public NativeParallelHashMap<Entity, float>.ReadOnly LimbYTranslations;

        public void Execute(RootPhenotypeEntity rootPhenotypeEntity, ref LocalTransform localTransform)
        {
            if (LimbYTranslations.TryGetValue(rootPhenotypeEntity.Value, out float translation))
                localTransform.Position.y += translation;
        }
    }
}
