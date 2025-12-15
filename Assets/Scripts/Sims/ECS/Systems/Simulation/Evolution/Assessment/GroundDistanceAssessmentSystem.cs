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
    public partial struct GroundDistanceAssessmentSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Fitness>();
            state.RequireForUpdate<GroundDistanceAssessmentData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            new GroundDistanceAssessmentJob
            {
            }.ScheduleParallel(state.Dependency).Complete();
        }
    }

    [BurstCompile]
    public partial struct GroundDistanceAssessmentJob : IJobEntity
    {
        private const float MaxVolume = 5f * 5f * 5f;

        public void Execute(in PhenotypeBoundingBox boundingBox, in DynamicBuffer<LimbStatus> limbStatuses, ref Fitness fitness, ref GroundDistanceAssessmentData data)
        {
            // If any limb is detached set fitness to 0.
            for (int i = 0; i < limbStatuses.Length; i++)
            {
                if (limbStatuses[i].AttachmentState == AttachmentState.Detached)
                {
                    fitness.Value = 0f;
                    return;
                }
            }

            float3 currentPosition = boundingBox.Center;

            if (!data.IsInitialised)
            {
                data.StartPosition = currentPosition;
                data.IsInitialised = true;
            }

            if (currentPosition.y < 0f)
            {
                // If the entity is below ground level, we consider it a failure.
                fitness.Value = 0f;
                return;
            }

            float xzDisplacement = math.length(new float2(currentPosition.x - data.StartPosition.x,
                                                            currentPosition.z - data.StartPosition.z));
            
            float volume = boundingBox.MaxExtents.x * boundingBox.MaxExtents.y * boundingBox.MaxExtents.z;
            float volumePenalty = 1f / math.max(volume - MaxVolume, 1f);

            fitness.Value = xzDisplacement * volumePenalty;
        }
    }
}
