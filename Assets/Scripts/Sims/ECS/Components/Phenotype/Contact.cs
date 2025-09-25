using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Components.Phenotype
{
    public struct ContactSensors : IComponentData
    {
        public float TotalLoad;
        public ushort TotalLoadSensorEmitterIndex;

        public float Slip;
        public ushort SlipSensorEmitterIndex;

        public float3 DirectionalLoad;
        public ushort XAxisLoadSensorEmitterIndex;
        public ushort YAxisLoadSensorEmitterIndex;
        public ushort ZAxisLoadSensorEmitterIndex;
    }
}
