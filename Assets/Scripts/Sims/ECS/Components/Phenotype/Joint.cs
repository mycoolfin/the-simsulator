using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Components.Phenotype
{
    public struct JointAngleSensors : IComponentData
    {
        public float3 Angles;
        public float3 AngleLimits;
        public ushort PrimarySensorEmitterIndex;
        public ushort SecondarySensorEmitterIndex;
        public ushort TertiarySensorEmitterIndex;
    }

    public struct JointAngleActuators : IComponentData
    {
        public float3 TargetAngularVelocities;
        public float3 AngleLimits;
        public sbyte PrimaryMotorConstraintIndex;
        public sbyte SecondaryMotorConstraintIndex;
        public sbyte TertiaryMotorConstraintIndex;
        public ushort PrimaryActuatorNeuronEmitterIndex;
        public ushort SecondaryActuatorNeuronEmitterIndex;
        public ushort TertiaryActuatorNeuronEmitterIndex;
    }

    public struct JointBreakDistance : IComponentData
    {
        public float DistanceSquared;
    }
}
