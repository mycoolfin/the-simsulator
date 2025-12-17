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
    public partial struct GroundLightFollowingAssessmentSystem : ISystem
    {
        private const float MovementAreaRadius = 20f;
        private const float LightVerticalOffset = 2f;

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
            state.RequireForUpdate<GroundLightFollowingAssessmentData>();
            state.RequireForUpdate<LightSourceTag>();

            // Initialize light at random position within the area (flat disc on XZ plane).
            radii = new(MovementAreaRadius, 0f, MovementAreaRadius);
            center = new(0f, LightVerticalOffset, 0f);
            lightSourcePosition = LightMovementUtility.GetRandomPositionInBounds(ref random, radii, center);
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

            new GroundLightFollowingAssessmentJob
            {
                LightSourcePosition = lightSourcePosition
            }.ScheduleParallel(state.Dependency).Complete();
        }
    }

    [BurstCompile]
    public partial struct GroundLightFollowingAssessmentJob : IJobEntity
    {
        public float3 LightSourcePosition;

        public void Execute(in PhenotypeBoundingBox boundingBox, in DynamicBuffer<LimbStatus> limbStatuses, ref Fitness fitness, ref GroundLightFollowingAssessmentData data)
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
