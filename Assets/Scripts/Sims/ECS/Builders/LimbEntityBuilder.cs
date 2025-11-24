using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.Rendering;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Builders
{
    using TheSimsulator.Sims.Phenotype;
    using Core.ECS.Caching;
    using Core.ECS.Components.Shared;
    using Components.Phenotype;
    using Systems.Initialisation;
    using NeuralNetwork;
    using Rendering;

    [BurstCompile]
    public static class LimbEntityBuilder
    {
        public static SimsRenderMeshArrayCreator RenderMeshArrayCreator;

        public const uint PHENOTYPE_LAYER = 1u << 9;
        public const uint ALL_LAYERS = ~0u;

        private static readonly PhysicsDamping DefaultDamping = new() { Linear = 0.01f, Angular = 0.05f };

        public static bool IsReady()
        {
            RenderMeshArray renderMeshArray = RenderMeshArrayCreator.RenderMeshArray;
            return renderMeshArray.MaterialReferences != null && renderMeshArray.MaterialReferences.Length != 0
                && renderMeshArray.MeshReferences != null && renderMeshArray.MeshReferences.Length != 0;
        }

        public static void CreateLimbEntities(
            ref SystemState state,
            EntityQuery limbCreationRequestQuery,
            NativeArray<LimbEntityCreationRequest> limbCreationRequests,
            NativeParallelHashMap<ulong, Entity>.ReadOnly rootPhenotypeEntityLookup,
            NativeParallelHashMap<ulong, NeuralGraphRef>.ReadOnly neuralGraphLookup,
            NativeParallelHashMap<PhenotypeLimbKey, Entity> limbEntityLookup,
            NativeParallelHashMap<PhenotypeLimbKey, LocalTransform> limbLocalTransformLookup
        )
        {
            using NativeArray<Entity> limbEntities = new(limbCreationRequests.Length, Allocator.TempJob);
            Entity limbPrototype = CreateLimbPrototype(ref state, RenderMeshArrayCreator.RenderMeshArray);
            state.EntityManager.Instantiate(limbPrototype, limbEntities);
            state.EntityManager.DestroyEntity(limbPrototype);

            using NativeArray<Entity> limbCreationRequestEntities = limbCreationRequestQuery.ToEntityArray(Allocator.TempJob);
            using EntityCommandBuffer ecb = new(Allocator.TempJob);
            SetUpLimbEntityJob setUpLimbEntityJob = new()
            {
                Ecb = ecb.AsParallelWriter(),
                RequestEntities = limbCreationRequestEntities,
                Requests = limbCreationRequests,
                LimbEntities = limbEntities,
                ColliderMap = ColliderCacheManager.Cache.ReadOnlyMap,
                RootPhenotypeEntityLookup = rootPhenotypeEntityLookup,
                NeuralGraphLookup = neuralGraphLookup,
                LimbEntityLookup = limbEntityLookup.AsParallelWriter(),
                LimbLocalTransformLookup = limbLocalTransformLookup.AsParallelWriter()
            };
            setUpLimbEntityJob.ScheduleParallelByRef(limbCreationRequestEntities.Length, 64, state.Dependency).Complete();
            ecb.Playback(state.EntityManager);
        }

        private static Entity CreateLimbPrototype(ref SystemState state, RenderMeshArray renderMeshArray)
        {
            Entity limbPrototype = state.EntityManager.CreateEntity();

            // Refs.
            state.EntityManager.AddComponentData(limbPrototype, new RootPhenotypeEntity());
            state.EntityManager.AddComponentData(limbPrototype, new LimbIndex());
            state.EntityManager.AddComponentData(limbPrototype, new ParentLimb() { Value = Entity.Null });

            // Limb sensors.
            state.EntityManager.AddComponentData(limbPrototype, new ContactSensors());
            state.EntityManager.AddComponentData(limbPrototype, new LightSensors());

            // Transform.
            state.EntityManager.AddComponentData(limbPrototype, new LocalTransform());
            state.EntityManager.AddComponentData(limbPrototype, new PostTransformMatrix());

            // Rendering.
            state.EntityManager.AddComponentData(limbPrototype, new LimbColor());
            state.EntityManager.AddComponentData(limbPrototype, new URPMaterialPropertyBaseColor());
            RenderMeshUtility.AddComponents(
                limbPrototype,
                state.EntityManager,
                new(
                    shadowCastingMode: UnityEngine.Rendering.ShadowCastingMode.Off,
                    receiveShadows: false
                ),
                renderMeshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(RenderMeshArrayCreator.LimbIndex, RenderMeshArrayCreator.LimbIndex)
            );

            // Physics.
            state.EntityManager.AddSharedComponent(limbPrototype, new PhysicsWorldIndex(0));
            state.EntityManager.AddComponentData(limbPrototype, new PhysicsCollider());
            state.EntityManager.AddComponentData(limbPrototype, new PhysicsMass());
            state.EntityManager.AddComponentData(limbPrototype, new PhysicsVelocity());
            state.EntityManager.AddComponentData(limbPrototype, DefaultDamping);

            return limbPrototype;
        }

        [BurstCompile]
        public static void GetCollisionFilter(in LimbEntityCreationRequest request, out CollisionFilter filter)
        {
            filter = new CollisionFilter
            {
                BelongsTo = PHENOTYPE_LAYER,
                CollidesWith = request.AllowInterPhenotypeCollisions == 1
                    ? ALL_LAYERS
                    : ~PHENOTYPE_LAYER,
                GroupIndex = GetNegativeGroupIndex(request.PhenotypeGid)
            };
        }

        private static int GetNegativeGroupIndex(ulong gid)
        {
            // Split into two 32-bit uints.
            uint low = (uint)(gid & 0xFFFFFFFF);
            uint high = (uint)(gid >> 32);

            // Hash the uint2 representation.
            uint hash = math.hash(new uint2(low, high));

            // Force high bit = 1 to make it negative when cast to int.
            return (int)(hash | 0x80000000);
        }

        [BurstCompile]
        private partial struct SetUpLimbEntityJob : IJobFor
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public NativeArray<Entity> RequestEntities;
            [ReadOnly] public NativeArray<LimbEntityCreationRequest> Requests;
            [ReadOnly] public NativeArray<Entity> LimbEntities;
            [ReadOnly] public NativeParallelHashMap<ColliderKey, BlobAssetReference<Collider>>.ReadOnly ColliderMap;
            [ReadOnly] public NativeParallelHashMap<ulong, Entity>.ReadOnly RootPhenotypeEntityLookup;
            [ReadOnly] public NativeParallelHashMap<ulong, NeuralGraphRef>.ReadOnly NeuralGraphLookup;
            public NativeParallelHashMap<PhenotypeLimbKey, Entity>.ParallelWriter LimbEntityLookup;
            public NativeParallelHashMap<PhenotypeLimbKey, LocalTransform>.ParallelWriter LimbLocalTransformLookup;

            public void Execute(int index)
            {
                LimbEntityCreationRequest requestData = Requests[index];
                Entity limbEntity = LimbEntities[index];

                // Root phenotype entity reference.
                Ecb.SetComponent(index, limbEntity, new RootPhenotypeEntity
                {
                    Value = RootPhenotypeEntityLookup[requestData.PhenotypeGid]
                });

                // Limb index.
                Ecb.SetComponent(index, limbEntity, new LimbIndex
                {
                    Value = requestData.LimbIndex
                });

                // Sensor neural emitter indices.
                ref CompiledNeuralGraph neuralGraph = ref NeuralGraphLookup[requestData.PhenotypeGid].Value.Value;
                Ecb.SetComponent(index, limbEntity, new ContactSensors
                {
                    TotalLoadSensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(requestData.LimbIndex, SensorType.ContactTotalLoad, 0),
                    SlipSensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(requestData.LimbIndex, SensorType.ContactSlip, 0),
                    XAxisLoadSensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(requestData.LimbIndex, SensorType.ContactDirectionalLoad, 0),
                    YAxisLoadSensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(requestData.LimbIndex, SensorType.ContactDirectionalLoad, 1),
                    ZAxisLoadSensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(requestData.LimbIndex, SensorType.ContactDirectionalLoad, 2)
                });
                Ecb.SetComponent(index, limbEntity, new LightSensors
                {
                    XAxisSensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(requestData.LimbIndex, SensorType.Light, 0),
                    YAxisSensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(requestData.LimbIndex, SensorType.Light, 1),
                    ZAxisSensorEmitterIndex = neuralGraph.GetEmitterIndexOfSensor(requestData.LimbIndex, SensorType.Light, 2)
                });

                // Transform.
                quaternion rotation = requestData.Rotation;
                if (rotation.value.w < 0f) // Canonicalise rotation: ensure W >= 0 for consistent physics behavior.
                    rotation.value = -rotation.value;
                
                LocalTransform localTransform = LocalTransform.FromPositionRotationScale(
                    requestData.Position + requestData.PhysicsPositionOffset,
                    rotation,
                    1f
                );
                Ecb.SetComponent(index, limbEntity, localTransform);
                Ecb.SetComponent(index, limbEntity, new PostTransformMatrix
                {
                    Value = float4x4.Scale(requestData.Dimensions)
                });

                // Rendering.
                Ecb.SetComponent(index, limbEntity, new LimbColor
                {
                    Value = requestData.Color
                });
                Ecb.SetComponent(index, limbEntity, new URPMaterialPropertyBaseColor
                {
                    Value = requestData.Color
                });
                Ecb.SetComponent(index, limbEntity, new RenderBounds
                {
                    Value = new AABB
                    {
                        Center = float3.zero,
                        Extents = requestData.Dimensions * 0.5f
                    }
                });

                // Physics.
                GetCollisionFilter(requestData, out CollisionFilter collisionFilter);
                ColliderMap.TryGetValue(new(requestData.Dimensions, collisionFilter), out BlobAssetReference<Collider> collider);
                Ecb.SetComponent(index, limbEntity, new PhysicsCollider { Value = collider });
                Ecb.SetComponent(index, limbEntity, PhysicsMass.CreateDynamic(collider.Value.MassProperties, requestData.Mass));

                // Add to lookups.
                PhenotypeLimbKey key = new(requestData.PhenotypeGid, requestData.LimbIndex);
                LimbEntityLookup.TryAdd(key, limbEntity);
                LimbLocalTransformLookup.TryAdd(key, localTransform);

                // Destroy the request entity.
                int disposalOffsetIndex = RequestEntities.Length;
                Ecb.DestroyEntity(index + disposalOffsetIndex, RequestEntities[index]);
            }
        }
    }
}
