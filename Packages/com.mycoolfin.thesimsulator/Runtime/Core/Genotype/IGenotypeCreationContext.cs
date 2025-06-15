namespace mycoolfin.TheSimsulator
{
    public interface IGenotypeCreationContext<TGenotype> where TGenotype : IGenotype<TGenotype>
    {
        /// <summary>
        /// Creates a new genotype instance based on the current context.
        /// </summary>
        /// <returns>A genotype instance.</returns>
        public TGenotype CreateGenotypeFromContext(bool pruneMissingReferences = true);

        /// <summary>
        /// Mutates the genotype creation context in place based on the provided mutation rate.
        /// </summary>
        /// <param name="mutationRate"></param>
        public void Mutate(float mutationRate);
    }
}
