using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Mathematics;

public struct LimbEntityCreationRequest : IComponentData
{
    public ulong PhenotypeGid;
    public byte LimbIndex;
    public float3 Position;
    public quaternion Rotation;
    public float3 Dimensions;
    public float Mass;
    public float4 Color;
    public float3 VisualOffset;
    [MarshalAs(UnmanagedType.U1)] public bool AllowInterPhenotypeCollisions;
}

public struct LimbIndex : IComponentData
{
    public byte Value;
}

public struct DetachedLimbTag : IComponentData { }
