using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public static class NeuralNetworkEntityBuilder
{
    public static EntityArchetype CreateNeuralNetworkArchetype(ref SystemState state)
    {
        return state.EntityManager.CreateArchetype(
            typeof(PhenotypeGid),
            typeof(NeuralGraphRef),
            typeof(EmitterState),
            typeof(LimbStatus)
        );
    }

    public static void CreateNeuralNetworkEntities(ref SystemState state, EntityArchetype neuralNetworkArchetype, EntityQuery neuralNetworkCreationRequestQuery, NativeArray<NeuralNetworkEntityCreationRequest> neuralNetworkCreationRequests, NativeParallelHashMap<ulong, NeuralGraphRef> neuralGraphLookup)
    {
        using NativeArray<Entity> neuralNetworkCreationRequestEntities = neuralNetworkCreationRequestQuery.ToEntityArray(Allocator.TempJob);
        using NativeArray<Entity> neuralNetworkEntities = new(neuralNetworkCreationRequestEntities.Length, Allocator.TempJob);
        state.EntityManager.CreateEntity(neuralNetworkArchetype, neuralNetworkEntities);
        using EntityCommandBuffer ecb = new(Allocator.TempJob);
        SetUpNeuralNetworkEntityJob setUpNeuralNetworkEntityJob = new()
        {
            Ecb = ecb.AsParallelWriter(),
            RequestEntities = neuralNetworkCreationRequestEntities,
            Requests = neuralNetworkCreationRequests,
            NeuralNetworkEntities = neuralNetworkEntities,
            NeuralGraphLookup = neuralGraphLookup.AsParallelWriter()
        };
        setUpNeuralNetworkEntityJob.ScheduleParallelByRef(neuralNetworkCreationRequestEntities.Length, 64, state.Dependency).Complete();
        ecb.Playback(state.EntityManager);
    }

    [BurstCompile]
    private partial struct SetUpNeuralNetworkEntityJob : IJobFor
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public NativeArray<Entity> RequestEntities;
        [ReadOnly] public NativeArray<NeuralNetworkEntityCreationRequest> Requests;
        [ReadOnly] public NativeArray<Entity> NeuralNetworkEntities;
        public NativeParallelHashMap<ulong, NeuralGraphRef>.ParallelWriter NeuralGraphLookup;

        private const int INSTANTIATION_KEY = 1;
        private const int DISPOSAL_KEY = 2;

        public void Execute(int index)
        {
            Ecb.DestroyEntity(DISPOSAL_KEY, RequestEntities[index]);

            NeuralNetworkEntityCreationRequest requestData = Requests[index];
            Entity neuralNetworkEntity = NeuralNetworkEntities[index];

            // ID.
            Ecb.SetComponent(INSTANTIATION_KEY, neuralNetworkEntity, new PhenotypeGid { Value = requestData.PhenotypeGid });

            // Neural graph blob reference.
            NeuralGraphRef neuralGraphRef = new() { Value = requestData.Graph };
            Ecb.SetComponent(INSTANTIATION_KEY, neuralNetworkEntity, neuralGraphRef);

            // Emitter states buffer.
            DynamicBuffer<EmitterState> buf = Ecb.AddBuffer<EmitterState>(INSTANTIATION_KEY, neuralNetworkEntity);
            int emitterCount = requestData.Graph.Value.SensorCount + requestData.Graph.Value.NeuronCount;
            buf.Resize(MultipleOf(emitterCount, 4), NativeArrayOptions.ClearMemory);

            // Limb statuses buffer.
            DynamicBuffer<LimbStatus> limbStatusBuf = Ecb.AddBuffer<LimbStatus>(INSTANTIATION_KEY, neuralNetworkEntity);
            limbStatusBuf.Resize(MultipleOf((int)requestData.LimbCount, 4), NativeArrayOptions.ClearMemory);

            // Add to the neural network entity lookup.
            NeuralGraphLookup.TryAdd(requestData.PhenotypeGid, neuralGraphRef);
        }

        private static int MultipleOf(int value, int multiple)
        {
            return (value + multiple - 1) / multiple * multiple;
        }
    }
}
