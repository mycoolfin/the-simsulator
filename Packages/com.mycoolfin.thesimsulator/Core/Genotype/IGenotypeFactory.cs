namespace mycoolfin.TheSimsulator.Core.Genotype
{
    public interface IGenotypeFactory<TGenotype> where TGenotype : IGenotype<TGenotype>
    {
        /// <summary>
        /// Creates a freshly-initialised genotype instance.
        /// </summary>
        /// <returns>A new genotype instance.</returns>
        public TGenotype CreateInitialisedGenotype();

        /// <summary>
        /// Recombines two parent genotypes to produce a new offspring genotype.
        /// </summary>
        /// <param name="parent1">The first parent genotype.</param>
        /// <param name="parent2">The second parent genotype.</param>
        /// <param name="mutationRate">The mutation rate to apply to the offspring.</param>
        /// <returns>A new offspring genotype.</returns>
        public TGenotype Recombine(TGenotype parent1, TGenotype parent2, float mutationRate);
    }
}
