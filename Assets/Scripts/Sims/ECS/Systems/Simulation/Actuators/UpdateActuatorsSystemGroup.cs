using Unity.Burst;
using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Actuators
{
    using Neurons;

    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(UpdateNeuronsSystem))]
    public partial class UpdateActuatorsSystemGroup : ComponentSystemGroup
    {
    }
}
