namespace mycoolfin.TheSimsulator.Core.Genotype
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
        /// If lockMorphology is true, mutations that would alter the morphology of the phenotype are disabled.
        /// </summary>
        /// <param name="mutationRate"></param>
        /// <param name="lockMorphology"></param>
        public void Mutate(float mutationRate, bool lockMorphology = false);
    }
}
