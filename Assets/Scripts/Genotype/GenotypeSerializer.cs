using System.IO;
using UnityEngine;

public static class GenotypeSerializer
{
    public static void WriteGenotypeToFile(Genotype genotype, string path)
    {
        string json = JsonUtility.ToJson(genotype, true);
        File.WriteAllText(path, json);
        Debug.Log("Wrote " + genotype.Id + " to " + path);
    }

    public static Genotype ReadGenotypeFromFile(string path)
    {
        string json = File.ReadAllText(path);
        Genotype genotype = JsonUtility.FromJson<Genotype>(json);
        genotype.Validate(); // Ensure all values are valid.
        Debug.Log("Read " + genotype.Id + " from " + path);
        return genotype;
    }
}
