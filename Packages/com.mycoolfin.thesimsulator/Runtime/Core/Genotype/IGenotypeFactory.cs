using System.Collections;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Core.Genotype
{
    public interface IGenotypeFactory<TGenotype> where TGenotype : IGenotype<TGenotype>
    {
        public delegate void OnGenotypeCreatedDelegate(int index, TGenotype genotype);

        /// <summary>
        /// Creates freshly initialised genotype instances.
        /// </summary>
        /// <param name="count">The number of genotype instances to create.</param>
        /// <param name="onCreated">An action to perform on each created genotype instance.</param>
        /// <returns>An enumerator that yields until all genotypes are created.</returns>
        public IEnumerator CreateInitialisedGenotypes(int count, OnGenotypeCreatedDelegate onCreated);

        /// <summary>
        /// Recombines a list of parent genotype pairs into offspring genotypes.
        /// </summary>
        /// <param name="parents">A list of tuples containing pairs of parent genotypes.</param>
        /// <param name="mutationRate">The mutation rate to apply to the offspring.</param>
        /// <param name="onRecombined">An action to perform on each offspring genotype.</param>
        /// <returns>An enumerator that yields until all offspring are created.</returns>
        public IEnumerator Recombine(IReadOnlyList<(TGenotype parent1, TGenotype parent2)> parents, float mutationRate, OnGenotypeCreatedDelegate onRecombined);
    }
}
