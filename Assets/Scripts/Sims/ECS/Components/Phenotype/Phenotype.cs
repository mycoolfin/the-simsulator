using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Components.Phenotype
{
    using NeuralNetwork;

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

    public struct NeuralGraphRef : IComponentData
    {
        public BlobAssetReference<CompiledNeuralGraph> Value;
    }

    public struct EmitterState : IBufferElementData
    {
        public float Value;
    }
}
