using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public enum SpawnType
{
    Surface,
    Underwater,
    All
}

public class Spawner : MonoBehaviour
{
    public string surfaceGenotypesFolder;
    public string underwaterGenotypesFolder;
    public List<Genotype> surfaceGenotypes;
    public List<Genotype> underwaterGenotypes;
    private Queue<Phenotype> queue;
    public float timerDuration = 10f;
    public bool useTimer;
    private float timer;
    public SpawnType spawnType = SpawnType.Surface;
    public float radius = 10f;
    public List<Vector3> spawnLocations = new();
    public int remainingSpawnCount = 15;

    private void Start()
    {
        queue = new();

        foreach (string path in Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, surfaceGenotypesFolder)))
            if (path.EndsWith(".genotype"))
                surfaceGenotypes.Add(GenotypeSerializer.ReadGenotypeFromFile(Path.Combine(Application.streamingAssetsPath, path)));
        surfaceGenotypes = surfaceGenotypes.Where(g => g != null).ToList();

        foreach (string path in Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, underwaterGenotypesFolder)))
            if (path.EndsWith(".genotype"))
                underwaterGenotypes.Add(GenotypeSerializer.ReadGenotypeFromFile(Path.Combine(Application.streamingAssetsPath, path)));
        underwaterGenotypes = underwaterGenotypes.Where(g => g != null).ToList();
    }

    private void FixedUpdate()
    {
        if (!useTimer) return;

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            WorldManager.Instance.EmptyTrashCan();
            if (queue.Count > 20)
                Destroy(queue.Dequeue().gameObject);

            Phenotype p = SpawnRandomCreature();
            if (p != null) queue.Enqueue(p);
            timer = timerDuration;
        }
    }

    public Phenotype SpawnRandomCreature()
    {
        if (remainingSpawnCount <= 0) return null;
        List<Genotype> genotypes = spawnType == SpawnType.Surface ? surfaceGenotypes : spawnType == SpawnType.Underwater ? underwaterGenotypes : surfaceGenotypes.Concat(underwaterGenotypes).ToList();
        if (genotypes.Count == 0 || spawnLocations.Count == 0)
            return null;
        Genotype g = genotypes[Random.Range(0, genotypes.Count)];
        Phenotype p = Phenotype.Construct(g);
        p.transform.position = transform.position + spawnLocations[Random.Range(0, spawnLocations.Count)]
            + Quaternion.Euler(0, Random.Range(0, 360), 0)
            * Vector3.forward * Random.Range(0f, radius);
        p.transform.rotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
        remainingSpawnCount--;
        return p;
    }
}
