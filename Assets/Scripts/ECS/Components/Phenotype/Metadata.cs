using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.Components.Phenotype
{
    public struct PhenotypeEntitiesMetadata : IComponentData
    {
        public int TotalRootPhenotypeCount;
        public int TotalLimbCount;
    }
}
