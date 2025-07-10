using Unity.Entities;

public struct RootPhenotypeEntityCreationRequest : IComponentData
{
    public ulong PhenotypeGid;
    public BlobAssetReference<CompiledNeuralGraph> Graph;
    public byte LimbCount;
}

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

public struct PhenotypeCreatedAt : IComponentData
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
