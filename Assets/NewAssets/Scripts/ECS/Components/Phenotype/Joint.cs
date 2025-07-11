using System.Runtime.InteropServices;
using Unity.Entities;

public struct JointAxisX : IComponentData
{
    public ushort SensorEmitterIndex;
    public ushort ActuatorNeuronEmitterIndex;
    public float AngleLimit;
    [MarshalAs(UnmanagedType.U1)] public bool SwapXZ;
}

public struct JointAxisY : IComponentData
{
    public ushort SensorEmitterIndex;
    public ushort ActuatorNeuronEmitterIndex;
    public float AngleLimit;
}

public struct JointAxisZ : IComponentData
{
    public ushort SensorEmitterIndex;
    public ushort ActuatorNeuronEmitterIndex;
    public float AngleLimit;
    [MarshalAs(UnmanagedType.U1)] public bool SwapXZ;
}
