using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Components.Phenotype
{
    public struct ContactSensors : IComponentData
    {
        public float TotalLoad;
        public float Slip;
        public float3 DirectionalLoad;
        public ushort TotalLoadSensorEmitterIndex;
        public ushort SlipSensorEmitterIndex;
        public ushort XAxisLoadSensorEmitterIndex;
        public ushort YAxisLoadSensorEmitterIndex;
        public ushort ZAxisLoadSensorEmitterIndex;
    }
}
