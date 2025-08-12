using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Evolution.Assessment
{
    using Components.Evolution;
    using Components.Phenotype;

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
            float3 currentPosition = (boundingBox.MinBounds + boundingBox.MaxBounds) * 0.5f; // Centroid.

            if (fitness.Value < 0f) // Assessment hasn't started yet.
                data.StartPosition = currentPosition;

            float displacement = math.length(new float3(currentPosition.x - data.StartPosition.x,
                                                            currentPosition.y - data.StartPosition.y,
                                                            currentPosition.z - data.StartPosition.z));

            fitness.Value = displacement;
        }
    }
}
