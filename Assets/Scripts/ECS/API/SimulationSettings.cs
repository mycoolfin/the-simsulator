using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.API
{
    using Systems.Simulation.Limbs;
    using Systems.Simulation.Joints;
    using Systems.Simulation.SimulationRate;

    // TODO: move
    public struct SimulationRateInfo
    {
        public float FrameBudget;
        public float ActualTPS;
    }

    public static class SimulationSettings
    {
        public static SimulationRateInfo GetSimulationRateInfo(World world)
        {
            // TODO
            return new SimulationRateInfo();
        }

        public static void SetSimulationRateControllerMode(World world, SimulationRateMode mode)
        {
            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(SimulationRateControllerSettings));
            SimulationRateControllerSettings settings = new() { Mode = mode };
            if (query.IsEmptyIgnoreFilter) entityManager.CreateSingleton(settings);
            else query.SetSingleton(settings);
        }

        public static void SetJointBreakSystemEnabled(World world, bool enabled)
        {
            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(JointBreakSystemSettings));
            JointBreakSystemSettings settings = new() { Enabled = enabled };
            if (query.IsEmptyIgnoreFilter) entityManager.CreateSingleton(settings);
            else query.SetSingleton(settings);
        }

        public static void SetFluidSimulation(World world, bool enabled, float fluidDensity = 1000f)
        {
            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(FluidSimulationSettings));
            FluidSimulationSettings settings = new() { Enabled = (byte)(enabled ? 1 : 0), FluidDensity = fluidDensity };
            if (query.IsEmptyIgnoreFilter) entityManager.CreateSingleton(settings);
            else query.SetSingleton(settings);
        }

        public static void SetGravity(World world, float3 gravity)
        {
            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(PhysicsStep));
            PhysicsStep p = PhysicsStep.Default;
            p.Gravity = gravity;
            if (query.IsEmpty) entityManager.CreateSingleton(p);
            else query.SetSingleton(p);
        }
    }
}
