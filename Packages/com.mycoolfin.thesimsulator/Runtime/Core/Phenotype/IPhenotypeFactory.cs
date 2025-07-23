using System.Collections;
using System.Collections.Generic;

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

        public delegate void OnPhenotypeConstructedDelegate(int index, TPhenotype phenotype);

        /// <summary>
        /// Constructs phenotypes from the provided genotypes.
        /// </summary>
        /// <param name="genotypes">A list of genotypes to construct phenotypes from.</param>
        /// <param name="onConstructed">An action to perform on each constructed phenotype instance.</param>
        /// <returns>An enumerator that yields until all phenotypes are constructed.</returns>
        public IEnumerator ConstructPhenotypes(IReadOnlyList<TGenotype> genotypes, OnPhenotypeConstructedDelegate onConstructed);
    }
}
