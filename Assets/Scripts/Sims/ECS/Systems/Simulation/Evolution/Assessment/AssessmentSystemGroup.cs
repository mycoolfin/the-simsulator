using Unity.Burst;
using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Evolution.Assessment
{
    using Core.ECS.Systems.Simulation.Phenotypes;

    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhenotypeSystemGroup))]
    public partial class AssessmentSystemGroup : ComponentSystemGroup
    {
    }
}
