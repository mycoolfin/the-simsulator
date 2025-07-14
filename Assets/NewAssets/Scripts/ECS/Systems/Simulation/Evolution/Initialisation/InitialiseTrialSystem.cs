using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;

public struct InitialiseTrialRequest : IComponentData
{
    public NewAssets.TrialType TrialType;
}

public struct InitialisingTrialTag : IComponentData { }

[UpdateInGroup(typeof(TrialInitialisationSystemGroup))]
public partial struct InitialiseTrialSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EnvironmentSubScenes>();
        state.RequireForUpdate<InitialiseTrialRequest>();
    }

    public void OnUpdate(ref SystemState state)
    {
        Entity requestEntity = SystemAPI.GetSingletonEntity<InitialiseTrialRequest>();

        state.EntityManager.CreateSingleton<InitialisingTrialTag>();

        NewAssets.TrialType trialType = SystemAPI.GetComponent<InitialiseTrialRequest>(requestEntity).TrialType;
        Entity environmentEntity = Entity.Null;
        if (trialType == NewAssets.TrialType.GroundDistance)
        {
            EntitySceneReference groundDistanceEnvironment = SystemAPI.GetSingleton<EnvironmentSubScenes>().GroundEnvironment;
            environmentEntity = SceneSystem.LoadSceneAsync(state.World.Unmanaged, groundDistanceEnvironment, new() { Flags = SceneLoadFlags.BlockOnStreamIn });
            state.EntityManager.CreateSingleton(new RepositionPhenotypesRequest { GroundY = 0f });
        }
        else if (trialType == NewAssets.TrialType.WaterDistance)
        {
            EntitySceneReference waterDistanceEnvironment = SystemAPI.GetSingleton<EnvironmentSubScenes>().WaterEnvironment;
            environmentEntity = SceneSystem.LoadSceneAsync(state.World.Unmanaged, waterDistanceEnvironment, new() { Flags = SceneLoadFlags.BlockOnStreamIn });
            state.EntityManager.CreateSingleton(new FluidSimulation() { Enabled = 0 });
        }

        if (environmentEntity != Entity.Null)
            state.EntityManager.CreateSingleton(new LoadingEnvironment { EnvironmentEntity = environmentEntity });

        state.EntityManager.DestroyEntity(requestEntity);

        // Pause FixedStep updates.
        // This will be re-enabled in WaitForInitialisationCompleteSystem.
        state.WorldUnmanaged.GetExistingSystemState<FixedStepSimulationSystemGroup>().Enabled = false;
        state.WorldUnmanaged.GetExistingSystemState<WaitForInitialisationCompleteSystem>().Enabled = true;
    }
}
