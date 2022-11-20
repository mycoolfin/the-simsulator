public class Population
{
    public int size;
    public Genotype[] genotypes;
    public float[] fitnesses;

    public Population(int populationSize)
    {
        this.size = populationSize;
        this.genotypes = new Genotype[populationSize];
        this.fitnesses = new float[populationSize];
        for (int i = 0; i < populationSize; i++)
            this.genotypes[i] = Genotype.CreateRandom();
    }

    public Population(Genotype[] genotypes)
    {
        this.size = genotypes.Length;
        this.genotypes = genotypes;
        this.fitnesses = new float[genotypes.Length];
    }
}
