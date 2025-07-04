using System.Collections.Generic;
using UnityEngine;
using mycoolfin.TheSimsulator.Sims;

public class RandomPhenotypeSpawner : MonoBehaviour
{
    public int SpawnCount;
    public bool Spawn = false;

    private static readonly SimsGenotypeFactory genotypeFactory = new(0.4f, 0.3f, 0.3f, 2);
    private static readonly SimsPhenotypeFactory phenotypeFactory = new();

    private void Update()
    {
        if (Spawn)
        {
            List<SimsPhenotype> phenotypes = new();
            for (int i = 0; i < SpawnCount; i++)
            {
                SimsGenotype genotype = genotypeFactory.CreateInitialisedGenotype();
                SimsPhenotype phenotype = phenotypeFactory.ConstructPhenotype(genotype);
                phenotypes.Add(phenotype);
            }
            EntityCreationAPI.CreateEntitiesFromPhenotypes(phenotypes.ConvertAll(p => new PhenotypeEntityCreationInfo
            {
                Phenotype = p,
                PhysicsPositionOffset = new System.Numerics.Vector3(0, 0, 0),
                PhysicsWorldIndex = 0
            }));
            Spawn = false;
        }
    }
}
