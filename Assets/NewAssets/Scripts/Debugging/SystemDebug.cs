using Unity.Entities;
using UnityEngine;

public class SystemDebug : MonoBehaviour
{
    private bool previousEnableJointBreakSystem;
    public bool EnableJointBreakSystem = false;

    private void Start()
    {
        SystemSettingsAPI.SetJointBreakSystemEnabled(World.DefaultGameObjectInjectionWorld, EnableJointBreakSystem);
        previousEnableJointBreakSystem = EnableJointBreakSystem;
    }

    private void Update()
    {
        if (EnableJointBreakSystem != previousEnableJointBreakSystem)
        {
            SystemSettingsAPI.SetJointBreakSystemEnabled(World.DefaultGameObjectInjectionWorld, EnableJointBreakSystem);
            previousEnableJointBreakSystem = EnableJointBreakSystem;
        }
    }
}
