using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
    using Components.WorldObject;
    using Builders;

    public class ObjectEntityManagement : IObjectEntityManagement
    {
        public void CreateGroundPlane(World world)
        {
            if (!world.IsCreated)
                return;

            float groundSize = 1000f; // Large but not infinite to avoid physics issues
            float groundThickness = 1f;

            EntityManager entityManager = world.EntityManager;

            EntityQuery groundPlaneQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GroundPlaneTag>());
            if (!groundPlaneQuery.IsEmptyIgnoreFilter)
                return; // Ground plane already exists.

            Entity groundPlane = entityManager.CreateEntity(
                typeof(GroundPlaneTag),
                typeof(LocalTransform),
                typeof(PhysicsCollider),
                typeof(PhysicsWorldIndex)
            );

            entityManager.SetComponentData(groundPlane, new LocalTransform
            {
                Position = new float3(0f, -groundThickness * 0.5f, 0f),
                Rotation = quaternion.identity,
                Scale = 1f
            });

            BoxGeometry boxGeometry = new()
            {
                Center = float3.zero,
                Size = new float3(groundSize, groundThickness, groundSize),
                Orientation = quaternion.identity
            };

            CollisionFilter groundFilter = new()
            {
                BelongsTo = 1u, // Ground layer.
                CollidesWith = ~0u, // Collide with everything.
                GroupIndex = 0
            };

            BlobAssetReference<Collider> groundCollider = BoxCollider.Create(boxGeometry, groundFilter);

            entityManager.SetComponentData(groundPlane, new PhysicsCollider { Value = groundCollider });
            entityManager.SetSharedComponent(groundPlane, new PhysicsWorldIndex(0));
        }

        public void DestroyGroundPlane(World world)
        {
            if (!world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            EntityQuery groundPlaneQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GroundPlaneTag>());
            if (!groundPlaneQuery.IsEmptyIgnoreFilter)
            {
                Entity groundPlane = groundPlaneQuery.GetSingletonEntity();
                entityManager.DestroyEntity(groundPlane);
            }
        }

        public void CreateLightSource(World world)
        {
            if (!world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;

            EntityQuery lightSourceQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<LightSourceTag>());
            if (!lightSourceQuery.IsEmptyIgnoreFilter)
                return; // Light source already exists.

            LightSourceEntityBuilder.CreateLightSource(entityManager);
        }

        public void DestroyLightSource(World world)
        {
            if (!world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            EntityQuery lightSourceQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<LightSourceTag>());
            if (!lightSourceQuery.IsEmptyIgnoreFilter)
            {
                Entity lightSource = lightSourceQuery.GetSingletonEntity();
                entityManager.DestroyEntity(lightSource);
            }
        }
    }
}
