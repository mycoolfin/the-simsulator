using Unity.Entities;

public struct JointAxisX : IComponentData
{
    public ushort SensorEmitterIndex;
    public ushort ActuatorNeuronEmitterIndex;
    public float AngleLimit;
    public byte SwapXZ;
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
    public byte SwapXZ;
}

public struct JointBreakDistance : IComponentData
{
    public float DistanceSquared;
}
