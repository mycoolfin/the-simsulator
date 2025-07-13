using Unity.Entities;

public struct LimbIndex : IComponentData
{
    public byte Value;
}

public struct ParentLimb : IComponentData
{
    public Entity Value;
}

public struct DetachedLimbTag : IComponentData { }
