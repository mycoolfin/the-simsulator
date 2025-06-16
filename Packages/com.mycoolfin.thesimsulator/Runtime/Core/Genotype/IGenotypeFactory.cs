namespace mycoolfin.TheSimsulator
{
    public interface IGenotypeFactory<TGenotype> where TGenotype : IGenotype<TGenotype>
    {
        /// <summary>
        /// Creates a freshly initialised genotype instance.
        /// </summary>
        /// <returns>A genotype instance.</returns>
        public TGenotype CreateInitialisedGenotype();

        /// <summary>
        /// Creates a new offspring genotype creation context from two parent genotypes.
        /// </summary>
        /// <param name="parent1">The first parent genotype.</param>
        /// <param name="parent2">The second parent genotype.</param>
        /// <returns>A genotype creation context.</returns>
        public IGenotypeCreationContext<TGenotype> Recombine(TGenotype parent1, TGenotype parent2);
    }
}
