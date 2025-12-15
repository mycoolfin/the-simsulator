using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Components.Evolution
{
    public struct Fitness : IComponentData
    {
        public float Value;
    }

    public interface IAssessmentData : IComponentData
    {
        bool IsInitialised { get; set; }
        float3 StartPosition { get; set; }
    }

    public struct GroundDistanceAssessmentData : IAssessmentData
    {
        public bool IsInitialised { get; set; }
        public float3 StartPosition { get; set; }
    }

    public struct WaterDistanceAssessmentData : IAssessmentData
    {
        public bool IsInitialised { get; set; }
        public float3 StartPosition { get; set; }
    }

    public interface ILightFollowingData : IAssessmentData
    {
        float AccumulatedFitness { get; set; }
    }

    public struct GroundLightFollowingAssessmentData : ILightFollowingData
    {
        public bool IsInitialised { get; set; }
        public float3 StartPosition { get; set; }
        public float AccumulatedFitness { get; set; }
    }

    public struct WaterLightFollowingAssessmentData : ILightFollowingData
    {
        public bool IsInitialised { get; set; }
        public float3 StartPosition { get; set; }
        public float AccumulatedFitness { get; set; }
    }
}
