using Unity.Burst;
using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.Systems.Simulation.Evolution.Assessment
{
    using Systems.Simulation.RootPhenotypes;

    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(RootPhenotypeSystemGroup))]
    public partial class AssessmentSystemGroup : ComponentSystemGroup
    {
    }
}
