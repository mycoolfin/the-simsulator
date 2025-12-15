using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Evolution.Assessment
{
    using Core.ECS.Components.Phenotype;
    using Core.ECS.Components.WorldObject;
    using Components.Evolution;
    using Components.Phenotype;

    [UpdateInGroup(typeof(AssessmentSystemGroup))]
    [UpdateAfter(typeof(BeginAssessmentSystem))]
    public partial struct WaterLightFollowingAssessmentSystem : ISystem
    {
        private const float SphereInnerRadius = 2f;
        private const float SphereOuterRadius = 20f;
        private const float LightMoveIntervalSecondsMin = 4f;
        private const float LightMoveIntervalSecondsMax = 10f;
        private float timeSinceLastMove;
        private float3 lightSourcePosition;
        private Random random;
        private float LightMoveIntervalSeconds;
        private float GetNewLightMoveInterval() => random.NextFloat(LightMoveIntervalSecondsMin, LightMoveIntervalSecondsMax);

        public void OnCreate(ref SystemState state)
        {
            random = new Random(12345); // TODO: Link to evolution seed.

            state.RequireForUpdate<Fitness>();
            state.RequireForUpdate<WaterLightFollowingAssessmentData>();
            state.RequireForUpdate<LightSourceTag>();

            LightMoveIntervalSeconds = GetNewLightMoveInterval();
            timeSinceLastMove = LightMoveIntervalSeconds + float.Epsilon; // Force immediate move on first update.
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
                LightMoveIntervalSeconds = GetNewLightMoveInterval();
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

        public void Execute(in PhenotypeBoundingBox boundingBox, in DynamicBuffer<LimbStatus> limbStatuses, ref Fitness fitness, ref WaterLightFollowingAssessmentData data)
        {
            LightFollowingAssessmentUtility.UpdateFitness(
                in boundingBox,
                in limbStatuses,
                ref fitness,
                ref data,
                LightSourceMoved,
                LightSourcePosition);
        }
    }
}
