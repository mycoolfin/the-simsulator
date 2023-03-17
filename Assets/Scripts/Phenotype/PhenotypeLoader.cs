using UnityEngine;

public class PhenotypeLoader : MonoBehaviour
{
    public string pathToGenotype;
    public bool loadGenotype;
    public Genotype loadedGenotype;
    private bool isGenotypeLoaded;
    public Vector3 phenotypeSpawnPosition;
    public bool spawnPhenotype;

    private void Update()
    {
        if (loadGenotype)
        {
            loadGenotype = false;
            isGenotypeLoaded = false;
            loadedGenotype = GenotypeSerializer.ReadGenotypeFromFile(pathToGenotype);
            isGenotypeLoaded = true;
        }

        if (spawnPhenotype)
        {
            spawnPhenotype = false;
            if (isGenotypeLoaded)
            {
                Phenotype p = PhenotypeBuilder.ConstructPhenotype(loadedGenotype);
                p.transform.position = phenotypeSpawnPosition;
                Debug.Log("Spawned phenotype from " + loadedGenotype.Id);
            }
        }
    }
}
