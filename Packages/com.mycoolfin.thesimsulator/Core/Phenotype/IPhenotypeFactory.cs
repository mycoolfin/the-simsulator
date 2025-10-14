namespace mycoolfin.TheSimsulator.Core.Phenotype
{
    using Genotype;

    public interface IPhenotypeFactory<TGenotype, TPhenotype>
    where TGenotype : IGenotype<TGenotype>
    where TPhenotype : IPhenotype<TPhenotype>
    {
        /// <summary>
        /// Constructs a phenotype from the provided genotype.
        /// </summary>
        /// <param name="genotype"></param>
        /// <returns>A phenotype instance.</returns>
        public TPhenotype ConstructPhenotype(TGenotype genotype);
    }
}
