using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

public static class ColliderCacheManager
{
#nullable enable
    public static ColliderCache? Cache;
#nullable disable
    private static int ActiveSystemCount;

    public static void Acquire()
    {
        if (ActiveSystemCount == 0)
            Cache = new ColliderCache();
        ActiveSystemCount++;
    }

    public static void Release()
    {
        ActiveSystemCount--;
        if (ActiveSystemCount <= 0)
        {
            Cache?.Dispose();
            Cache = null;
        }
    }
}

[BurstCompile]
public struct ColliderKey : IEquatable<ColliderKey>
{
    public float3 Dimensions;
    public CollisionFilter CollisionFilter;

    public ColliderKey(float3 dimensions, CollisionFilter collisionFilter)
    {
        Dimensions = Quantize(dimensions);
        CollisionFilter = collisionFilter;
    }

    private static float3 Quantize(float3 v, float precision = 0.001f) => math.round(v / precision) * precision;

    public bool Equals(ColliderKey other)
    {
        return Dimensions.Equals(other.Dimensions) && CollisionFilter.Equals(other.CollisionFilter);
    }

    public override bool Equals(object obj) => obj is ColliderKey other && Equals(other);

    public override readonly int GetHashCode()
    {
        return Dimensions.GetHashCode() ^ CollisionFilter.GetHashCode();
    }
}

public class ColliderCache
{
    private NativeParallelHashMap<ColliderKey, BlobAssetReference<Collider>> map;
    public NativeParallelHashMap<ColliderKey, BlobAssetReference<Collider>>.ReadOnly ReadOnlyMap => map.AsReadOnly();

    public ColliderCache()
    {
        map = new NativeParallelHashMap<ColliderKey, BlobAssetReference<Collider>>(1024, Allocator.Persistent);
    }

    public void AddColliders(NativeHashSet<ColliderKey> keys)
    {
        // Check if we need to expand capacity.
        int requiredCapacity = map.Count() + keys.Count;
        if (requiredCapacity > map.Capacity)
        {
            // Expand capacity to accommodate new keys with some headroom.
            int newCapacity = math.max(requiredCapacity * 2, map.Capacity * 2);
            NativeParallelHashMap<ColliderKey, BlobAssetReference<Collider>> newMap = new(newCapacity, Allocator.Persistent);

            // Copy existing data.
            foreach (var kvp in map)
                newMap.TryAdd(kvp.Key, kvp.Value);

            map.Dispose();
            map = newMap;
        }

        foreach (ColliderKey key in keys)
            map.TryAdd(key, CreateColliderBlob(key.Dimensions, key.CollisionFilter));
    }

    private static BlobAssetReference<Collider> CreateColliderBlob(float3 dimensions, CollisionFilter collisionFilter)
    {
        BoxGeometry boxGeometry = new()
        {
            Center = float3.zero,
            Size = dimensions,
            Orientation = quaternion.identity
        };
        return BoxCollider.Create(boxGeometry, collisionFilter, Material.Default);
    }

    public void Dispose()
    {
        var values = map.GetValueArray(Allocator.Temp);
        foreach (var blob in values)
        {
            if (blob.IsCreated)
                blob.Dispose();
        }
        values.Dispose();
        map.Dispose();
    }
}