using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(RootPhenotypeSystemGroup))]
public partial struct UpdatePhenotypeCentroidsSystem : ISystem
{
    private ComponentLookup<LocalTransform> localTransformLookup;
    private NativeParallelMultiHashMap<Entity, float3> limbPositions;

    public void OnCreate(ref SystemState state)
    {
        localTransformLookup = state.GetComponentLookup<LocalTransform>(true);

        limbPositions = new(256, Allocator.Persistent);

        state.RequireForUpdate<PhenotypeEntitiesMetadata>();
        state.RequireForUpdate<RootPhenotypeEntity>();
        state.RequireForUpdate<PhenotypeCentroid>();
    }

    public void OnUpdate(ref SystemState state)
    {
        localTransformLookup.Update(ref state);

        // Check if we need to reallocate based on metadata counts.           
        PhenotypeEntitiesMetadata metadata = SystemAPI.GetSingleton<PhenotypeEntitiesMetadata>();
        int requiredLimbCapacity = NextPowerOfTwo(metadata.TotalLimbCount);
        if (requiredLimbCapacity > limbPositions.Capacity)
        {
            limbPositions.Dispose();
            limbPositions = new(requiredLimbCapacity, Allocator.Persistent);
        }

        // Clear previous data.
        limbPositions.Clear();

        // Collect limb positions in parallel.
        new CollectLimbPositionsJob
        {
            LimbPositions = limbPositions.AsParallelWriter()
        }.ScheduleParallel(state.Dependency).Complete();

        // Update centroids in entities.
        new UpdateCentroidsJob
        {
            LimbPositions = limbPositions.AsReadOnly()
        }.ScheduleParallel(state.Dependency).Complete();
    }

    public void OnDestroy(ref SystemState state)
    {
        if (limbPositions.IsCreated)
            limbPositions.Dispose();
    }

    private static int NextPowerOfTwo(int value)
    {
        if (value <= 0) return 1;
        if (value == 1) return 2;
        
        // Find the next power of 2
        int power = 1;
        while (power < value)
            power <<= 1;
        
        return power;
    }
}

[BurstCompile]
[WithNone(typeof(DetachedLimbTag))] // Don't count detached limbs.
public partial struct CollectLimbPositionsJob : IJobEntity
{
    [WriteOnly] public NativeParallelMultiHashMap<Entity, float3>.ParallelWriter LimbPositions;

    public void Execute(in LocalTransform localTransform, in RootPhenotypeEntity rootPhenotypeEntity)
    {
        LimbPositions.Add(rootPhenotypeEntity.Value, localTransform.Position);
    }
}

[BurstCompile]
public partial struct UpdateCentroidsJob : IJobEntity
{
    [ReadOnly] public NativeParallelMultiHashMap<Entity, float3>.ReadOnly LimbPositions;

    public void Execute(Entity rootPhenotypeEntity, ref PhenotypeCentroid phenotypeCentroid)
    {
        if (LimbPositions.TryGetFirstValue(rootPhenotypeEntity, out float3 firstPos, out var iterator))
        {
            float3 minBounds = firstPos;
            float3 maxBounds = firstPos;

            while (LimbPositions.TryGetNextValue(out float3 nextPos, ref iterator))
            {
                minBounds = math.min(minBounds, nextPos);
                maxBounds = math.max(maxBounds, nextPos);
            }

            float3 centroid = (minBounds + maxBounds) * 0.5f;

            phenotypeCentroid.Value = centroid;
        }
        else
        {
            // If no limbs are found, set centroid to zero.
            phenotypeCentroid.Value = float3.zero;
        }
    }
}
