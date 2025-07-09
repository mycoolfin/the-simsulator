using System.Runtime.InteropServices;
using Unity.Entities;

public struct NeuralNetworkEntityCreationRequest : IComponentData
{
    public ulong PhenotypeGid;
    public BlobAssetReference<CompiledNeuralGraph> Graph;
    public uint LimbCount;
}

public struct NeuralGraphRef : IComponentData
{
    public BlobAssetReference<CompiledNeuralGraph> Value;
}

public struct EmitterState : IBufferElementData
{
    public float Value;
}

public struct LimbStatus : IBufferElementData
{
    [MarshalAs(UnmanagedType.U1)] public bool Detached;
}

public struct NeuralNetworkEntity : IComponentData
{
    public Entity Value;
}
