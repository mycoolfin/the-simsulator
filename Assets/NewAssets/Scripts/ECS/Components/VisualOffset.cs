using Unity.Entities;
using Unity.Mathematics;

// Add this component to visually offset creatures without affecting physics.
public struct VisualOffset : IComponentData
{
    public float3 Offset;
}
