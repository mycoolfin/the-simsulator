using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.Systems.Initialisation
{
    using Sims.Genotype;
    using Components.Phenotype;
    using Caching;
    using Builders;
    using NeuralNetwork;

    public struct RootPhenotypeEntityCreationRequest : IComponentData
    {
        public ulong PhenotypeGid;
        public BlobAssetReference<CompiledNeuralGraph> Graph;
        public byte LimbCount;
    }

    public struct LimbEntityCreationRequest : IComponentData
    {
        public ulong PhenotypeGid;
        public byte LimbIndex;
        public float3 Position;
        public quaternion Rotation;
        public float3 Dimensions;
        public float Mass;
        public float4 Color;
        public float3 PhysicsPositionOffset;
        public byte AllowInterPhenotypeCollisions;
    }

    public struct JointEntityCreationRequest : IComponentData
    {
        public ulong PhenotypeGid;
        public JointType JointType;
        public int ReferenceLimbIndex;
        public int AttachedLimbIndex;
        public float3 ReferenceLimbSpaceAnchor;
        public float3 ReferenceLimbSpaceXAxis;
        public float3 ReferenceLimbSpaceYAxis;
        public float3 ReferenceLimbSpaceZAxis;
        public byte FlippedHandedness;
        public float3 AngleLimits;
        public float MinCrossSectionalArea;
    }

    [BurstCompile]
    public struct PhenotypeLimbKey : IEquatable<PhenotypeLimbKey>
    {
        public ulong PhenotypeGid;
        public int LimbIndex;

        public PhenotypeLimbKey(ulong phenotypeGid, int limbIndex)
        {
            PhenotypeGid = phenotypeGid;
            LimbIndex = limbIndex;
        }

        public readonly bool Equals(PhenotypeLimbKey other)
        {
            return PhenotypeGid == other.PhenotypeGid && LimbIndex == other.LimbIndex;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is PhenotypeLimbKey other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            return (int)math.hash(new uint2(
                (uint)(PhenotypeGid ^ (PhenotypeGid >> 32)),
                (uint)LimbIndex
            ));
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PhenotypeEntityCreationSystem : ISystem
    {
        private EntityArchetype jointArchetype;
        private EntityArchetype neuralNetworkArchetype;

        public void OnCreate(ref SystemState state)
        {
            ColliderCacheManager.Acquire();
            jointArchetype = JointEntityBuilder.CreateJointArchetype(ref state);
            neuralNetworkArchetype = RootPhenotypeEntityBuilder.CreateRootPhenotypeArchetype(ref state);
            state.RequireForUpdate<RootPhenotypeEntityCreationRequest>();
            state.RequireForUpdate<LimbEntityCreationRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!LimbEntityBuilder.IsReady())
                return;

            EntityQuery rootPhenotypeCreationRequestQuery = SystemAPI.QueryBuilder().WithAll<RootPhenotypeEntityCreationRequest>().Build();
            using NativeArray<RootPhenotypeEntityCreationRequest> rootPhenotypeCreationRequests = rootPhenotypeCreationRequestQuery.ToComponentDataArray<RootPhenotypeEntityCreationRequest>(Allocator.TempJob);
            using NativeParallelHashMap<ulong, NeuralGraphRef> neuralGraphLookup = new(rootPhenotypeCreationRequestQuery.CalculateEntityCount() * 2, Allocator.TempJob);
            using NativeParallelHashMap<ulong, Entity> rootPhenotypeEntityLookup = new(rootPhenotypeCreationRequestQuery.CalculateEntityCount() * 2, Allocator.TempJob);
            FixedStepSimulationSystemGroup fixedStepGroup = state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            RootPhenotypeEntityBuilder.CreateRootPhenotypeEntities(
                ref state,
                neuralNetworkArchetype,
                rootPhenotypeCreationRequestQuery,
                rootPhenotypeCreationRequests,
                rootPhenotypeEntityLookup,
                neuralGraphLookup,
                (float)fixedStepGroup.World.Time.ElapsedTime
            );

            EntityQuery limbCreationRequestQuery = SystemAPI.QueryBuilder().WithAll<LimbEntityCreationRequest>().Build();
            using NativeArray<LimbEntityCreationRequest> limbCreationRequests = limbCreationRequestQuery.ToComponentDataArray<LimbEntityCreationRequest>(Allocator.TempJob);
            AddCollidersToCache(limbCreationRequests);
            using NativeParallelHashMap<PhenotypeLimbKey, Entity> limbEntityLookup = new(limbCreationRequestQuery.CalculateEntityCount() * 2, Allocator.TempJob);
            using NativeParallelHashMap<PhenotypeLimbKey, LocalTransform> limbLocalTransformLookup = new(limbCreationRequestQuery.CalculateEntityCount() * 2, Allocator.TempJob);
            LimbEntityBuilder.CreateLimbEntities(
                ref state,
                limbCreationRequestQuery,
                limbCreationRequests,
                rootPhenotypeEntityLookup.AsReadOnly(),
                limbEntityLookup,
                limbLocalTransformLookup
            );

            EntityQuery jointCreationRequestQuery = SystemAPI.QueryBuilder().WithAll<JointEntityCreationRequest>().Build();
            using EntityCommandBuffer ecb = new(Allocator.TempJob);
            new CreateJointEntityJob
            {
                Ecb = ecb.AsParallelWriter(),
                JointArchetype = jointArchetype,
                LimbEntityLookup = limbEntityLookup.AsReadOnly(),
                LimbLocalTransformLookup = limbLocalTransformLookup.AsReadOnly(),
                RootPhenotypeEntityLookup = rootPhenotypeEntityLookup.AsReadOnly(),
                NeuralGraphLookup = neuralGraphLookup.AsReadOnly()
            }.ScheduleParallel(jointCreationRequestQuery, state.Dependency).Complete();
            ecb.Playback(state.EntityManager);

            // Update metadata with new counts.
            PhenotypeEntitiesMetadata metadata = new()
            {
                TotalRootPhenotypeCount = rootPhenotypeCreationRequests.Length,
                TotalLimbCount = limbCreationRequests.Length
            };
            Entity metadataSingleton = SystemAPI.HasSingleton<PhenotypeEntitiesMetadata>()
                ? SystemAPI.GetSingletonEntity<PhenotypeEntitiesMetadata>()
                : state.EntityManager.CreateSingleton<PhenotypeEntitiesMetadata>();
            state.EntityManager.SetComponentData(metadataSingleton, metadata);
        }

        public readonly void OnDestroy(ref SystemState state)
        {
            ColliderCacheManager.Release();
        }

        private readonly void AddCollidersToCache(NativeArray<LimbEntityCreationRequest> requests)
        {
            using NativeHashSet<ColliderKey> uniqueKeys = new(requests.Length, Allocator.TempJob);
            for (int i = 0; i < requests.Length; i++)
            {
                LimbEntityBuilder.GetCollisionFilter(requests[i], out CollisionFilter collisionFilter);
                uniqueKeys.Add(new(requests[i].Dimensions, collisionFilter));
            }
            ColliderCacheManager.Cache.AddColliders(uniqueKeys);
        }
    }
}
