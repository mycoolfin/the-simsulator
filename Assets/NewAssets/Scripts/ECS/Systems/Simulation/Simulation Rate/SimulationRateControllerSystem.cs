using Unity.Entities;
using UnityEngine;

public enum SimulationRateMode : byte
{
    Paused,
    RealTime,
    FullSpeed
}

public struct SimulationRateControllerSettings : IComponentData
{
    public SimulationRateMode Mode;
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct SimulationRateControllerSystem : ISystem
{
    private const float FRAME_BUDGET = 1f / 15f; // 15 FPS.
    private const float MAX_REAL_TIME_STEP = 1f / 30f; // 30 FPS max for real-time mode.
    private const float FIXED_SIMULATION_STEP = 1f / 60f; // 60 Hz fixed timestep for full-speed mode.

    private SimulationRateMode currentMode;

    public void OnCreate(ref SystemState state)
    {
        currentMode = SimulationRateMode.RealTime;
        state.EntityManager.CreateSingleton(new SimulationRateControllerSettings { Mode = currentMode });
        ChangeSimulationRate(currentMode, state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>());

        state.RequireForUpdate<SimulationRateControllerSettings>();
    }

    public void OnUpdate(ref SystemState state)
    {
        SimulationRateControllerSettings settings = SystemAPI.GetSingleton<SimulationRateControllerSettings>();
        if (settings.Mode == currentMode)
            return;

        currentMode = settings.Mode;
        ChangeSimulationRate(currentMode, state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>());
    }

    private static void ChangeSimulationRate(SimulationRateMode mode, FixedStepSimulationSystemGroup fixedStepGroup)
    {
        switch (mode)
        {
            case SimulationRateMode.Paused:
                Time.timeScale = 0f; // TODO
                // fixedStepGroup.RateManager = new PausedRateManager();
                break;

            case SimulationRateMode.RealTime:
                Time.timeScale = 1f; // TODO
                // fixedStepGroup.RateManager = new RealTimeRateManager(MAX_REAL_TIME_STEP, fixedStepGroup.World.Time.ElapsedTime);
                break;

            case SimulationRateMode.FullSpeed: // TODO: FullSpeedRateManager might be causing Physics issues, need to investigate.
                Time.timeScale = 10f; // TODO
                // fixedStepGroup.RateManager = new FullSpeedRateManager(FIXED_SIMULATION_STEP, FRAME_BUDGET, fixedStepGroup.World.Time.ElapsedTime);
                break;
        }
    }
}
