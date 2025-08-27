using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Initialisation
{
    using Core.ECS.Math;
    using Components.Phenotype;

    public struct DestroyPhenotypeEntitiesRequest : IComponentData
    {
        /// <summary>
        /// GID of the phenotype to destroy, or 0 for all.
        /// </summary>
        public ulong PhenotypeGid;
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PhenotypeEntityDestructionSystem : ISystem
    {
        private NativeList<ulong> phenotypeGidsToDestroy;

        public void OnCreate(ref SystemState state)
        {
            phenotypeGidsToDestroy = new NativeList<ulong>(Allocator.Persistent);

            state.RequireForUpdate<DestroyPhenotypeEntitiesRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            using EntityCommandBuffer ecb = new(Allocator.TempJob);

            if (!SystemAPI.HasSingleton<PhenotypeEntitiesMetadata>())
            {
                // No metadata - just destroy the requests.
                foreach (var (request, requestEntity) in SystemAPI.Query<DestroyPhenotypeEntitiesRequest>().WithEntityAccess())
                {
                    ecb.DestroyEntity(requestEntity);
                }

                ecb.Playback(state.EntityManager);
            }
            else
            {
                PhenotypeEntitiesMetadata metadata = SystemAPI.GetSingleton<PhenotypeEntitiesMetadata>();
                int requiredCapacity = metadata.TotalRootPhenotypeCount.NextPowerOfTwo();
                if (requiredCapacity > phenotypeGidsToDestroy.Capacity)
                {
                    phenotypeGidsToDestroy.Dispose();
                    phenotypeGidsToDestroy = new(requiredCapacity, Allocator.Persistent);
                }
                phenotypeGidsToDestroy.Clear();

                bool destroyAll = false;
                foreach (var (request, requestEntity) in SystemAPI.Query<DestroyPhenotypeEntitiesRequest>().WithEntityAccess())
                {
                    ecb.DestroyEntity(requestEntity);

                    if (request.PhenotypeGid == 0)
                        destroyAll = true;

                    if (!destroyAll)
                        phenotypeGidsToDestroy.Add(request.PhenotypeGid);
                }

                if (destroyAll)
                    DestroyAllPhenotypeEntities(ref state, ecb);
                else
                    DestroySpecificChildPhenotypeEntities(ref state, ecb, phenotypeGidsToDestroy);

                ecb.Playback(state.EntityManager);

                // Request metadata recalculation after destruction.
                if (!SystemAPI.HasSingleton<RecalculatePhenotypeMetadataRequest>())
                    state.EntityManager.CreateSingleton<RecalculatePhenotypeMetadataRequest>();
            }
        }

        public void OnDestroy(ref SystemState state)
        {
            if (phenotypeGidsToDestroy.IsCreated)
                phenotypeGidsToDestroy.Dispose();
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

        private void DestroySpecificChildPhenotypeEntities(ref SystemState state, EntityCommandBuffer ecb, NativeList<ulong> phenotypeGids)
        {
            // Find the root entity for the specified phenotype GIDs.
            foreach (var (phenotypeGidComponent, entity) in SystemAPI.Query<RefRO<PhenotypeGid>>().WithEntityAccess())
            {
                if (phenotypeGids.Contains(phenotypeGidComponent.ValueRO.Value))
                {
                    Entity rootPhenotypeEntity = entity;

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

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, in RootPhenotypeEntity rootPhenotypeEntity)
        {
            if (rootPhenotypeEntity.Value == RootPhenotypeEntity)
                Ecb.DestroyEntity(chunkIndex, entity);
        }
    }
}
