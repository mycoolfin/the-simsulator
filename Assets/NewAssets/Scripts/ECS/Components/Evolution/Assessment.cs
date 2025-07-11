

using Unity.Entities;
using Unity.Mathematics;

public struct Fitness : IComponentData
{
    public float Value;
}

public struct DistanceAssessmentData : IComponentData
{
    public float3 StartPosition;
}
