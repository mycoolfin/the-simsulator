using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
    using Systems.Simulation.Physics;
    using Systems.Simulation.SimulationRate;

    public struct GroundPlaneTag : IComponentData { }

    public abstract class SimulationSettingsBase : ISimulationSettings
    {
        public SimulationRateInfo GetSimulationRateInfo(World world)
        {
            // TODO
            return new SimulationRateInfo();
        }

        public void SetSimulationRateControllerMode(World world, SimulationRateMode mode)
        {
            if (!world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(SimulationRateControllerSettings));
            SimulationRateControllerSettings settings = new() { Mode = mode };
            if (query.IsEmptyIgnoreFilter) entityManager.CreateSingleton(settings);
            else query.SetSingleton(settings);
        }

        public void SetGravity(World world, float3 gravity)
        {
            if (!world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(PhysicsStep));
            PhysicsStep p = PhysicsStep.Default;
            p.Gravity = gravity;
            if (query.IsEmpty) entityManager.CreateSingleton(p);
            else query.SetSingleton(p);
        }

        public void SetFluidSimulation(World world, bool enabled, float fluidDensity = 1000f)
        {
            if (!world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(FluidSimulationSettings));
            FluidSimulationSettings settings = new() { Enabled = (byte)(enabled ? 1 : 0), FluidDensity = fluidDensity };
            if (query.IsEmptyIgnoreFilter) entityManager.CreateSingleton(settings);
            else query.SetSingleton(settings);
        }

        public void CreateGroundPlane(World world)
        {
            if (!world.IsCreated)
                return;

            float groundSize = 1000f; // Large but not infinite to avoid physics issues
            float groundThickness = 1f;

            EntityManager entityManager = world.EntityManager;

            Entity groundPlane = entityManager.CreateEntity();

            entityManager.AddComponentData(groundPlane, new GroundPlaneTag());

            entityManager.AddComponentData(groundPlane, new LocalTransform
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

            entityManager.AddComponentData(groundPlane, new PhysicsCollider { Value = groundCollider });
            entityManager.AddSharedComponent(groundPlane, new PhysicsWorldIndex(0));
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
    }
}
