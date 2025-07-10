using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(UpdateNeuronsSystem))]
public partial class UpdateActuatorsSystemGroup : ComponentSystemGroup
{
}
