using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Physics;

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

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
public partial struct PhenotypeEntityCreationSystem : ISystem
{
    public static RenderMeshArray RenderMeshArray; // TODO: This won't work across worlds. Use a singleton baker instead.

    private EntityArchetype jointArchetype;
    private EntityArchetype neuralNetworkArchetype;

    public void OnCreate(ref SystemState state)
    {
        ColliderCacheManager.Acquire();
        jointArchetype = JointEntityBuilder.CreateJointArchetype(ref state);
        neuralNetworkArchetype = NeuralNetworkEntityBuilder.CreateNeuralNetworkArchetype(ref state);
        state.RequireForUpdate<NeuralNetworkEntityCreationRequest>();
        state.RequireForUpdate<LimbEntityCreationRequest>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (RenderMeshArray.MaterialReferences == null || RenderMeshArray.MaterialReferences.Length == 0 ||
            RenderMeshArray.MeshReferences == null || RenderMeshArray.MeshReferences.Length == 0)
            return;

        EntityQuery neuralNetworkCreationRequestQuery = SystemAPI.QueryBuilder().WithAll<NeuralNetworkEntityCreationRequest>().Build();
        using NativeArray<NeuralNetworkEntityCreationRequest> neuralNetworkCreationRequests = neuralNetworkCreationRequestQuery.ToComponentDataArray<NeuralNetworkEntityCreationRequest>(Allocator.TempJob);
        using NativeParallelHashMap<ulong, NeuralGraphRef> neuralGraphLookup = new(neuralNetworkCreationRequestQuery.CalculateEntityCount(), Allocator.TempJob);
        NeuralNetworkEntityBuilder.CreateNeuralNetworkEntities(ref state, neuralNetworkArchetype, neuralNetworkCreationRequestQuery, neuralNetworkCreationRequests, neuralGraphLookup);

        EntityQuery limbCreationRequestQuery = SystemAPI.QueryBuilder().WithAll<LimbEntityCreationRequest>().Build();
        using NativeArray<LimbEntityCreationRequest> limbCreationRequests = limbCreationRequestQuery.ToComponentDataArray<LimbEntityCreationRequest>(Allocator.TempJob);
        AddCollidersToCache(limbCreationRequests);
        using NativeParallelHashMap<PhenotypeLimbKey, Entity> limbEntityLookup = new(limbCreationRequestQuery.CalculateEntityCount(), Allocator.TempJob);
        using NativeParallelHashMap<PhenotypeLimbKey, LocalTransform> limbLocalTransformLookup = new(limbCreationRequestQuery.CalculateEntityCount(), Allocator.TempJob);
        LimbEntityBuilder.CreateLimbEntities(ref state, limbCreationRequestQuery, limbCreationRequests, limbEntityLookup, limbLocalTransformLookup, RenderMeshArray);

        EntityQuery jointCreationRequestQuery = SystemAPI.QueryBuilder().WithAll<JointEntityCreationRequest>().Build();
        using EntityCommandBuffer ecb = new(Allocator.TempJob);
        new CreateJointEntityJob
        {
            Ecb = ecb.AsParallelWriter(),
            JointArchetype = jointArchetype,
            LimbEntityLookup = limbEntityLookup.AsReadOnly(),
            LimbLocalTransformLookup = limbLocalTransformLookup.AsReadOnly(),
            NeuralGraphLookup = neuralGraphLookup.AsReadOnly()
        }.ScheduleParallel(jointCreationRequestQuery, state.Dependency).Complete();
        ecb.Playback(state.EntityManager);
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
