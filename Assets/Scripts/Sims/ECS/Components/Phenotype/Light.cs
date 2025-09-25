using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Components.Phenotype
{
    public struct LightSensors : IComponentData
    {
        public ushort XAxisSensorEmitterIndex;
        public ushort YAxisSensorEmitterIndex;
        public ushort ZAxisSensorEmitterIndex;
    }
}
