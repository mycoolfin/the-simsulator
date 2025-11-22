using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Evolution.Assessment
{
    using Core.ECS.Components.Phenotype;
    using Core.ECS.Components.WorldObject;
    using Components.Evolution;
    using Unity.Transforms;

    [UpdateInGroup(typeof(AssessmentSystemGroup))]
    [UpdateAfter(typeof(BeginAssessmentSystem))]
    public partial struct WaterLightFollowingAssessmentSystem : ISystem
    {
        private const float SphereInnerRadius = 2f;
        private const float SphereOuterRadius = 20f;
        private const float LightMoveIntervalSeconds = 4f;
        private float timeSinceLastMove;
        private float3 lightSourcePosition;
        private Random random;

        public void OnCreate(ref SystemState state)
        {
            random = new Random(12345); // TODO: Link to evolution seed.

            state.RequireForUpdate<Fitness>();
            state.RequireForUpdate<WaterLightFollowingAssessmentData>();
            state.RequireForUpdate<LightSourceTag>();

            timeSinceLastMove = LightMoveIntervalSeconds; // Force immediate move on first update.
        }

        public void OnUpdate(ref SystemState state)
        {
            bool moved = MoveLightSourceIfNeeded(ref state);

            new WaterLightFollowingAssessmentJob
            {
                LightSourceMoved = moved,
                LightSourcePosition = lightSourcePosition
            }.ScheduleParallel(state.Dependency).Complete();
        }

        private bool MoveLightSourceIfNeeded(ref SystemState state)
        {
            FixedStepSimulationSystemGroup fixedStepGroup = state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            Entity lightSourceEntity = SystemAPI.GetSingletonEntity<LightSourceTag>();

            // Move light source to new random position in hollow sphere after every interval.
            bool moved = false;
            timeSinceLastMove += fixedStepGroup.World.Time.DeltaTime;
            if (timeSinceLastMove >= LightMoveIntervalSeconds)
            {
                // Sample random point in hollow sphere (spherical shell) using spherical coordinates.
                float theta = random.NextFloat(0f, math.PI * 2f);  // Azimuthal angle.
                float phi = math.acos(random.NextFloat(-1f, 1f));  // Polar angle (uniform distribution).
                float radius = random.NextFloat(SphereInnerRadius, SphereOuterRadius);  // Random radius in shell.
                
                float x = radius * math.sin(phi) * math.cos(theta);
                float y = radius * math.sin(phi) * math.sin(theta);
                float z = radius * math.cos(phi);
                
                float3 newPosition = new(x, y, z);
                state.EntityManager.SetComponentData(lightSourceEntity, new LocalTransform
                {
                    Position = newPosition,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
                timeSinceLastMove = 0f;
                lightSourcePosition = newPosition;
                moved = true;
            }

            return moved;
        }
    }

    [BurstCompile]
    public partial struct WaterLightFollowingAssessmentJob : IJobEntity
    {
        public bool LightSourceMoved;
        public float3 LightSourcePosition;

        public void Execute(in PhenotypeBoundingBox boundingBox, ref Fitness fitness, ref WaterLightFollowingAssessmentData data)
        {
            float3 currentPosition = boundingBox.Center;

            if (LightSourceMoved)
            {
                data.StartPosition = currentPosition;
                data.AccumulatedFitness = fitness.Value;
            }

            float distanceFromStartToLight = math.distance(data.StartPosition, LightSourcePosition);
            float distanceFromCurrentToLight = math.distance(currentPosition, LightSourcePosition);
            float progress =  distanceFromStartToLight - distanceFromCurrentToLight;

            float volume = boundingBox.MaxExtents.x * boundingBox.MaxExtents.y * boundingBox.MaxExtents.z;
            float volumePenalty = math.max(volume, 5f * 5f * 5f);

            fitness.Value = math.max(data.AccumulatedFitness + progress, 0f) / volumePenalty;
        }
    }
}
