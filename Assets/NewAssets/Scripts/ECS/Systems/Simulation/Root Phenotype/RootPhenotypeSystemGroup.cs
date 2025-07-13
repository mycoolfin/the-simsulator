using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial class RootPhenotypeSystemGroup : ComponentSystemGroup
{
}
