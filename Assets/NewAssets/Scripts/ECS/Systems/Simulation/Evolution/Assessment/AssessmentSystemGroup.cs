using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(RootPhenotypeSystemGroup))]
public partial class AssessmentSystemGroup : ComponentSystemGroup
{
}
