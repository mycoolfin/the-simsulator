using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Evolution.Assessment
{
    using Core.ECS.Components.Phenotype;
    using Components.Evolution;
    using Components.Phenotype;

    public static class LightFollowingAssessmentUtility
    {
        private const float MaxVolume = 5f * 5f * 5f;

        public static void UpdateFitness<TData>(
            in PhenotypeBoundingBox boundingBox,
            in DynamicBuffer<LimbStatus> limbStatuses,
            ref Fitness fitness,
            ref TData data,
            bool lightSourceMoved,
            float3 lightSourcePosition)
            where TData : unmanaged, ILightFollowingData
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
                data.AccumulatedFitness = 0f;
                data.IsInitialised = true;
            }

            if (lightSourceMoved)
            {
                data.StartPosition = currentPosition;
                data.AccumulatedFitness = fitness.Value;
            }

            float distanceFromStartToLight = math.distance(data.StartPosition, lightSourcePosition);
            float distanceFromCurrentToLight = math.distance(currentPosition, lightSourcePosition);
            float progress = distanceFromStartToLight - distanceFromCurrentToLight;

            float volume = boundingBox.MaxExtents.x * boundingBox.MaxExtents.y * boundingBox.MaxExtents.z;
            float volumePenalty = 1f / math.max(volume - MaxVolume, 1f);

            fitness.Value = data.AccumulatedFitness + progress * volumePenalty;
        }
    }
}
