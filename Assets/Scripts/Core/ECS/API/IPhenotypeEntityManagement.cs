using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
    using TheSimsulator.Core.Phenotype;

    public struct PhenotypeEntityCreationInfo<TPhenotype>
        where TPhenotype : IPhenotype<TPhenotype>
    {
        public TPhenotype Phenotype;
        public Vector3 PhysicsPositionOffset;
        public bool AllowInterPhenotypeCollisions;
    }

    public interface IPhenotypeEntityManagement<TPhenotype>
        where TPhenotype : IPhenotype<TPhenotype>
    {
        IEnumerator CreateEntitiesFromPhenotypes(World world, List<PhenotypeEntityCreationInfo<TPhenotype>> creationInfoList);

        IEnumerator DestroyAllPhenotypeEntities(World world);
    
        IEnumerator DestroyPhenotypeEntities(World world, TPhenotype phenotype);

        PhenotypeTransformData GetPhenotypeTransformData(World world, TPhenotype phenotype);

        bool TryRaycastToPhenotype(World world, Ray ray, float rayLength, out ulong phenotypeGid);
    }
}
