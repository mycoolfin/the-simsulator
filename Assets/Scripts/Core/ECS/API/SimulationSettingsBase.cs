using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
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
            SetFluidSimulation(world, false, 1f);
            SetPhenotypeRepositionerSettings(world, new PhenotypeRepositionerSettings
            {
                Enabled = true,
                Pivot = BoundingBoxPivot.BoundingBoxCenterYMin, // Bottom of the bounding box.
                AllowedZone = new() { Center = new float3(0f, 20f, 0f), Extents = new float3(500f, 20f, 500f) }, // Box sitting at ground level (y=0).
                Margin = 0.1f,
                TargetPosition = new float3(0f, 0.01f, 0f), // Just above ground level.
                ZeroVelocitiesOnReposition = true
            });
        }

        public void SetToAquaticDefaults(World world)
        {
            SetGravity(world, float3.zero);
            SetFluidSimulation(world, true, 1f);
            SetPhenotypeRepositionerSettings(world, new PhenotypeRepositionerSettings
            {
                Enabled = true,
                Pivot = BoundingBoxPivot.BoundingBoxCenter, // Center of the bounding box.
                AllowedZone = new() { Center = new float3(0f, 0f, 0f), Extents = new float3(500f, 500f, 500f) }, // Box centered at origin.
                Margin = 0.1f,
                TargetPosition = float3.zero, // Dead center.
                ZeroVelocitiesOnReposition = true
            });
        }
    }
}
