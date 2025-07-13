using Unity.Burst;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class TrialInitialisationSystemGroup : ComponentSystemGroup
{
}
