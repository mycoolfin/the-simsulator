using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Sensors
{
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial class UpdateSensorsSystemGroup : ComponentSystemGroup
    {
    }
}
