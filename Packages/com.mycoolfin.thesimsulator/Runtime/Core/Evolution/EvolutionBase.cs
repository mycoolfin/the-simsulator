using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace mycoolfin.TheSimsulator.Core.Evolution
{
    using Genotype;
    using Phenotype;

    public abstract class EvolutionConfigBase
    {
        public int PopulationSize { get; set; } = 100;
        public float SurvivalRate { get; set; } = 0.2f;
        public float MutationRate { get; set; } = 0.1f;
        public int Seed { get; set; } = Environment.TickCount;
    }

    public abstract class EvolutionBase<TEvolutionConfig, TGenotype, TPhenotype>
        where TEvolutionConfig : EvolutionConfigBase
        where TGenotype : IGenotype<TGenotype>
        where TPhenotype : IPhenotype<TPhenotype>
    {
        private readonly TEvolutionConfig config;

        protected IGenotypeFactory<TGenotype> genotypeFactory;
        protected IPhenotypeFactory<TGenotype, TPhenotype> phenotypeFactory;

        /// <summary>
        /// Delegate for assessing phenotypes.
        /// It is expected to populate the individual.fitness field.
        /// </summary>
        /// <param name="population">The population of individuals.</param>
        /// <returns>An enumerator for coroutine support.</returns>
        public delegate IEnumerator AssessPhenotypesDelegate(List<Individual<TGenotype, TPhenotype>> population);
        protected AssessPhenotypesDelegate AssessPhenotypes;

        protected List<Individual<TGenotype, TPhenotype>> population;
        public IReadOnlyList<Individual<TGenotype, TPhenotype>> Population => population.AsReadOnly();

        protected int maxSurvivors;

        protected float mutationRate;

        public int IterationCount { get; private set; } = 0;
        public event Action OnIterationStart;
        public event Action OnAssessmentStart;
        public event Action OnAssessmentEnd;
        public event Action OnIterationEnd;
        public bool IsIterating { get; private set; }

        public EvolutionBase(
            TEvolutionConfig config,
            AssessPhenotypesDelegate assessPhenotypesDelegate,
            IGenotypeFactory<TGenotype> genotypeFactory,
            IPhenotypeFactory<TGenotype, TPhenotype> phenotypeFactory
        )
        {
            SharedRandom.Reset();
            SharedRandom.Seed(config.Seed);

            this.config = config;

            if (config.PopulationSize <= 0)
                throw new ArgumentException("Population size must be greater than zero.");

            this.genotypeFactory = genotypeFactory;
            this.phenotypeFactory = phenotypeFactory;

            if (genotypeFactory == null)
                throw new ArgumentNullException(nameof(genotypeFactory));
            if (phenotypeFactory == null)
                throw new ArgumentNullException(nameof(phenotypeFactory));

            AssessPhenotypes = assessPhenotypesDelegate;
            maxSurvivors = (int)Math.Ceiling(config.PopulationSize * config.SurvivalRate);
            mutationRate = config.MutationRate;

            population = new();
        }

        /// <summary>
        /// Performs one iteration of the evolution process.
        /// </summary>
        public IEnumerator Iterate()
        {
            IsIterating = true;

            // Dispose of old phenotypes.
            population.ForEach(individual => individual.phenotype?.Dispose());

            if (IterationCount == 0) // First iteration - initialise the population.
            {
                population = new();
                yield return genotypeFactory.CreateInitialisedGenotypes(config.PopulationSize, (_, newGenotype) =>
                {
                    population.Add(new Individual<TGenotype, TPhenotype>
                    {
                        genotype = newGenotype,
                        phenotype = default,
                        fitness = 0
                    });
                });
            }
            else // Create the next population based on assessed fitnesses.
            {
                List<Individual<TGenotype, TPhenotype>> survivors = SelectSurvivors(population, maxSurvivors);
                int offspringNeeded = config.PopulationSize - survivors.Count;

                // Choose parent pairs and create offspring.
                List<(TGenotype, TGenotype)> parentPairs = ChooseParents(survivors, offspringNeeded);
                Individual<TGenotype, TPhenotype>[] offspring = new Individual<TGenotype, TPhenotype>[parentPairs.Count];
                yield return genotypeFactory.Recombine(parentPairs, mutationRate, (index, newGenotype) =>
                {
                    offspring[index] = new()
                    {
                        genotype = newGenotype,
                        phenotype = default,
                        fitness = 0
                    };
                });

                // If we need more offspring than we have parent pairs, create additional initialised genotypes.
                int padCount = offspringNeeded - parentPairs.Count;
                yield return genotypeFactory.CreateInitialisedGenotypes(padCount, (index, newGenotype) =>
                {
                    offspring[parentPairs.Count + index] = new()
                    {
                        genotype = newGenotype,
                        phenotype = default,
                        fitness = 0
                    };
                });

                population = survivors.Select(s => new Individual<TGenotype, TPhenotype>
                {
                    genotype = s.genotype,
                    phenotype = default,
                    fitness = 0
                }).Concat(offspring).ToList();
            }

            IterationCount++;
            OnIterationStart?.Invoke();

            // Create new phenotypes.
            yield return phenotypeFactory.ConstructPhenotypes(population.Select(individual => individual.genotype).ToList(), (index, phenotype) =>
            {
                population[index].phenotype = phenotype;
            });

            // Assess the phenotypes.
            OnAssessmentStart?.Invoke();
            yield return AssessPhenotypes(population);
            OnAssessmentEnd?.Invoke();

            // Clamp fitnesses above zero.
            foreach (Individual<TGenotype, TPhenotype> individual in population)
                individual.fitness = Math.Max(0, individual.fitness);

            OnIterationEnd?.Invoke();

            IsIterating = false;
        }

        /// <summary>
        /// Selects survivors from a population based on their fitness.
        /// </summary>
        /// <param name="population"></param>
        /// <param name="maxSurvivors"></param>
        /// <param name="includeZeroFitness"></param>
        /// <returns>
        /// A list of individuals of maximum length maxSurvivors, ordered by fitness in descending order.
        /// </returns>
        public List<Individual<TGenotype, TPhenotype>> SelectSurvivors(
            List<Individual<TGenotype, TPhenotype>> population,
            int maxSurvivors,
            bool includeZeroFitness = false
        )
        {
            return population
            .Where(x => includeZeroFitness || x.fitness > 0)
            .OrderByDescending(x => x.fitness)
            .Take(maxSurvivors)
            .ToList();
        }

        /// <summary>
        /// Chooses parent pairs from a list of parents using a weighted roulette wheel selection based on fitness. 
        /// </summary>
        /// <param name="parents"></param>
        /// <param name="pairsWanted"></param>
        /// <returns>
        /// A list of tuples containing the genotypes of the selected parent pairs.
        /// </returns> 
        protected List<(TGenotype, TGenotype)> ChooseParents(IReadOnlyList<Individual<TGenotype, TPhenotype>> parents, int pairsWanted)
        {
            if (parents == null)
                throw new ArgumentNullException(nameof(parents));
            if (parents.Count < 2) // At least two parents are required to form pairs.
                return new();

            var result = new List<(TGenotype, TGenotype)>(pairsWanted);
            int n = parents.Count;
            if (n == 0 || pairsWanted <= 0) return result;

            // 1. Build cumulative wheel.
            double[] cum = new double[n];
            double running = 0.0;
            for (int k = 0; k < n; ++k)
            {
                running += parents[k].fitness;
                cum[k] = running;
            }
            double totalFit = running;

            // Helper: binary-search cumulative array for pick in [0, totalFit).
            static int SelectIdx(double[] c, double pick)
            {
                int lo = 0, hi = c.Length - 1;
                while (lo < hi)
                {
                    int mid = (lo + hi) >> 1;
                    if (pick < c[mid]) hi = mid;
                    else lo = mid + 1;
                }
                return lo;
            }

            bool useUniform = totalFit <= 0.0; // Uniform probabilities when all zero fitness.

            // 2. Produce parent pairs.
            while (result.Count < pairsWanted)
            {
                // Draw parent #1.
                int i = useUniform
                          ? SharedRandom.Next(n)
                          : SelectIdx(cum, SharedRandom.NextDouble() * totalFit);

                var p1 = parents[i];

                // Draw parent #2 from residual wheel.
                int j;
                if (useUniform)
                {
                    // Uniform, but ensure j ≠ i.
                    do { j = SharedRandom.Next(n); } while (j == i);
                }
                else
                {
                    double fit1 = p1.fitness;
                    // Pick in [0, totalFit-fit1).
                    double r = SharedRandom.NextDouble() * (totalFit - fit1);
                    // Translate into original wheel, skipping slice i.
                    double pick = r < cum[i] - fit1 ? r : r + fit1;
                    j = SelectIdx(cum, pick);
                }

                var p2 = parents[j];

                // Order as (more-fit, less-fit).
                if (p1.fitness >= p2.fitness)
                    result.Add((p1.genotype, p2.genotype));
                else
                    result.Add((p2.genotype, p1.genotype));
            }

            return result;
        }
    }
}
