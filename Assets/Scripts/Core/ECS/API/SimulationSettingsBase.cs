using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
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
    }
}
