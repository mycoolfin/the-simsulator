using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Components.Phenotype
{
    using NeuralNetwork;

    public struct PhenotypeGid : IComponentData
    {
        public ulong Value;
    }

    public struct LimbCount : IComponentData
    {
        public byte Value;
    }

    public enum AttachmentState : byte
    {
        Attached,
        Detached
    }

    public struct LimbStatus : IBufferElementData
    {
        public AttachmentState AttachmentState;
    }

    public struct PhenotypeSimulationTime : IComponentData
    {
        public float Value;
    }

    public struct NeuralGraphRef : IComponentData
    {
        public BlobAssetReference<CompiledNeuralGraph> Value;
    }

    public struct EmitterState : IBufferElementData
    {
        public float Value;
    }

    public struct PhenotypeBoundingBox : IComponentData
    {
        public float3 MinBounds;
        public float3 MaxBounds;
    }
}
