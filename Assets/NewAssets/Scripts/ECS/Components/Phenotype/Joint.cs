using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Mathematics;

public struct JointEntityCreationRequest : IComponentData
{
    public ulong PhenotypeGid;
    public mycoolfin.TheSimsulator.Sims.Genotype.JointType JointType;
    public int ReferenceLimbIndex;
    public int AttachedLimbIndex;
    public float3 ReferenceLimbSpaceAnchor;
    public float3 ReferenceLimbSpaceXAxis;
    public float3 ReferenceLimbSpaceYAxis;
    public float3 ReferenceLimbSpaceZAxis;
    [MarshalAs(UnmanagedType.U1)] public bool FlippedHandedness;
    public float3 AngleLimits;
    public float MaxMotorImpulseScaleFactor;
}

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
