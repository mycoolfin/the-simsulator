using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Components.Phenotype
{
    public struct PhenotypeEntitiesMetadata : IComponentData
    {
        public int TotalRootPhenotypeCount;
        public int TotalLimbCount;
    }
}
