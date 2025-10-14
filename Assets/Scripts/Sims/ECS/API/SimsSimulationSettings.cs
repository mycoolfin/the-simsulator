using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.API
{
    using Core.ECS.API;
    using Systems.Simulation.Joints;

    public class SimsSimulationSettings : SimulationSettingsBase
    {
        public void SetJointBreakSystemEnabled(World world, bool enabled)
        {
            if (!world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(typeof(JointBreakSystemSettings));
            JointBreakSystemSettings settings = new() { Enabled = enabled };
            if (query.IsEmptyIgnoreFilter) entityManager.CreateSingleton(settings);
            else query.SetSingleton(settings);
        }
    }
}
