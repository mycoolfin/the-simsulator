using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Components.Phenotype
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

    public struct NodeGid : IComponentData
    {
        public ulong Value;
    }

    public struct SelectedLimbTag : IComponentData { }

    [MaterialProperty("_OutlineStrength")]
    public struct LimbOutline : IComponentData
    {
        public float Value;
    }
}
