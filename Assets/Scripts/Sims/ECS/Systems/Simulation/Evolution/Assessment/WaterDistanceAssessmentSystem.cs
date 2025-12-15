using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Evolution.Assessment
{
    using Core.ECS.Components.Phenotype;
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
        private const float MaxVolume = 5f * 5f * 5f;

        public void Execute(in PhenotypeBoundingBox boundingBox, in DynamicBuffer<LimbStatus> limbStatuses, ref Fitness fitness, ref WaterDistanceAssessmentData data)
        {
            float3 currentPosition = boundingBox.Center;

            if (!data.IsInitialised)
            {
                data.StartPosition = currentPosition;
                data.IsInitialised = true;
            }

            float displacement = math.distance(currentPosition, data.StartPosition);

            float volume = boundingBox.MaxExtents.x * boundingBox.MaxExtents.y * boundingBox.MaxExtents.z;
            float volumePenalty = 1f / math.max(volume - MaxVolume, 1f);

            fitness.Value = displacement * volumePenalty;
        }
    }
}
