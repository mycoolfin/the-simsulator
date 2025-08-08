using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.Systems.Initialisation
{
    using Components.Phenotype;

    public struct DestroyPhenotypeEntitiesRequest : IComponentData
    {
        public ulong PhenotypeGid; // GID of the phenotype to destroy, or 0 for all.
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PhenotypeEntityDestructionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DestroyPhenotypeEntitiesRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Get the request component.
            DestroyPhenotypeEntitiesRequest request = SystemAPI.GetSingleton<DestroyPhenotypeEntitiesRequest>();

            using EntityCommandBuffer ecb = new(Allocator.TempJob);
            if (request.PhenotypeGid == 0)
                DestroyAllPhenotypeEntities(ref state, ecb);
            else
                DestroySpecificChildPhenotypeEntities(ref state, ecb, request.PhenotypeGid);

            // Destroy the request entity itself.
            Entity requestEntity = SystemAPI.GetSingletonEntity<DestroyPhenotypeEntitiesRequest>();
            ecb.DestroyEntity(requestEntity);

            ecb.Playback(state.EntityManager);

            // Request metadata recalculation after destruction.
            if (!SystemAPI.HasSingleton<RecalculatePhenotypeMetadataRequest>())
                state.EntityManager.CreateSingleton<RecalculatePhenotypeMetadataRequest>();
        }

        private void DestroyAllPhenotypeEntities(ref SystemState state, EntityCommandBuffer ecb)
        {
            // Dispose of all BlobAssetReferences before destroying entities.
            new DisposeBlobAssetsJob().ScheduleParallel(state.Dependency).Complete();

            new DestroyAllPhenotypeEntitiesJob
            {
                Ecb = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency).Complete();
        }

        private void DestroySpecificChildPhenotypeEntities(ref SystemState state, EntityCommandBuffer ecb, ulong phenotypeGid)
        {
            // Find the root entity for the specified phenotype GID.
            Entity rootPhenotypeEntity = Entity.Null;

            foreach (var (phenotypeGidComponent, entity) in SystemAPI.Query<RefRO<PhenotypeGid>>().WithEntityAccess())
            {
                if (phenotypeGidComponent.ValueRO.Value == phenotypeGid)
                {
                    rootPhenotypeEntity = entity;
                    break;
                }
            }

            if (rootPhenotypeEntity != Entity.Null)
            {
                // Dispose of blob assets for this specific phenotype.
                if (state.EntityManager.HasComponent<NeuralGraphRef>(rootPhenotypeEntity))
                {
                    NeuralGraphRef neuralGraphRef = state.EntityManager.GetComponentData<NeuralGraphRef>(rootPhenotypeEntity);
                    if (neuralGraphRef.Value.IsCreated)
                        neuralGraphRef.Value.Dispose();
                }

                // Destroy all entities related to this phenotype.
                new DestroySpecificChildPhenotypeEntitiesJob
                {
                    Ecb = ecb.AsParallelWriter(),
                    RootPhenotypeEntity = rootPhenotypeEntity
                }.ScheduleParallel(state.Dependency).Complete();

                // Destroy the root phenotype entity itself.
                ecb.DestroyEntity(rootPhenotypeEntity);
            }
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
    public partial struct DestroyAllPhenotypeEntitiesJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity)
        {
            Ecb.DestroyEntity(chunkIndex, entity);
        }
    }

    [BurstCompile]
    public partial struct DestroySpecificChildPhenotypeEntitiesJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        public Entity RootPhenotypeEntity;

        public void Execute(Entity entity, in RootPhenotypeEntity rootPhenotypeEntity)
        {
            if (rootPhenotypeEntity.Value == RootPhenotypeEntity)
                Ecb.DestroyEntity(0, entity);
        }
    }
}
