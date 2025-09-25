using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
    using Components.WorldObject;
    using Components.Phenotype;
    using Systems.Simulation.Phenotypes;
    using Systems.Simulation.Physics;
    using Systems.Simulation.SimulationRate;

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

            EntityQuery groundPlaneQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GroundPlaneTag>());
            if (!groundPlaneQuery.IsEmptyIgnoreFilter)
                return; // Ground plane already exists.

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

        public void SetPhenotypeRepositionerSettings(World world, PhenotypeRepositionerSettings settings)
        {
            if (!world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(PhenotypeRepositionerSettings));
            PhenotypeRepositionerSettings newSettings = settings;
            if (query.IsEmptyIgnoreFilter) entityManager.CreateSingleton(newSettings);
            else query.SetSingleton(newSettings);
        }

        public void SetToTerrestrialDefaults(World world)
        {
            SetGravity(world, PhysicsStep.Default.Gravity);
            CreateGroundPlane(world);
            SetPhenotypeRepositionerSettings(world, new PhenotypeRepositionerSettings
            {
                Enabled = true,
                Pivot = BoundingBoxPivot.BoundingBoxCenterYMin, // Bottom of the bounding box.
                AllowedZone = new() { Center = new float3(0f, 500f, 0f), Extents = new float3(500f, 500f, 500f) }, // 1000^3 m box sitting at ground level (y=0).
                Margin = 0.1f,
                TargetPosition = new float3(0f, 0.01f, 0f), // Just above ground level.
                ZeroVelocitiesOnReposition = true
            });
        }

        public void SetToAquaticDefaults(World world)
        {
            SetGravity(world, float3.zero);
            SetFluidSimulation(world, true, 1f);
            DestroyGroundPlane(world);
            SetPhenotypeRepositionerSettings(world, new PhenotypeRepositionerSettings
            {
                Enabled = true,
                Pivot = BoundingBoxPivot.BoundingBoxCenter, // Center of the bounding box.
                AllowedZone = new() { Center = new float3(0f, 0f, 0f), Extents = new float3(500f, 500f, 500f) }, // 1000^3 m box centered at origin.
                Margin = 0.1f,
                TargetPosition = float3.zero, // Dead center.
                ZeroVelocitiesOnReposition = true
            });
        }
    }
}
