using Unity.Entities;
using Unity.Mathematics;

public struct Fitness : IComponentData
{
    public float Value;
}

public struct GroundDistanceAssessmentData : IComponentData
{
    public float3 StartPosition;
}

public struct WaterDistanceAssessmentData : IComponentData
{
    public float3 StartPosition;
}
