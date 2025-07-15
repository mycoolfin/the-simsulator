using Unity.Entities;

public enum SimulationRateMode : byte
{
    Paused,
    RealTime,
    FullSpeed60FPS,
    FullSpeed10FPS
}

public struct SimulationRateControllerSettings : IComponentData
{
    public SimulationRateMode Mode;
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct SimulationRateControllerSystem : ISystem
{
    const double FIXED_TIMESTEP = 1.0 / 60.0;
    private const float REAL_TIME_FRAME_BUDGET = 1f / 60f; // 60 FPS.
    private const float FULL_SPEED_60FPS_FRAME_BUDGET = 1f / 60f; // 60 FPS.
    private const float FULL_SPEED_10FPS_FRAME_BUDGET = 1f / 10f; // 10 FPS.

    private SimulationRateMode currentMode;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationRateControllerSettings>();
    }

    public void OnUpdate(ref SystemState state)
    {
        FixedStepSimulationSystemGroup fixedStepGroup = state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
        SimulationRateControllerSettings settings = SystemAPI.GetSingleton<SimulationRateControllerSettings>();

        if (settings.Mode != currentMode)
        {
            currentMode = settings.Mode;

            switch (currentMode)
            {
                case SimulationRateMode.Paused:
                    fixedStepGroup.Enabled = false;
                    break;
                case SimulationRateMode.RealTime:
                    // Cap to 60 Hz to match the fixed timestep rate
                    fixedStepGroup.RateManager = new FrameBudgetRateManager(FIXED_TIMESTEP, REAL_TIME_FRAME_BUDGET, 1 / FIXED_TIMESTEP);
                    fixedStepGroup.Enabled = true;
                    break;
                case SimulationRateMode.FullSpeed60FPS:
                    fixedStepGroup.RateManager = new FrameBudgetRateManager(FIXED_TIMESTEP, FULL_SPEED_60FPS_FRAME_BUDGET);
                    fixedStepGroup.Enabled = true;
                    break;
                case SimulationRateMode.FullSpeed10FPS:
                    fixedStepGroup.RateManager = new FrameBudgetRateManager(FIXED_TIMESTEP, FULL_SPEED_10FPS_FRAME_BUDGET);
                    fixedStepGroup.Enabled = true;
                    break;
            }
        }
    }
}
