using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Evolution.Assessment
{
    public static class LightMovementUtility
    {
        public const float TargetSelectionInterval = 1f;
        public const float GaussianStdDev = 5f;
        public const float MinConeAngleDegrees = 30f;
        public const float MaxConeAngleDegrees = 180f;
        public const float VelocityForMinCone = GaussianStdDev / TargetSelectionInterval * 2f;

        // Box-Muller transform for gaussian random numbers.
        public static float GaussianRandom(ref Random random)
        {
            float u1 = random.NextFloat();
            float u2 = random.NextFloat();
            return math.sqrt(-2f * math.log(u1)) * math.cos(2f * math.PI * u2);
        }

        // Calculate cone angle based on velocity.
        public static float CalculateConeAngleRadians(float3 velocity)
        {
            float speed = math.length(velocity);
            float t = math.clamp(speed / VelocityForMinCone, 0f, 1f);
            float coneAngleDegrees = math.lerp(MaxConeAngleDegrees, MinConeAngleDegrees, t);
            return math.radians(coneAngleDegrees);
        }

        // Get random direction in 3D (sphere) - constrained by radii dimensions.
        public static float3 GetRandomDirection3D(ref Random random, float3 radii)
        {
            float theta = random.NextFloat(0f, math.PI * 2f);
            float phi = math.acos(random.NextFloat(-1f, 1f));
            float3 direction = new(
                math.sin(phi) * math.cos(theta),
                math.sin(phi) * math.sin(theta),
                math.cos(phi)
            );
            
            // Scale by radii (0 radius naturally cancels out that axis).
            direction *= radii;
            float length = math.length(direction);
            return length > 0.001f ? direction / length : new float3(1f, 0f, 0f);
        }

        // Generate a random position within bounds (spheroid/ellipsoid).
        public static float3 GetRandomPositionInBounds(ref Random random, float3 radii, float3 center = default)
        {
            float theta = random.NextFloat(0f, math.PI * 2f);
            float phi = math.acos(random.NextFloat(-1f, 1f));
            float r = random.NextFloat(0f, 1f);
            
            return center + new float3(
                r * radii.x * math.sin(phi) * math.cos(theta),
                r * radii.y * math.sin(phi) * math.sin(theta),
                r * radii.z * math.cos(phi)
            );
        }

        // Generate direction within cone in 3D (works for 2D if constrained to plane).
        public static float3 GenerateDirectionInCone(
            float3 forwardDirection,
            float deviationAngle,
            float azimuthalAngle
        )
        {
            // Create orthonormal basis with forwardDirection as Z axis.
            float3 arbitraryVector = math.abs(forwardDirection.y) < 0.9f ?
                new float3(0f, 1f, 0f) : new float3(1f, 0f, 0f);
            float3 tangent1 = math.normalize(math.cross(forwardDirection, arbitraryVector));
            float3 tangent2 = math.cross(forwardDirection, tangent1);

            // Construct direction within cone.
            return forwardDirection * math.cos(deviationAngle) +
                (tangent1 * math.cos(azimuthalAngle) + tangent2 * math.sin(azimuthalAngle)) * math.sin(deviationAngle);
        }

        // Clamp position to bounds (ellipsoid/spheroid).
        public static float3 ClampToBounds(float3 position, float3 radii, float3 center = default)
        {
            float3 offset = position - center;
            
            // Normalize by radii to get position in unit sphere space.
            float3 normalized = new(
                radii.x > 0.001f ? offset.x / radii.x : 0f,
                radii.y > 0.001f ? offset.y / radii.y : 0f,
                radii.z > 0.001f ? offset.z / radii.z : 0f
            );
            
            float distanceSquared = math.lengthsq(normalized);
            
            if (distanceSquared > 1f)
            {
                float distance = math.sqrt(distanceSquared);
                normalized /= distance;
                
                // Scale back by radii
                offset = new float3(
                    normalized.x * radii.x,
                    normalized.y * radii.y,
                    normalized.z * radii.z
                );
            }
            
            return center + offset;
        }

        // Select new target position with momentum-based movement.
        public static float3 SelectNewTargetPosition(
            ref Random random,
            float3 currentPosition,
            float3 velocity,
            float3 radii,
            float3 center
        )
        {
            float speed = math.length(velocity);
            float coneAngleRadians = CalculateConeAngleRadians(velocity);

            float3 forwardDirection = speed > 0.1f
                ? math.normalize(velocity)
                : GetRandomDirection3D(ref random, radii);

            // Generate a direction within the cone.
            float deviationAngle = random.NextFloat(0f, coneAngleRadians);
            float azimuthalAngle = random.NextFloat(0f, math.PI * 2f);

            float3 targetDirection = GenerateDirectionInCone(
                forwardDirection, deviationAngle, azimuthalAngle);

            // Scale by radii (0 radius naturally cancels out that axis).
            targetDirection *= radii;
            float length = math.length(targetDirection);
            if (length > 0.001f)
                targetDirection /= length;

            // Generate distance using gaussian.
            float distance = math.abs(GaussianRandom(ref random)) * GaussianStdDev;
            distance = math.clamp(distance, 1f, GaussianStdDev * 2f);

            // Calculate new position and clamp to area.
            float3 newPosition = currentPosition + targetDirection * distance;
            return ClampToBounds(newPosition, radii, center);
        }

        // Update light position with momentum-based movement.
        public static void UpdateLightPosition(
            ref SystemState state,
            in EntityQuery lightSourceQuery,
            ref Random random,
            ref float timeSinceLastTargetSelection,
            ref float3 lightSourcePosition,
            ref float3 lightTargetPosition,
            ref float3 lightVelocity,
            ref float3 positionAtLastTargetSelection,
            in float deltaTime,
            in float3 radii,
            in float3 center
        )
        {
            Entity lightSourceEntity = lightSourceQuery.GetSingletonEntity();

            // Select new target position every interval.
            timeSinceLastTargetSelection += deltaTime;
            if (timeSinceLastTargetSelection >= TargetSelectionInterval)
            {
                // Calculate velocity based on displacement since last target selection.
                float3 displacement = lightSourcePosition - positionAtLastTargetSelection;
                lightVelocity = displacement / TargetSelectionInterval;

                // Select new target and reset tracking.
                positionAtLastTargetSelection = lightSourcePosition;
                lightTargetPosition = SelectNewTargetPosition(
                    ref random, lightSourcePosition, lightVelocity, radii, center);
                timeSinceLastTargetSelection = 0f;
            }

            // Smoothly move light toward target position.
            // Speed is calculated to reach target exactly at the end of the interval.
            float3 direction = lightTargetPosition - lightSourcePosition;
            float distanceToTarget = math.length(direction);

            if (distanceToTarget > 0.01f)
            {
                float remainingTime = TargetSelectionInterval - timeSinceLastTargetSelection;
                if (remainingTime > 0.01f)
                {
                    float requiredSpeed = distanceToTarget / remainingTime;
                    float moveDistance = requiredSpeed * deltaTime;
                    lightSourcePosition += (direction / distanceToTarget) * moveDistance;
                }
                else
                {
                    lightSourcePosition = lightTargetPosition;
                }
            }

            // Update the actual light entity position.
            state.EntityManager.SetComponentData(lightSourceEntity, new LocalTransform
            {
                Position = lightSourcePosition,
                Rotation = quaternion.identity,
                Scale = 1f
            });
        }
    }
}
