using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

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
        private const float MovementAreaRadius = 20f;

        private EntityQuery lightSourceQuery;
        private float3 radii;
        private float3 center;
        private float timeSinceLastTargetSelection;
        private float3 lightSourcePosition;
        private float3 lightTargetPosition;
        private float3 lightVelocity;
        private float3 positionAtLastTargetSelection;
        private Random random;

        public void OnCreate(ref SystemState state)
        {
            random = new Random(12345); // TODO: Link to evolution seed.

            state.RequireForUpdate<Fitness>();
            state.RequireForUpdate<WaterLightFollowingAssessmentData>();
            state.RequireForUpdate<LightSourceTag>();

            lightSourceQuery = state.GetEntityQuery(typeof(LightSourceTag));

            // Initialize light at random position within the sphere.
            radii = new(MovementAreaRadius, MovementAreaRadius, MovementAreaRadius);
            center = float3.zero;
            lightSourcePosition = LightMovementUtility.GetRandomPositionInBounds(ref random, radii);
            lightTargetPosition = lightSourcePosition;
            lightVelocity = float3.zero;
            positionAtLastTargetSelection = lightSourcePosition;
            timeSinceLastTargetSelection = 0f;
        }

        public void OnUpdate(ref SystemState state)
        {
            FixedStepSimulationSystemGroup fixedStepGroup = state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            float deltaTime = fixedStepGroup.World.Time.DeltaTime;

            LightMovementUtility.UpdateLightPosition(
                ref state,
                lightSourceQuery,
                ref random,
                ref timeSinceLastTargetSelection,
                ref lightSourcePosition,
                ref lightTargetPosition,
                ref lightVelocity,
                ref positionAtLastTargetSelection,
                deltaTime,
                radii,
                center
            );

            new WaterLightFollowingAssessmentJob
            {
                LightSourcePosition = lightSourcePosition,
            }.ScheduleParallel(state.Dependency).Complete();
        }
    }

    [BurstCompile]
    public partial struct WaterLightFollowingAssessmentJob : IJobEntity
    {
        public float3 LightSourcePosition;

        public void Execute(in PhenotypeBoundingBox boundingBox, in DynamicBuffer<LimbStatus> limbStatuses, ref Fitness fitness, ref WaterLightFollowingAssessmentData data)
        {
            LightFollowingAssessmentUtility.UpdateFitness(
                in boundingBox,
                in limbStatuses,
                ref fitness,
                ref data,
                LightSourcePosition
            );
        }
    }
}
