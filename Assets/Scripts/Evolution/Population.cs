using System.Collections.Generic;

public class AssessableGenotype
{
    public Genotype genotype;
    public float? fitness;
}

public class Population
{
    public List<AssessableGenotype> assessableGenotypes;

    public Population() { }

    public Population(int populationSize)
    {
        assessableGenotypes = new();
        for (int i = 0; i < populationSize; i++)
            this.assessableGenotypes.Add(new() { genotype = Genotype.CreateRandom() });
    }

    public Population(List<Genotype> genotypes)
    {
        assessableGenotypes = new();
        for (int i = 0; i < genotypes.Count; i++)
            this.assessableGenotypes.Add(new() { genotype = genotypes[i] });
    }
}
