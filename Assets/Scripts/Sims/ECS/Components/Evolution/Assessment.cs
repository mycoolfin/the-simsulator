using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Components.Evolution
{
    public struct Fitness : IComponentData
    {
        public float Value;
    }

    public interface IDistanceData : IComponentData
    {
        bool IsInitialised { get; set; }
        float3 StartPosition { get; set; }
    }

    public struct GroundDistanceAssessmentData : IDistanceData
    {
        public bool IsInitialised { get; set; }
        public float3 StartPosition { get; set; }
    }

    public struct WaterDistanceAssessmentData : IDistanceData
    {
        public bool IsInitialised { get; set; }
        public float3 StartPosition { get; set; }
    }

    public interface ILightFollowingData : IComponentData
    {
        bool IsInitialised { get; set; }
        float3 PreviousPosition { get; set; }
    }

    public struct GroundLightFollowingAssessmentData : ILightFollowingData
    {
        public bool IsInitialised { get; set; }
        public float3 PreviousPosition { get; set; }
    }

    public struct WaterLightFollowingAssessmentData : ILightFollowingData
    {
        public bool IsInitialised { get; set; }
        public float3 PreviousPosition { get; set; }
    }
}
