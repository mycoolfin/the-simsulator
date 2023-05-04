using System.Linq;
using System.Collections.Generic;

public class Individual
{
    public Genotype genotype;
    public Phenotype phenotype;
    public float fitness = 0f;
    public bool isProtected;
    public bool preProcessingComplete;
}

public class Population
{
    public List<Individual> individuals;

    public Population() { }

    public Population(int populationSize)
    {
        individuals = new();
        for (int i = 0; i < populationSize; i++)
            this.individuals.Add(new() { genotype = Genotype.CreateRandom() });
    }

    public Population(List<Genotype> genotypes)
    {
        individuals = new();
        for (int i = 0; i < genotypes.Count; i++)
            this.individuals.Add(new() { genotype = genotypes[i] });
    }

    public Population(List<Individual> individuals)
    {
        this.individuals = individuals.Select(i => new Individual() { genotype = i.genotype, isProtected = i.isProtected }).ToList();
    }
}
