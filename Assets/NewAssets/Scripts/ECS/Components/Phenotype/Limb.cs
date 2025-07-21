using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.Components.Phenotype
{
    public struct LimbIndex : IComponentData
    {
        public byte Value;
    }

    public struct ParentLimb : IComponentData
    {
        public Entity Value;
    }

    public struct DetachedLimbTag : IComponentData { }

    public struct LimbColor : IComponentData
    {
        public float4 Value;
    }
}
