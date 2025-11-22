using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Components.Phenotype
{
    public struct LightSensors : IComponentData
    {
        public float3 Intensities;
        public ushort XAxisSensorEmitterIndex;
        public ushort YAxisSensorEmitterIndex;
        public ushort ZAxisSensorEmitterIndex;
    }
}
