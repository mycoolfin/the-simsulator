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
            float3 lightSourcePosition
        )
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
                data.PreviousPosition = currentPosition;
                data.IsInitialised = true;
                return; // Skip first frame, no displacement to measure yet.
            }

            // Calculate displacement (movement) this frame.
            float3 displacement = currentPosition - data.PreviousPosition;
            
            // Calculate desired direction toward light
            float3 toLightDirection = lightSourcePosition - currentPosition;
            float distanceToLight = math.length(toLightDirection);
            
            if (distanceToLight > 0.01f)
            {
                toLightDirection /= distanceToLight; // Normalize.
                
                // Reward movement aligned with direction to light.
                float displacementTowardLight = math.dot(displacement, toLightDirection);
                
                float volume = boundingBox.MaxExtents.x * boundingBox.MaxExtents.y * boundingBox.MaxExtents.z;
                float volumePenalty = 1f / math.max(volume - MaxVolume, 1f);

                // Positive fitness for moving toward light, negative for moving away.
                fitness.Value += displacementTowardLight * volumePenalty;
            }

            // Store current position for next frame.
            data.PreviousPosition = currentPosition;
        }
    }
}
