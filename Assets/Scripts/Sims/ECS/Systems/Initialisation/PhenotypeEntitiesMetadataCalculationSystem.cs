using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Initialisation
{
    using Core.ECS.Components.Shared;
    using Core.ECS.Components.Phenotype;
    using Components.Phenotype;

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(PhenotypeEntityCreationSystem))]
    [UpdateAfter(typeof(PhenotypeEntityDestructionSystem))]
    public partial struct PhenotypeEntitiesMetadataCalculationSystem : ISystem
    {
        private EntityQuery rootPhenotypeQuery;
        private EntityQuery limbQuery;

        public void OnCreate(ref SystemState state)
        {
            rootPhenotypeQuery = SystemAPI.QueryBuilder()
                .WithAll<PhenotypeGid>()
                .Build();
            limbQuery = SystemAPI.QueryBuilder()
                .WithAll<RootPhenotypeEntity, LimbIndex>()
                .Build();

            state.RequireForUpdate<RecalculatePhenotypeMetadataRequest>();
        }

        public void OnUpdate(ref SystemState state)
        {
            int rootPhenotypeCount = rootPhenotypeQuery.CalculateEntityCount();
            int limbCount = limbQuery.CalculateEntityCount();

            PhenotypeEntitiesMetadata metadata = new()
            {
                TotalRootPhenotypeCount = rootPhenotypeCount,
                TotalLimbCount = limbCount
            };

            SystemAPI.TryGetSingletonEntity<PhenotypeEntitiesMetadata>(out Entity metadataSingleton);
            if (metadataSingleton != Entity.Null)
                state.EntityManager.SetComponentData(metadataSingleton, metadata);
            else
                state.EntityManager.CreateSingleton(metadata);

            Entity requestEntity = SystemAPI.GetSingletonEntity<RecalculatePhenotypeMetadataRequest>();
            state.EntityManager.DestroyEntity(requestEntity);
        }
    }
}
