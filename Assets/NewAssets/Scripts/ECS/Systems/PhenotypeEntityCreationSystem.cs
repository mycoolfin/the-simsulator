using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine.Rendering;
using mycoolfin.TheSimsulator;

public struct LimbEntityCreationRequest : IComponentData
{
    public int LimbIndex;
    public ulong PhenotypeGid;
    public float3 Position;
    public quaternion Rotation;
    public float3 Dimensions;
    public float Mass;
    public float4 Color;
    public float3 PhysicsPositionOffset;
    public uint PhysicsWorldIndex;
}

public struct JointEntityCreationRequest : IComponentData
{
    public ulong PhenotypeGid;
    public mycoolfin.TheSimsulator.Sims.JointType JointType;
    public int ReferenceLimbIndex;
    public int AttachedLimbIndex;
    public float3 ReferenceLimbSpaceAnchor;
    public float3 ReferenceLimbSpaceXAxis;
    public float3 ReferenceLimbSpaceYAxis;
    public float3 ReferenceLimbSpaceZAxis;
    public bool FlippedHandedness;
    public float3 AngleLimits;
    public uint PhysicsWorldIndex;
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

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
public partial struct PhenotypeEntityCreationSystem : ISystem
{
    public static RenderMeshArray RenderMeshArray;

    private static readonly PhysicsDamping DefaultDamping = new() { Linear = 0.01f, Angular = 0.05f };

    public readonly void OnCreate(ref SystemState state)
    {
        ColliderCacheManager.Acquire();
        state.RequireForUpdate<LimbEntityCreationRequest>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (RenderMeshArray.MaterialReferences == null || RenderMeshArray.MaterialReferences.Length == 0 ||
            RenderMeshArray.MeshReferences == null || RenderMeshArray.MeshReferences.Length == 0)
            return;

        EntityQuery limbCreationRequestQuery = SystemAPI.QueryBuilder().WithAll<LimbEntityCreationRequest>().Build();
        NativeArray<LimbEntityCreationRequest> limbCreationRequests = limbCreationRequestQuery.ToComponentDataArray<LimbEntityCreationRequest>(Allocator.TempJob);
        if (limbCreationRequests.Length > 0)
        {
            AddCollidersToCache(limbCreationRequests);
            InstantiateLimbEntities(ref state, limbCreationRequestQuery);
        }

        EntityQuery limbEntityQuery = SystemAPI.QueryBuilder().WithAll<PhenotypeGid, LimbIndex, LocalTransform>().Build();
        NativeParallelHashMap<PhenotypeLimbKey, Entity> limbEntityLookup = new(limbCreationRequests.Length, Allocator.TempJob);
        NativeParallelHashMap<PhenotypeLimbKey, LocalTransform> limbLocalTransformLookup = new(limbCreationRequests.Length, Allocator.TempJob);
        BuildLimbEntityLookup(ref state, limbEntityQuery, limbEntityLookup, limbLocalTransformLookup);

        EntityQuery jointCreationRequestQuery = SystemAPI.QueryBuilder().WithAll<JointEntityCreationRequest>().Build();
        NativeArray<JointEntityCreationRequest> jointCreationRequests = jointCreationRequestQuery.ToComponentDataArray<JointEntityCreationRequest>(Allocator.TempJob);
        if (jointCreationRequests.Length > 0)
        {
            InstantiateJointEntities(ref state, jointCreationRequestQuery, limbEntityLookup, limbLocalTransformLookup);
        }
        limbCreationRequests.Dispose();
        jointCreationRequests.Dispose();
        limbEntityLookup.Dispose();
        limbLocalTransformLookup.Dispose();
    }

    public readonly void OnDestroy(ref SystemState state)
    {
        ColliderCacheManager.Release();
    }

    private readonly void AddCollidersToCache(NativeArray<LimbEntityCreationRequest> requests)
    {
        NativeHashSet<ColliderKey> uniqueKeys = new(requests.Length, Allocator.TempJob);
        for (int i = 0; i < requests.Length; i++)
            uniqueKeys.Add(new(requests[i].Dimensions));
        ColliderCacheManager.Cache.AddColliders(uniqueKeys);
        uniqueKeys.Dispose();
    }

    private void InstantiateLimbEntities(ref SystemState state, EntityQuery limbCreationRequestQuery)
    {
        Entity limbPrototype = CreateLimbPrototype(state.EntityManager);
        EntityCommandBuffer ecb = new(Allocator.TempJob);
        new CreateLimbEntityJob
        {
            Ecb = ecb.AsParallelWriter(),
            LimbPrototype = limbPrototype,
            ColliderMap = ColliderCacheManager.Cache.ReadOnlyMap,
        }.ScheduleParallel(limbCreationRequestQuery, state.Dependency).Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        state.EntityManager.DestroyEntity(limbPrototype);
    }

    private readonly Entity CreateLimbPrototype(EntityManager entityManager)
    {
        Entity limbPrototype = entityManager.CreateEntity();

        // IDs.
        entityManager.AddComponentData(limbPrototype, new LimbIndex());
        entityManager.AddComponentData(limbPrototype, new PhenotypeGid());

        // Transform.
        entityManager.AddComponentData(limbPrototype, new LocalTransform());
        entityManager.AddComponentData(limbPrototype, new PostTransformMatrix());

        // Rendering.
        entityManager.AddComponentData(limbPrototype, new URPMaterialPropertyBaseColor());
        entityManager.AddComponentData(limbPrototype, new RenderBounds());
        RenderMeshUtility.AddComponents(
            limbPrototype,
            entityManager,
            new(
                shadowCastingMode: ShadowCastingMode.Off,
                receiveShadows: false
            ),
            RenderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
        );
        entityManager.AddComponentData(limbPrototype, new VisualOffset());

        // Physics.
        entityManager.AddSharedComponent(limbPrototype, new PhysicsWorldIndex());
        entityManager.AddComponentData(limbPrototype, new PhysicsCollider());
        entityManager.AddComponentData(limbPrototype, new PhysicsMass());
        entityManager.AddComponentData(limbPrototype, new PhysicsVelocity());
        entityManager.AddComponentData(limbPrototype, DefaultDamping);

        return limbPrototype;
    }

    [BurstCompile]
    public partial struct CreateLimbEntityJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        public Entity LimbPrototype;
        [ReadOnly] public NativeParallelHashMap<ColliderKey, BlobAssetReference<Collider>>.ReadOnly ColliderMap;

        private const int INSTANTIATION_KEY = 1;
        private const int DISPOSAL_KEY = 2;

        public void Execute(Entity requestEntity, ref LimbEntityCreationRequest requestData)
        {
            Ecb.DestroyEntity(DISPOSAL_KEY, requestEntity); // Destroy request entity in the Disposal stage.

            Entity limbEntity = Ecb.Instantiate(INSTANTIATION_KEY, LimbPrototype);

            // IDs.
            Ecb.SetComponent(INSTANTIATION_KEY, limbEntity, new LimbIndex
            {
                Value = requestData.LimbIndex
            });
            Ecb.SetComponent(INSTANTIATION_KEY, limbEntity, new PhenotypeGid
            {
                Value = requestData.PhenotypeGid
            });

            // Transform.
            Ecb.SetComponent(INSTANTIATION_KEY, limbEntity, LocalTransform.FromPositionRotationScale(
                requestData.Position + requestData.PhysicsPositionOffset,
                requestData.Rotation,
                1f
            ));
            Ecb.SetComponent(INSTANTIATION_KEY, limbEntity, new PostTransformMatrix
            {
                Value = float4x4.Scale(requestData.Dimensions)
            });

            // Rendering.
            Ecb.SetComponent(INSTANTIATION_KEY, limbEntity, new URPMaterialPropertyBaseColor
            {
                Value = requestData.Color
            });
            Ecb.SetComponent(INSTANTIATION_KEY, limbEntity, new RenderBounds
            {
                Value = new AABB
                {
                    Center = float3.zero,
                    Extents = requestData.Dimensions * 0.5f
                }
            });
            Ecb.SetComponent(INSTANTIATION_KEY, limbEntity, new VisualOffset
            {
                Offset = -requestData.PhysicsPositionOffset
            });

            // Physics.
            Ecb.SetSharedComponent(INSTANTIATION_KEY, limbEntity, new PhysicsWorldIndex(requestData.PhysicsWorldIndex));
            ColliderMap.TryGetValue(new(requestData.Dimensions), out BlobAssetReference<Collider> collider);
            Ecb.SetComponent(INSTANTIATION_KEY, limbEntity, new PhysicsCollider { Value = collider });
            Ecb.SetComponent(INSTANTIATION_KEY, limbEntity, PhysicsMass.CreateDynamic(collider.Value.MassProperties, requestData.Mass));
        }
    }

    private void BuildLimbEntityLookup(ref SystemState state, EntityQuery limbEntityQuery, NativeParallelHashMap<PhenotypeLimbKey, Entity> limbEntityLookup, NativeParallelHashMap<PhenotypeLimbKey, LocalTransform> limbLocalTransformLookup)
    {
        new BuildLimbEntityLookupJob
        {
            LimbEntityLookup = limbEntityLookup.AsParallelWriter(),
            LimbLocalTransformLookup = limbLocalTransformLookup.AsParallelWriter(),
        }.ScheduleParallel(limbEntityQuery, state.Dependency).Complete();
    }

    [BurstCompile]
    private partial struct BuildLimbEntityLookupJob : IJobEntity
    {
        public NativeParallelHashMap<PhenotypeLimbKey, Entity>.ParallelWriter LimbEntityLookup;
        public NativeParallelHashMap<PhenotypeLimbKey, LocalTransform>.ParallelWriter LimbLocalTransformLookup;

        public void Execute(Entity entity, in PhenotypeGid phenotypeGid, in LimbIndex limbIndex, in LocalTransform localTransform)
        {
            PhenotypeLimbKey key = new(phenotypeGid.Value, limbIndex.Value);
            LimbEntityLookup.TryAdd(key, entity);
            LimbLocalTransformLookup.TryAdd(key, localTransform);
        }
    }

    private void InstantiateJointEntities(ref SystemState state, EntityQuery jointCreationRequestQuery, NativeParallelHashMap<PhenotypeLimbKey, Entity> limbEntityLookup, NativeParallelHashMap<PhenotypeLimbKey, LocalTransform> limbLocalTransformLookup)
    {
        EntityCommandBuffer ecb = new(Allocator.TempJob);

        new CreateJointEntityJob
        {
            Ecb = ecb.AsParallelWriter(),
            LimbEntityLookup = limbEntityLookup.AsReadOnly(),
            LimbLocalTransformLookup = limbLocalTransformLookup.AsReadOnly()
        }.ScheduleParallel(jointCreationRequestQuery, state.Dependency).Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public partial struct CreateJointEntityJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public NativeParallelHashMap<PhenotypeLimbKey, Entity>.ReadOnly LimbEntityLookup;
        [ReadOnly] public NativeParallelHashMap<PhenotypeLimbKey, LocalTransform>.ReadOnly LimbLocalTransformLookup;

        private const int INSTANTIATION_KEY = 1;
        private const int DISPOSAL_KEY = 2;

        public void Execute(Entity requestEntity, ref JointEntityCreationRequest requestData)
        {
            Ecb.DestroyEntity(DISPOSAL_KEY, requestEntity);

            PhenotypeLimbKey refKey = new(requestData.PhenotypeGid, requestData.ReferenceLimbIndex);
            PhenotypeLimbKey attKey = new(requestData.PhenotypeGid, requestData.AttachedLimbIndex);
            if (!LimbEntityLookup.TryGetValue(refKey, out Entity referenceLimbEntity)) return;
            if (!LimbEntityLookup.TryGetValue(attKey, out Entity attachedLimbEntity)) return;

            Entity jointEntity = Ecb.CreateEntity(INSTANTIATION_KEY);
            Ecb.AddSharedComponent(INSTANTIATION_KEY, jointEntity, new PhysicsWorldIndex(requestData.PhysicsWorldIndex));
            Ecb.AddComponent(INSTANTIATION_KEY, jointEntity, new PhysicsConstrainedBodyPair(
                referenceLimbEntity,
                attachedLimbEntity,
                false
            ));
            PhysicsJoint joint = JointFactory.CreateJoint(
                requestData.JointType,
                LimbLocalTransformLookup[refKey].ToMatrix(),
                LimbLocalTransformLookup[attKey].ToMatrix(),
                requestData.ReferenceLimbSpaceAnchor,
                requestData.ReferenceLimbSpaceXAxis,
                requestData.ReferenceLimbSpaceYAxis,
                requestData.ReferenceLimbSpaceZAxis,
                requestData.AngleLimits
            );
            Ecb.AddComponent(INSTANTIATION_KEY, jointEntity, joint);
        }
    }
}
