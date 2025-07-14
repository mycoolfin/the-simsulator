using Unity.Entities;

[UpdateInGroup(typeof(TrialInitialisationSystemGroup))]
[UpdateAfter(typeof(InitialiseTrialSystem))]
[UpdateAfter(typeof(WaitForEnvironmentToLoadSystem))]
[UpdateAfter(typeof(RepositionPhenotypesSystem))]
public partial struct WaitForInitialisationCompleteSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<InitialisingTrialTag>();

        state.Enabled = false; // Enabled by InitialiseTrialSystem.
    }

    public void OnUpdate(ref SystemState state)
    {
        // If any init markers still exist, don’t run yet.
        if (SystemAPI.HasSingleton<LoadingEnvironment>() ||
            SystemAPI.HasSingleton<RepositionPhenotypesRequest>())
            return;

        state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<InitialisingTrialTag>());

        // Resume physics updates.
        state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>().Enabled = true;

        // Disable this system after it has run once.
        state.Enabled = false;
    }
}
