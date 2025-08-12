using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.RootPhenotypes
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial class RootPhenotypeSystemGroup : ComponentSystemGroup
    {
    }
}
