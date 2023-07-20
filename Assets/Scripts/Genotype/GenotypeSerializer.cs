using System.IO;
using UnityEngine;

public static class GenotypeSerializer
{
    public static string WriteGenotypeToFile(Genotype genotype, string path)
    {
        try
        {
            string json = JsonUtility.ToJson(genotype);
            File.WriteAllText(path, json);
            Debug.Log("Wrote " + genotype.Id + " to " + path);
            return path;
        }
        catch
        {
            Debug.Log("Failed to write " + genotype.Id + " to " + path);
            return null;
        }
    }

    public static Genotype ReadGenotypeFromFile(string path)
    {
        Genotype genotype;
        try
        {
            string json = File.ReadAllText(path);
            genotype = JsonUtility.FromJson<Genotype>(json);
        }
        catch
        {
            Debug.Log("Failed to read genotype from " + path);
            return null;
        }
        try
        {
            genotype.Validate(); // Ensure all values are valid.
            Debug.Log("Read " + genotype.Id + " from " + path);
            return genotype;
        }
        catch
        {
            Debug.Log("Validation failed for genotype " + genotype.Id);
            return null;
        }
    }
}
