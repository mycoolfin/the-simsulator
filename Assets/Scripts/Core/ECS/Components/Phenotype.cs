using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.Components.Phenotype
{
    public struct PhenotypeGid : IComponentData
    {
        public ulong Value;
    }

    public struct PhenotypeSimulationTime : IComponentData
    {
        public float Value;
    }

    public struct PhenotypeBoundingBox : IComponentData
    {
        public float3 Center;
        public float3 CurrentExtents;
        public float3 MaxExtents;
    }
}
