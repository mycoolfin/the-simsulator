using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

public struct RepositionPhenotypesRequest : IComponentData
{
    public float GroundY;
}

public partial struct RepositionPhenotypesSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhenotypeEntitiesMetadata>();
        state.RequireForUpdate<RepositionPhenotypesRequest>();
    }

    public void OnUpdate(ref SystemState state)
    {
        Entity requestEntity = SystemAPI.GetSingletonEntity<RepositionPhenotypesRequest>();

        // Run the RootPhenotypeSystemGroup manually to ensure bounding boxes are ready.
        state.World.GetExistingSystemManaged<RootPhenotypeSystemGroup>().Update();

        float groundY = SystemAPI.GetComponent<RepositionPhenotypesRequest>(requestEntity).GroundY;
        int phenotypeCount = SystemAPI.GetSingleton<PhenotypeEntitiesMetadata>().TotalRootPhenotypeCount;
        using NativeParallelHashMap<Entity, float> limbYTranslations = new(phenotypeCount * 2, Allocator.TempJob);

        using EntityCommandBuffer ecb = new(Allocator.TempJob);

        JobHandle phenotypesJob = new CalculateLimbYTranslationsJob
        {
            GroundY = groundY,
            limbYTranslations = limbYTranslations.AsParallelWriter()
        }.ScheduleParallel(state.Dependency);

        new TranslateLimbsJob
        {
            LimbYTranslations = limbYTranslations.AsReadOnly()
        }.ScheduleParallel(phenotypesJob).Complete();

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
        float limbYTranslation = GroundY - boundingBox.MinBounds.y + smallOffset;
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
