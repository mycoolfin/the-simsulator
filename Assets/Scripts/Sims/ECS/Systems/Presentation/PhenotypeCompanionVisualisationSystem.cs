using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.Systems.Presentation
{
    using Core.ECS.Math;
    using Core.ECS.Rendering;
    using Core.ECS.Components.Shared;
    using Core.ECS.Components.Phenotype;
    using Sims.ECS.Components.Phenotype;

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(ApplyWorldVisualOffsetSystem))]
    public partial struct PhenotypeCompanionVisualisationSystem : ISystem
    {
        private ComponentLookup<PhenotypeBoundingBox> phenotypeBoundingBoxLookup;
        private NativeParallelHashMap<Entity, float4x4> phenotypeCompanionTransformsMap;

        public void OnCreate(ref SystemState state)
        {
            phenotypeBoundingBoxLookup = state.GetComponentLookup<PhenotypeBoundingBox>(isReadOnly: true);
            phenotypeCompanionTransformsMap = new NativeParallelHashMap<Entity, float4x4>(128, Allocator.Persistent);

            state.RequireForUpdate<PhenotypeEntitiesMetadata>();
            state.RequireForUpdate<LocalTransform>();
            state.RequireForUpdate<PostTransformMatrix>();
            state.RequireForUpdate<LocalToWorld>();
            state.RequireForUpdate<LimbIndex>();
            state.RequireForUpdate<PhenotypeCompanionObject>();
        }

        public void OnUpdate(ref SystemState state)
        {
            phenotypeBoundingBoxLookup.Update(ref state);

            UpdateTransformsMap(ref state);

            new UpdatePhenotypeSubEntitiesLocalToWorldsJob
            {
                PhenotypeBoundingBoxLookup = phenotypeBoundingBoxLookup,
                PhenotypeCompanionTransformsMap = phenotypeCompanionTransformsMap.AsReadOnly()
            }.ScheduleParallel(state.Dependency).Complete();
        }

        public void OnDestroy()
        {
            if (phenotypeCompanionTransformsMap.IsCreated)
                phenotypeCompanionTransformsMap.Dispose();
        }

        private void UpdateTransformsMap(ref SystemState state)
        {
            // Resize if necessary.
            PhenotypeEntitiesMetadata metadata = SystemAPI.GetSingleton<PhenotypeEntitiesMetadata>();
            int requiredCapacity = metadata.TotalRootPhenotypeCount.NextPowerOfTwo();
            // Expand capacity to accommodate new keys with some headroom.
            int newCapacity = math.max(requiredCapacity * 2, phenotypeCompanionTransformsMap.Capacity * 2);
            if (requiredCapacity > phenotypeCompanionTransformsMap.Capacity)
            {
                phenotypeCompanionTransformsMap.Dispose();
                phenotypeCompanionTransformsMap = new(newCapacity, Allocator.Persistent);
            }

            // Update phenotype entity / companion object transform pairs.
            // Managed query — main thread only.
            phenotypeCompanionTransformsMap.Clear();
            foreach (var (companionObjectTag, rootPhenotypeEntity) in SystemAPI.Query<
                        SystemAPI.ManagedAPI.UnityEngineComponent<PhenotypeCompanionObject>
                    >().WithEntityAccess())
            {
                PhenotypeCompanionObject companionObject = companionObjectTag.Value;
                Matrix4x4 companionTransform = companionObject != null ? companionObject.transform.localToWorldMatrix : Matrix4x4.zero;
                phenotypeCompanionTransformsMap.TryAdd(rootPhenotypeEntity, companionTransform);
            }
        }
    }

    [BurstCompile]
    public partial struct UpdatePhenotypeSubEntitiesLocalToWorldsJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<PhenotypeBoundingBox> PhenotypeBoundingBoxLookup;
        [ReadOnly] public NativeParallelHashMap<Entity, float4x4>.ReadOnly PhenotypeCompanionTransformsMap;

        public void Execute(in RootPhenotypeEntity rootPhenotypeEntity, in LocalTransform localTransform, in PostTransformMatrix postTransform, ref LocalToWorld localToWorld)
        {
            if (PhenotypeCompanionTransformsMap.TryGetValue(rootPhenotypeEntity.Value, out float4x4 companionTransform))
            {
                if (PhenotypeBoundingBoxLookup.TryGetComponent(rootPhenotypeEntity.Value, out PhenotypeBoundingBox boundingBox))
                {
                    float4x4 realTransform = math.mul(
                        float4x4.TRS(localTransform.Position, localTransform.Rotation, new float3(localTransform.Scale)),
                        postTransform.Value
                    );

                    float3 maxSize = boundingBox.MaxExtents * 2f;
                    float maxDimension = math.max(math.max(maxSize.x, maxSize.y), maxSize.z);
                    maxDimension = maxDimension > 0f ? maxDimension : math.INFINITY; // Prevent division by zero.
                    float scalingFactor = maxDimension > math.EPSILON ? 1f / maxDimension : 1f;

                    float4x4 scaleThenCenter = math.mul(
                        float4x4.Scale(scalingFactor),
                        float4x4.Translate(-boundingBox.Center)
                    );

                    float4x4 phenotypeSpace = math.mul(scaleThenCenter, realTransform);

                    localToWorld.Value = math.mul(companionTransform, phenotypeSpace);
                }
                else
                {
                    localToWorld.Value = float4x4.zero;
                }
            }
        }
    }
}
