using Unity.Burst;
using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.RootPhenotypes
{
    using Components.Phenotype;

    [UpdateInGroup(typeof(RootPhenotypeSystemGroup))]
    public partial struct UpdatePhenotypeSimulationTimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RootPhenotypeEntity>();
            state.RequireForUpdate<PhenotypeSimulationTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            FixedStepSimulationSystemGroup fixedStepGroup = state.World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            float deltaTime = fixedStepGroup.World.Time.DeltaTime;

            new UpdatePhenotypeSimulationTimeJob
            {
                DeltaTime = deltaTime
            }.ScheduleParallel(state.Dependency).Complete();
        }
    }

    [BurstCompile]
    public partial struct UpdatePhenotypeSimulationTimeJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref PhenotypeSimulationTime phenotypeSimulationTime)
        {
            phenotypeSimulationTime.Value += DeltaTime;
        }
    }
}
