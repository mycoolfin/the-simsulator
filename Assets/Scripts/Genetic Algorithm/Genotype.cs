using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Genotype
{
    public int[] genes;

    public Genotype(int genomeLength)
    {
        // Randomise genes.
    }

    public Genotype(int[] genes)
    {
        this.genes = genes;
    }
}
