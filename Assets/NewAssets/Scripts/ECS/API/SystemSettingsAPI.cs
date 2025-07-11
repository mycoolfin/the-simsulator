using Unity.Entities;

public static class SystemSettingsAPI
{
    public static SimulationRateControllerSettings GetSimulationRateControllerSettings(World world)
    {
        EntityManager entityManager = world.EntityManager;
        EntityQuery query = entityManager.CreateEntityQuery(typeof(SimulationRateControllerSettings));
        if (query.IsEmptyIgnoreFilter)
            return default;

        Entity singletonEntity = query.GetSingletonEntity();
        return entityManager.GetComponentData<SimulationRateControllerSettings>(singletonEntity);
    }

    public static void SetSimulationRateControllerSettings(World world, SimulationRateMode mode, float stopAfterSeconds = 0f)
    {
        EntityManager entityManager = world.EntityManager;
        EntityQuery query = entityManager.CreateEntityQuery(typeof(SimulationRateControllerSettings));
        if (query.IsEmptyIgnoreFilter)
            return;

        Entity singletonEntity = query.GetSingletonEntity();
        SimulationRateControllerSettings settings = entityManager.GetComponentData<SimulationRateControllerSettings>(singletonEntity);
        settings.Mode = mode;
        settings.StopAfterSeconds = stopAfterSeconds;
        entityManager.SetComponentData(singletonEntity, settings);
    }

    public static void SetJointBreakSystemEnabled(World world, bool enabled)
    {
        EntityManager entityManager = world.EntityManager;
        EntityQuery query = entityManager.CreateEntityQuery(typeof(JointBreakSystemSettings));
        if (query.IsEmptyIgnoreFilter)
            return;

        Entity singletonEntity = query.GetSingletonEntity();
        JointBreakSystemSettings settings = entityManager.GetComponentData<JointBreakSystemSettings>(singletonEntity);
        settings.Enabled = enabled;
        entityManager.SetComponentData(singletonEntity, settings);
    }
}
