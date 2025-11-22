using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Evolution.Assessment
{
    using Core.ECS.Components.Phenotype;
    using Components.Evolution;

    [UpdateInGroup(typeof(AssessmentSystemGroup))]
    [UpdateAfter(typeof(BeginAssessmentSystem))]
    public partial struct WaterDistanceAssessmentSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Fitness>();
            state.RequireForUpdate<WaterDistanceAssessmentData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            new WaterDistanceAssessmentJob
            {
            }.ScheduleParallel(state.Dependency).Complete();
        }
    }

    [BurstCompile]
    public partial struct WaterDistanceAssessmentJob : IJobEntity
    {
        public void Execute(in PhenotypeBoundingBox boundingBox, ref Fitness fitness, ref WaterDistanceAssessmentData data)
        {
            float3 currentPosition = boundingBox.Center;

            if (fitness.Value < 0f) // Assessment hasn't started yet.
                data.StartPosition = currentPosition;

            float displacement = math.distance(currentPosition, data.StartPosition);

            float volume = boundingBox.MaxExtents.x * boundingBox.MaxExtents.y * boundingBox.MaxExtents.z;
            float volumePenalty = math.max(volume, 5f * 5f * 5f);

            fitness.Value = displacement / volumePenalty;
        }
    }
}
