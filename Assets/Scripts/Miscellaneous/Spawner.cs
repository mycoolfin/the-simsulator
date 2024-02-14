using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class SpawnLooper : MonoBehaviour
{
    public List<string> genotypePaths;
    public List<Genotype> loadedGenotypes;
    public Queue<Phenotype> queue;
    public float timer;
    public float radius = 10f;

    private void Start()
    {
        queue = new();
        loadedGenotypes = genotypePaths.Select(path =>
            GenotypeSerializer.ReadGenotypeFromFile(Path.Combine(Application.streamingAssetsPath, path))).Where(g => g != null).ToList();
        
        if (loadedGenotypes.Count == 0)
            enabled = false;
    }

    private void FixedUpdate()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            WorldManager.Instance.EmptyTrashCan();
            if (queue.Count > 20)
                Destroy(queue.Dequeue().gameObject);

            Genotype g = loadedGenotypes[Random.Range(0, loadedGenotypes.Count)];
            Phenotype p = Phenotype.Construct(g);
            queue.Enqueue(p);
            p.transform.position = transform.position
                + Quaternion.Euler(0, Random.Range(0, 360), 0)
                * Vector3.forward * Random.Range(0f, radius);
            p.transform.rotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360));
            timer = 10;
        }
    }
}
