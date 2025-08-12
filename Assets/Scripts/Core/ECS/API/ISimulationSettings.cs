using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
    using Systems.Simulation.SimulationRate;

    public struct SimulationRateInfo
    {
        public float FrameBudget;
        public float ActualTPS;
    }

    public interface ISimulationSettings
    {
        SimulationRateInfo GetSimulationRateInfo(World world);

        void SetSimulationRateControllerMode(World world, SimulationRateMode mode);
    }
}
