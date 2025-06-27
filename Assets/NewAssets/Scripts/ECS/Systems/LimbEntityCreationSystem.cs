using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine.Rendering;

public struct ColliderKey : IEquatable<ColliderKey>
{
    public float3 Dimensions;
    public uint BelongsTo;
    public uint CollidesWith;
    public int GroupIndex;

    public ColliderKey(float3 dimensions, uint belongsTo, uint collidesWith, int groupIndex)
    {
        Dimensions = Quantize(dimensions);
        BelongsTo = belongsTo;
        CollidesWith = collidesWith;
        GroupIndex = groupIndex;
    }

    private static float3 Quantize(float3 v, float precision = 0.001f) => math.round(v / precision) * precision;

    public bool Equals(ColliderKey other)
    {
        return Dimensions.Equals(other.Dimensions) &&
               BelongsTo == other.BelongsTo &&
               CollidesWith == other.CollidesWith &&
               GroupIndex == other.GroupIndex;
    }

    public override bool Equals(object obj) => obj is ColliderKey other && Equals(other);

    public override readonly int GetHashCode()
    {
        return (int)math.hash(new uint4(
            math.asuint(Dimensions.x),
            math.asuint(Dimensions.y),
            math.asuint(Dimensions.z),
            BelongsTo ^ CollidesWith ^ (uint)GroupIndex
        ));
    }
}

public class ColliderCache
{
    private NativeParallelHashMap<ColliderKey, BlobAssetReference<Collider>> map;
    public NativeParallelHashMap<ColliderKey, BlobAssetReference<Collider>>.ReadOnly ReadOnlyMap => map.AsReadOnly();

    public ColliderCache()
    {
        map = new NativeParallelHashMap<ColliderKey, BlobAssetReference<Collider>>(64, Allocator.Persistent);
    }

    public void AddColliders(NativeHashSet<ColliderKey> keys)
    {
        foreach (ColliderKey key in keys)
            map.TryAdd(key, CreateColliderBlob(key.Dimensions, key.BelongsTo, key.CollidesWith, key.GroupIndex));
    }

    private static BlobAssetReference<Collider> CreateColliderBlob(float3 dimensions, uint belongsTo, uint collidesWith, int groupIndex)
    {
        BoxGeometry boxGeometry = new()
        {
            Center = float3.zero,
            Size = dimensions,
            Orientation = quaternion.identity
        };

        CollisionFilter filter = new()
        {
            BelongsTo = belongsTo,
            CollidesWith = collidesWith,
            GroupIndex = groupIndex
        };

        return BoxCollider.Create(boxGeometry, filter, Material.Default);
    }

    public void Dispose()
    {
        map.Dispose();
    }
}

public struct LimbCreationRequest : IComponentData
{
    public int PhenotypeId;
    public float3 Position;
    public quaternion Rotation;
    public float3 Dimensions;
    public float Mass;
    public float4 Color;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
public partial struct LimbEntityCreationSystem : ISystem
{
    public static RenderMeshArray RenderMeshArray;

    private static ColliderCache colliderCache;

    static readonly PhysicsDamping DefaultDamping = new() { Linear = 0.01f, Angular = 0.01f };

    private const int INSTANTIATION_KEY = 1;
    private const int DISPOSAL_KEY = 2;

    private const uint DEFAULT_LAYER = 1 << 0;
    private const uint LIMB_LAYER = 1 << 9;

    public readonly void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<LimbCreationRequest>();
        colliderCache = new ColliderCache();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (RenderMeshArray.MaterialReferences == null || RenderMeshArray.MaterialReferences.Length == 0 ||
            RenderMeshArray.MeshReferences == null || RenderMeshArray.MeshReferences.Length == 0)
            return;


        EntityQuery requestQuery = SystemAPI.QueryBuilder().WithAll<LimbCreationRequest>().Build();

        NativeArray<LimbCreationRequest> requests = requestQuery.ToComponentDataArray<LimbCreationRequest>(Allocator.TempJob);

        if (requests.Length > 0)
        {
            // Add unique colliders to the cache.
            NativeHashSet<ColliderKey> uniqueKeys = new(requests.Length, Allocator.TempJob);
            for (int i = 0; i < requests.Length; i++)
                uniqueKeys.Add(new ColliderKey(
                    requests[i].Dimensions,
                    LIMB_LAYER,
                    LIMB_LAYER | DEFAULT_LAYER,
                    requests[i].PhenotypeId
                ));
            colliderCache.AddColliders(uniqueKeys);
            uniqueKeys.Dispose();

            // Instantiate limb entities.
            Entity limbPrototype = CreateLimbPrototype(state.EntityManager);
            EntityCommandBuffer ecb = new(Allocator.TempJob);
            CreateLimbEntityJob createJob = new()
            {
                Ecb = ecb.AsParallelWriter(),
                LimbPrototype = limbPrototype,
                ColliderMap = colliderCache.ReadOnlyMap
            };
            createJob.ScheduleParallel(requestQuery, state.Dependency).Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            state.EntityManager.DestroyEntity(limbPrototype);
        }

        requests.Dispose();
    }

    public readonly void OnDestroy(ref SystemState state)
    {
        colliderCache.Dispose();
    }

    private readonly Entity CreateLimbPrototype(EntityManager entityManager)
    {
        Entity limbPrototype = entityManager.CreateEntity();

        entityManager.AddComponentData(limbPrototype, new LimbTag());
        entityManager.AddComponentData(limbPrototype, new PhenotypeId { Value = -1 });

        // Transform.
        entityManager.AddComponentData(limbPrototype, new LocalTransform());
        entityManager.AddComponentData(limbPrototype, new PostTransformMatrix());
        entityManager.AddComponentData(limbPrototype, new LocalToWorld());

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

        public void Execute(Entity requestEntity, ref LimbCreationRequest requestData)
        {
            Ecb.DestroyEntity(DISPOSAL_KEY, requestEntity); // Destroy request entity in the Disposal stage.

            // Limbs.
            Entity limbEntity = Ecb.Instantiate(INSTANTIATION_KEY, LimbPrototype);

            Ecb.SetComponent(INSTANTIATION_KEY, limbEntity, new PhenotypeId
            {
                Value = requestData.PhenotypeId
            });

            // Transform.
            Ecb.SetComponent(INSTANTIATION_KEY, limbEntity, LocalTransform.FromPositionRotationScale(
                requestData.Position,
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

            // Physics.
            ColliderKey key = new(
                requestData.Dimensions,
                LIMB_LAYER,
                LIMB_LAYER | DEFAULT_LAYER,
                requestData.PhenotypeId
            );
            ColliderMap.TryGetValue(key, out BlobAssetReference<Collider> collider);
            Ecb.SetComponent(INSTANTIATION_KEY, limbEntity, new PhysicsCollider { Value = collider });
            Ecb.SetComponent(INSTANTIATION_KEY, limbEntity, PhysicsMass.CreateDynamic(collider.Value.MassProperties, requestData.Mass));
        }
    }
}
