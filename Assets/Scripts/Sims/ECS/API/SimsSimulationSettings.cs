using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.API
{
    using Core.ECS.API;
    using Systems.Simulation.Joints;
    using Systems.Simulation.Limbs;

    public class SimsSimulationSettings : SimulationSettingsBase
    {
        public void SetGravity(World world, float3 gravity)
        {
            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(PhysicsStep));
            PhysicsStep p = PhysicsStep.Default;
            p.Gravity = gravity;
            if (query.IsEmpty) entityManager.CreateSingleton(p);
            else query.SetSingleton(p);
        }

        public void SetFluidSimulation(World world, bool enabled, float fluidDensity = 1000f)
        {
            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(FluidSimulationSettings));
            FluidSimulationSettings settings = new() { Enabled = (byte)(enabled ? 1 : 0), FluidDensity = fluidDensity };
            if (query.IsEmptyIgnoreFilter) entityManager.CreateSingleton(settings);
            else query.SetSingleton(settings);
        }

        public void SetJointBreakSystemEnabled(World world, bool enabled)
        {
            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(JointBreakSystemSettings));
            JointBreakSystemSettings settings = new() { Enabled = enabled };
            if (query.IsEmptyIgnoreFilter) entityManager.CreateSingleton(settings);
            else query.SetSingleton(settings);
        }
    }
}
