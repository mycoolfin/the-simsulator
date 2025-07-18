using Unity.Entities;

public enum SimulationRateMode : byte
{
    Paused,
    RealTime,
    FullSpeed,
    MaximumOverdrive
}

public struct SimulationRateControllerSettings : IComponentData
{
    public SimulationRateMode Mode;
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct SimulationRateControllerSystem : ISystem
{
    const double FIXED_TIMESTEP = 1.0 / 60.0;
    private const float REAL_TIME_FRAME_BUDGET = 1f / 90f; // 90 FPS.
    private const float FULL_SPEED_FRAME_BUDGET = 1f / 90f; // 90 FPS.
    private const float MAXIMUM_OVERDRIVE_FRAME_BUDGET = 1f / 10f; // 10 FPS.

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
                    fixedStepGroup.RateManager = new FrameBudgetRateManager(FIXED_TIMESTEP, REAL_TIME_FRAME_BUDGET, 1 / FIXED_TIMESTEP);
                    fixedStepGroup.Enabled = true;
                    break;
                case SimulationRateMode.FullSpeed:
                    fixedStepGroup.RateManager = new FrameBudgetRateManager(FIXED_TIMESTEP, FULL_SPEED_FRAME_BUDGET);
                    fixedStepGroup.Enabled = true;
                    break;
                case SimulationRateMode.MaximumOverdrive:
                    fixedStepGroup.RateManager = new FrameBudgetRateManager(FIXED_TIMESTEP, MAXIMUM_OVERDRIVE_FRAME_BUDGET);
                    fixedStepGroup.Enabled = true;
                    break;
            }
        }
    }
}
