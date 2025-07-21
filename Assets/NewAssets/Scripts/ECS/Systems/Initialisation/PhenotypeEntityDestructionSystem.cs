using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.Systems.Initialisation
{
    using Components.Phenotype;

    public struct DestroyAllPhenotypeEntitiesRequest : IComponentData { }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PhenotypeEntityDestructionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DestroyAllPhenotypeEntitiesRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Dispose of all BlobAssetReferences before destroying entities.
            new DisposeBlobAssetsJob().ScheduleParallel(state.Dependency).Complete();

            using EntityCommandBuffer ecb = new(Allocator.TempJob);
            new DestroyPhenotypeEntitiesJob
            {
                Ecb = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency).Complete();

            // Destroy the metadata singleton if it exists.
            if (SystemAPI.HasSingleton<PhenotypeEntitiesMetadata>())
                ecb.DestroyEntity(SystemAPI.GetSingletonEntity<PhenotypeEntitiesMetadata>());

            // Destroy the request entity itself.
            Entity requestEntity = SystemAPI.GetSingletonEntity<DestroyAllPhenotypeEntitiesRequest>();
            ecb.DestroyEntity(requestEntity);

            ecb.Playback(state.EntityManager);
        }
    }

    [BurstCompile]
    [WithAll(typeof(NeuralGraphRef))]
    public partial struct DisposeBlobAssetsJob : IJobEntity
    {
        public void Execute(in NeuralGraphRef neuralGraphRef)
        {
            if (neuralGraphRef.Value.IsCreated)
                neuralGraphRef.Value.Dispose();
        }
    }

    [BurstCompile]
    [WithAny(typeof(PhenotypeGid), typeof(RootPhenotypeEntity))]
    public partial struct DestroyPhenotypeEntitiesJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity)
        {
            Ecb.DestroyEntity(chunkIndex, entity);
        }
    }
}
