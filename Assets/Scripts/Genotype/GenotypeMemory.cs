using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public struct RecordedGenotype
{
    public Genotype genotype;
    public DateTime dateTime;
    public string description;
    public Texture2D image;
}

public static class GenotypeMemory
{
    private const int memorySize = 5;

    private static Queue<RecordedGenotype> recentGenotypes = new();
    public static ReadOnlyCollection<RecordedGenotype> RecentGenotypes => recentGenotypes.ToList().AsReadOnly();

    public static void RecordRecentGenotype(Genotype genotype, string description, Texture2D image)
    {
        recentGenotypes.Enqueue(new()
        {
            genotype = genotype,
            dateTime = DateTime.Now,
            description = description,
            image = image
        });
        if (recentGenotypes.Count > memorySize)
            recentGenotypes.Dequeue();
    }
}
