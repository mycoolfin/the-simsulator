using Unity.Entities;

public static class SystemSettingsAPI
{
    public static void SetSimulationSpeedMode(World world, SimulationRateMode mode)
    {
        EntityManager entityManager = world.EntityManager;
        SimulationRateControllerSettings settings = new() { Mode = mode };
        EntityQuery query = entityManager.CreateEntityQuery(typeof(SimulationRateControllerSettings));
        if (query.IsEmptyIgnoreFilter)
            return;

        Entity singletonEntity = query.GetSingletonEntity();
        entityManager.SetComponentData(singletonEntity, settings);
    }

    public static void SetJointBreakSystemEnabled(World world, bool enabled)
    {
        EntityManager entityManager = world.EntityManager;
        JointBreakSystemSettings settings = new() { Enabled = enabled };
        EntityQuery query = entityManager.CreateEntityQuery(typeof(JointBreakSystemSettings));
        if (query.IsEmptyIgnoreFilter)
            return;

        Entity singletonEntity = query.GetSingletonEntity();
        entityManager.SetComponentData(singletonEntity, settings);
    }
}
