using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public static class RootPhenotypeEntityBuilder
{
    public static EntityArchetype CreateRootPhenotypeArchetype(ref SystemState state)
    {
        return state.EntityManager.CreateArchetype(
            typeof(PhenotypeGid),
            typeof(LimbCount),
            typeof(LimbStatus),
            typeof(PhenotypeCreatedAt),
            typeof(NeuralGraphRef),
            typeof(EmitterState),
            typeof(PhenotypeBoundingBox)
        );
    }

    public static void CreateRootPhenotypeEntities(ref SystemState state, EntityArchetype rootPhenotypeArchetype, EntityQuery rootPhenotypeCreationRequestQuery, NativeArray<RootPhenotypeEntityCreationRequest> rootPhenotypeCreationRequests, NativeParallelHashMap<ulong, Entity> rootPhenotypeEntityLookup, NativeParallelHashMap<ulong, NeuralGraphRef> neuralGraphLookup, float elapsedTime)
    {
        using NativeArray<Entity> rootPhenotypeCreationRequestEntities = rootPhenotypeCreationRequestQuery.ToEntityArray(Allocator.TempJob);
        using NativeArray<Entity> rootPhenotypeEntities = new(rootPhenotypeCreationRequestEntities.Length, Allocator.TempJob);
        state.EntityManager.CreateEntity(rootPhenotypeArchetype, rootPhenotypeEntities);
        using EntityCommandBuffer ecb = new(Allocator.TempJob);
        SetUpRootPhenotypeEntityJob setUpRootPhenotypeEntityJob = new()
        {
            Ecb = ecb.AsParallelWriter(),
            RequestEntities = rootPhenotypeCreationRequestEntities,
            Requests = rootPhenotypeCreationRequests,
            RootPhenotypeEntities = rootPhenotypeEntities,
            RootPhenotypeEntityLookup = rootPhenotypeEntityLookup.AsParallelWriter(),
            NeuralGraphLookup = neuralGraphLookup.AsParallelWriter(),
            ElapsedTime = elapsedTime
        };
        setUpRootPhenotypeEntityJob.ScheduleParallelByRef(rootPhenotypeCreationRequestEntities.Length, 64, state.Dependency).Complete();
        ecb.Playback(state.EntityManager);
    }

    [BurstCompile]
    private partial struct SetUpRootPhenotypeEntityJob : IJobFor
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public NativeArray<Entity> RequestEntities;
        [ReadOnly] public NativeArray<RootPhenotypeEntityCreationRequest> Requests;
        [ReadOnly] public NativeArray<Entity> RootPhenotypeEntities;
        public NativeParallelHashMap<ulong, Entity>.ParallelWriter RootPhenotypeEntityLookup;
        public NativeParallelHashMap<ulong, NeuralGraphRef>.ParallelWriter NeuralGraphLookup;
        public float ElapsedTime;

        public void Execute(int index)
        {
            RootPhenotypeEntityCreationRequest requestData = Requests[index];
            Entity rootPhenotypeEntity = RootPhenotypeEntities[index];

            // Phenotype GID.
            Ecb.SetComponent(index, rootPhenotypeEntity, new PhenotypeGid { Value = requestData.PhenotypeGid });

            // Limb count.
            Ecb.SetComponent(index, rootPhenotypeEntity, new LimbCount { Value = requestData.LimbCount });

            // Limb statuses buffer.
            DynamicBuffer<LimbStatus> limbStatusBuf = Ecb.AddBuffer<LimbStatus>(index, rootPhenotypeEntity);
            limbStatusBuf.Resize(requestData.LimbCount, NativeArrayOptions.ClearMemory);

            // Creation time.
            Ecb.SetComponent(index, rootPhenotypeEntity, new PhenotypeCreatedAt { Value = ElapsedTime });

            // Neural graph blob reference.
            NeuralGraphRef neuralGraphRef = new() { Value = requestData.Graph };
            Ecb.SetComponent(index, rootPhenotypeEntity, neuralGraphRef);

            // Emitter states buffer.
            DynamicBuffer<EmitterState> emitterStatesBuf = Ecb.AddBuffer<EmitterState>(index, rootPhenotypeEntity);
            int emitterCount = requestData.Graph.Value.TotalEmitterCount;
            emitterStatesBuf.Resize(MultipleOf(emitterCount, 4), NativeArrayOptions.ClearMemory); // SIMD-friendly length.

            // Add to the neural network lookups.
            RootPhenotypeEntityLookup.TryAdd(requestData.PhenotypeGid, rootPhenotypeEntity);
            NeuralGraphLookup.TryAdd(requestData.PhenotypeGid, neuralGraphRef);

            // Destroy the request entity.
            int disposalOffsetIndex = RequestEntities.Length;
            Ecb.DestroyEntity(index + disposalOffsetIndex, RequestEntities[index]);
        }

        private static int MultipleOf(int value, int multiple)
        {
            return (value + multiple - 1) / multiple * multiple;
        }
    }
}
