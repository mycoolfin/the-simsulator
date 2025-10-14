using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.Systems.Simulation.Phenotypes
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial class PhenotypeSystemGroup : ComponentSystemGroup
    {
    }
}
