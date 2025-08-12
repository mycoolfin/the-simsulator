using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
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
    }
}
