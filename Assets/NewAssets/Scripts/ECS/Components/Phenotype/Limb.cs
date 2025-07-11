using Unity.Entities;

public struct LimbIndex : IComponentData
{
    public byte Value;
}

public struct DetachedLimbTag : IComponentData { }
