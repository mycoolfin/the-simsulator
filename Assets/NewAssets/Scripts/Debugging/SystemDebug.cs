using Unity.Entities;
using UnityEngine;

public class SystemDebug : MonoBehaviour
{
    private SimulationRateMode previousSimulationSpeedMode;
    public SimulationRateMode SimulationSpeedMode = SimulationRateMode.RealTime;

    private bool previousEnableJointBreakSystem;
    public bool EnableJointBreakSystem = false;

    private void Start()
    {
        SystemSettingsAPI.SetSimulationSpeedMode(World.DefaultGameObjectInjectionWorld, SimulationSpeedMode);

        SystemSettingsAPI.SetJointBreakSystemEnabled(World.DefaultGameObjectInjectionWorld, EnableJointBreakSystem);
        previousEnableJointBreakSystem = EnableJointBreakSystem;
    }

    private void Update()
    {
        if (SimulationSpeedMode != previousSimulationSpeedMode)
        {
            SystemSettingsAPI.SetSimulationSpeedMode(World.DefaultGameObjectInjectionWorld, SimulationSpeedMode);
            previousSimulationSpeedMode = SimulationSpeedMode;
        }

        if (EnableJointBreakSystem != previousEnableJointBreakSystem)
        {
            SystemSettingsAPI.SetJointBreakSystemEnabled(World.DefaultGameObjectInjectionWorld, EnableJointBreakSystem);
            previousEnableJointBreakSystem = EnableJointBreakSystem;
        }
    }
}
