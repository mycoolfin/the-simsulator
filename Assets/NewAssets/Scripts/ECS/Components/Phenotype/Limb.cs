using Unity.Entities;
using Unity.Mathematics;

public struct LimbIndex : IComponentData
{
    public byte Value;
}

public struct ParentLimb : IComponentData
{
    public Entity Value;
}

public struct DetachedLimbTag : IComponentData { }

public struct LimbColor : IComponentData
{
    public float4 Value;
}
