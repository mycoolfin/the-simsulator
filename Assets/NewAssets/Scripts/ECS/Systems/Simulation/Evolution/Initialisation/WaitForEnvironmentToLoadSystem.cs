using Unity.Entities;
using Unity.Scenes;

public struct LoadingEnvironment : IComponentData
{
    public Entity EnvironmentEntity;
}

[UpdateInGroup(typeof(TrialInitialisationSystemGroup))]
[UpdateAfter(typeof(InitialiseTrialSystem))]
public partial struct WaitForEnvironmentToLoadSystem : ISystem
{
    private const int WAIT_FOR_FRAMES = 0;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<LoadingEnvironment>();
    }

    public void OnUpdate(ref SystemState state)
    {
        Entity loadingEntity = SystemAPI.GetSingletonEntity<LoadingEnvironment>();
        LoadingEnvironment loadingEnvironment = SystemAPI.GetComponent<LoadingEnvironment>(loadingEntity);

        if (!SceneSystem.IsSceneLoaded(state.WorldUnmanaged, loadingEnvironment.EnvironmentEntity))
            return; // Wait for the environment to load.

        state.EntityManager.DestroyEntity(loadingEntity);
    }
}
