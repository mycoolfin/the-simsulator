using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.RootPhenotypes
{
    using Core.ECS.Math;
    using Components.Phenotype;

    [UpdateInGroup(typeof(RootPhenotypeSystemGroup))]
    public partial struct UpdatePhenotypeBoundingBoxesSystem : ISystem
    {
        private ComponentLookup<LocalTransform> localTransformLookup;
        private NativeParallelMultiHashMap<Entity, AABB> limbBounds;

        public void OnCreate(ref SystemState state)
        {
            localTransformLookup = state.GetComponentLookup<LocalTransform>(true);

            limbBounds = new(256, Allocator.Persistent);

            state.RequireForUpdate<PhenotypeEntitiesMetadata>();
            state.RequireForUpdate<RootPhenotypeEntity>();
            state.RequireForUpdate<PhenotypeBoundingBox>();
        }

        public void OnUpdate(ref SystemState state)
        {
            localTransformLookup.Update(ref state);

            // Check if we need to reallocate based on metadata counts.           
            PhenotypeEntitiesMetadata metadata = SystemAPI.GetSingleton<PhenotypeEntitiesMetadata>();
            int requiredCapacity = (metadata.TotalLimbCount * 2).NextPowerOfTwo(); // 2x capacity for good hash map performance.
            int newCapacity = math.max(requiredCapacity * 2, limbBounds.Capacity * 2);
            if (requiredCapacity > limbBounds.Capacity)
            {
                limbBounds.Dispose();
                limbBounds = new(newCapacity, Allocator.Persistent);
            }

            limbBounds.Clear();

            JobHandle collectLimbsJob = new CollectLimbBoundsJob
            {
                LimbBounds = limbBounds.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new UpdatePhenotypeBoundingBoxesJob
            {
                LimbBounds = limbBounds.AsReadOnly()
            }.ScheduleParallel(collectLimbsJob);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (limbBounds.IsCreated)
                limbBounds.Dispose();
        }
    }

    [BurstCompile]
    [WithNone(typeof(DetachedLimbTag))] // Don't count detached limbs.
    public partial struct CollectLimbBoundsJob : IJobEntity
    {
        [WriteOnly] public NativeParallelMultiHashMap<Entity, AABB>.ParallelWriter LimbBounds;

        public void Execute(in LocalTransform localTransform, in PostTransformMatrix postTransform, in RootPhenotypeEntity rootPhenotypeEntity)
        {
            float3 scale = postTransform.Value.Scale();
            float3 halfExtents = scale * 0.5f;
            float3 center = localTransform.Position;

            // Calculate the oriented bounding box corners and find the axis-aligned bounds.
            float3x3 rotationMatrix = new(math.normalize(localTransform.Rotation));
            float3 corner0 = math.mul(rotationMatrix, new float3(-halfExtents.x, -halfExtents.y, -halfExtents.z)) + center;
            float3 corner1 = math.mul(rotationMatrix, new float3(halfExtents.x, -halfExtents.y, -halfExtents.z)) + center;
            float3 corner2 = math.mul(rotationMatrix, new float3(-halfExtents.x, halfExtents.y, -halfExtents.z)) + center;
            float3 corner3 = math.mul(rotationMatrix, new float3(halfExtents.x, halfExtents.y, -halfExtents.z)) + center;
            float3 corner4 = math.mul(rotationMatrix, new float3(-halfExtents.x, -halfExtents.y, halfExtents.z)) + center;
            float3 corner5 = math.mul(rotationMatrix, new float3(halfExtents.x, -halfExtents.y, halfExtents.z)) + center;
            float3 corner6 = math.mul(rotationMatrix, new float3(-halfExtents.x, halfExtents.y, halfExtents.z)) + center;
            float3 corner7 = math.mul(rotationMatrix, new float3(halfExtents.x, halfExtents.y, halfExtents.z)) + center;

            // Find the axis-aligned bounding box that contains all corners
            float3 minBounds = corner0;
            float3 maxBounds = corner0;
            minBounds = math.min(minBounds, corner1);
            maxBounds = math.max(maxBounds, corner1);
            minBounds = math.min(minBounds, corner2);
            maxBounds = math.max(maxBounds, corner2);
            minBounds = math.min(minBounds, corner3);
            maxBounds = math.max(maxBounds, corner3);
            minBounds = math.min(minBounds, corner4);
            maxBounds = math.max(maxBounds, corner4);
            minBounds = math.min(minBounds, corner5);
            maxBounds = math.max(maxBounds, corner5);
            minBounds = math.min(minBounds, corner6);
            maxBounds = math.max(maxBounds, corner6);
            minBounds = math.min(minBounds, corner7);
            maxBounds = math.max(maxBounds, corner7);

            AABB aabb = new()
            {
                Center = (minBounds + maxBounds) * 0.5f,
                Extents = (maxBounds - minBounds) * 0.5f
            };

            LimbBounds.Add(rootPhenotypeEntity.Value, aabb);
        }
    }

    [BurstCompile]
    public partial struct UpdatePhenotypeBoundingBoxesJob : IJobEntity
    {
        [ReadOnly] public NativeParallelMultiHashMap<Entity, AABB>.ReadOnly LimbBounds;

        public void Execute(Entity rootPhenotypeEntity, ref PhenotypeBoundingBox phenotypeBoundingBox)
        {
            float3 minBounds = float3.zero;
            float3 maxBounds = float3.zero;
            bool hasLimbs = false;

            if (LimbBounds.TryGetFirstValue(rootPhenotypeEntity, out AABB firstAABB, out var iterator))
            {
                // Initialize with the first limb's bounds
                minBounds = firstAABB.Center - firstAABB.Extents;
                maxBounds = firstAABB.Center + firstAABB.Extents;
                hasLimbs = true;

                // Combine with all other limbs
                while (LimbBounds.TryGetNextValue(out AABB nextAABB, ref iterator))
                {
                    float3 limbMin = nextAABB.Center - nextAABB.Extents;
                    float3 limbMax = nextAABB.Center + nextAABB.Extents;

                    minBounds = math.min(minBounds, limbMin);
                    maxBounds = math.max(maxBounds, limbMax);
                }
            }

            // Only update if we found limbs
            if (hasLimbs)
            {
                phenotypeBoundingBox.MinBounds = minBounds;
                phenotypeBoundingBox.MaxBounds = maxBounds;
            }
            else
            {
                // Default to zero bounds if no limbs
                phenotypeBoundingBox.MinBounds = float3.zero;
                phenotypeBoundingBox.MaxBounds = float3.zero;
            }
        }
    }
}
