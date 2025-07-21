using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.Systems.Simulation
{
    public struct FixedStepPauseAfterTimerRequest : IComponentData
    {
        public float TimerSeconds;
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct FixedStepPauseAfterTimerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FixedStepPauseAfterTimerRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity requestEntity = SystemAPI.GetSingletonEntity<FixedStepPauseAfterTimerRequest>();
            FixedStepPauseAfterTimerRequest request = SystemAPI.GetSingleton<FixedStepPauseAfterTimerRequest>();

            if (request.TimerSeconds > 0f)
            {
                // Decrease the timer.
                request.TimerSeconds -= SystemAPI.Time.DeltaTime;
                SystemAPI.SetSingleton(request);
            }
            else // Timer has expired.
            {
                // Destroy the request entity.
                state.EntityManager.DestroyEntity(requestEntity);

                // Pause the fixed step simulation.
                state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>().Enabled = false;
            }
        }
    }
}
