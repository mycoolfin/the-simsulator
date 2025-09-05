using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace mycoolfin.TheSimsulator.Core.Evolution
{
    using Genotype;
    using Phenotype;

    public class SeedGenotype<TGenotype>
        where TGenotype : IGenotype<TGenotype>
    {
        public TGenotype Genotype;
        public SeedGenotype(TGenotype genotype = default)
        {
            Genotype = genotype;
        }
    }

    public abstract class EvolutionConfigBase<TGenotype>
        where TGenotype : IGenotype<TGenotype>
    {
        public int PopulationSize { get; set; } = 100;
        public float SurvivalRate { get; set; } = 0.2f;
        public float MutationRate { get; set; } = 0.1f;
        public SeedGenotype<TGenotype> SeedGenotype { get; set; } = null;
        public bool LockMorphologies { get; set; } = false;
        public int Seed { get; set; } = Environment.TickCount;
    }

    public abstract class EvolutionBase<TEvolutionConfig, TGenotype, TPhenotype, TIndividual> : IDisposable
        where TEvolutionConfig : EvolutionConfigBase<TGenotype>
        where TGenotype : IGenotype<TGenotype>
        where TPhenotype : IPhenotype<TPhenotype>
        where TIndividual : IIndividual<TGenotype, TPhenotype>, new()
    {
        private readonly EvolutionConfigBase<TGenotype> config;

        protected IGenotypeFactory<TGenotype> genotypeFactory;
        protected IPhenotypeFactory<TGenotype, TPhenotype> phenotypeFactory;

        protected List<TIndividual> population;
        public IReadOnlyList<TIndividual> Population => population.AsReadOnly();

        /// <summary>
        /// Delegate for assessing individuals.
        /// It is expected to populate the individual.fitness field.
        /// </summary>
        /// <param name="population">The population of individuals.</param>
        /// <returns>A task that completes when the assessment is done.</returns>
        public delegate Task AssessIndividualsDelegate(List<TIndividual> population, CancellationToken cancellationToken);
        protected AssessIndividualsDelegate AssessIndividuals;

        public int IterationCount { get; private set; } = 0;
        public event Action OnIterationStart;
        public event Action OnAssessmentStart;
        public event Action OnAssessmentEnd;
        public event Action OnIterationEnd;
        private volatile bool isIterating;
        public bool IsIterating => isIterating;

        public float GenotypeCreationProgress { get; private set; }
        public float PhenotypeCreationProgress { get; private set; }

        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly CancellationToken cancellationToken;

        public EvolutionBase(
            TEvolutionConfig config,
            AssessIndividualsDelegate assessIndividualsDelegate,
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

            population = new(config.PopulationSize);

            AssessIndividuals = assessIndividualsDelegate;

            cancellationTokenSource = new();
            cancellationToken = cancellationTokenSource.Token;
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Performs one iteration of the evolution process.
        /// </summary>
        /// <returns>A task that completes when the iteration is done.</returns>
        public async Task IterateAsync()
        {
            isIterating = true;

            // Dispose of old phenotypes.
            foreach (TIndividual individual in population)
                individual.Phenotype?.Dispose();

            // Create new genotypes.
            List<TGenotype> genotypes;
            if (IterationCount == 0) // First iteration - initialise the population.
            {
                genotypes = await CreateInitialisedGenotypesAsync(
                    genotypeFactory,
                    config.PopulationSize,
                    config.SeedGenotype,
                    cancellationToken,
                    new Progress<int>(progress => GenotypeCreationProgress = progress / (float)config.PopulationSize)
                );
            }
            else // Create the next population based on assessed fitnesses.
            {
                int maxSurvivors = (int)Math.Ceiling(config.PopulationSize * config.SurvivalRate);
                List<TIndividual> survivors = SelectSurvivors(population, maxSurvivors);
                int offspringNeeded = config.PopulationSize - survivors.Count;

                genotypes = survivors.Select(s => s.Genotype).ToList();
                if (offspringNeeded > 0)
                {
                    // Choose parent pairs and create offspring.
                    List<(TGenotype, TGenotype)> parentPairs = ChooseParents(survivors, offspringNeeded);
                    genotypes.AddRange(await RecombineAllAsync(
                        genotypeFactory,
                        parentPairs,
                        config.MutationRate,
                        config.LockMorphologies,
                        cancellationToken,
                        new Progress<int>(progress => GenotypeCreationProgress = progress / (float)offspringNeeded)
                    ));

                    // If we need more offspring than we have parent pairs, create additional initialised genotypes.
                    int padCount = offspringNeeded - parentPairs.Count;
                    if (padCount > 0)
                    {
                        List<TGenotype> paddingGenotypes = await CreateInitialisedGenotypesAsync(
                            genotypeFactory,
                            padCount,
                            config.SeedGenotype,
                            cancellationToken,
                            new Progress<int>(progress => GenotypeCreationProgress = (progress + parentPairs.Count) / (float)offspringNeeded)
                        );
                        genotypes.AddRange(paddingGenotypes);
                    }

                    GenotypeCreationProgress = 1f;
                }
            }

            if (genotypes.Count != config.PopulationSize)
                throw new InvalidOperationException($"Expected {config.PopulationSize} genotypes, but got {genotypes.Count}.");

            IterationCount++;
            OnIterationStart?.Invoke();

            // Create new phenotypes.
            List<TPhenotype> phenotypes = await ConstructPhenotypesAsync(
                phenotypeFactory,
                genotypes,
                cancellationToken,
                new Progress<int>(progress => PhenotypeCreationProgress = progress / (float)config.PopulationSize)
            );
            PhenotypeCreationProgress = 1f;

            if (phenotypes.Count != config.PopulationSize)
                throw new InvalidOperationException($"Expected {config.PopulationSize} phenotypes, but got {phenotypes.Count}.");

            // Create new individuals from genotypes and phenotypes.
            population.Clear();
            for (int i = 0; i < config.PopulationSize; i++)
            {
                population.Add(new()
                {
                    Genotype = genotypes[i],
                    Phenotype = phenotypes[i],
                    Fitness = 0
                });
            }

            // Assess the individuals.
            OnAssessmentStart?.Invoke();
            await AssessIndividuals(population, cancellationToken);
            OnAssessmentEnd?.Invoke();

            // Clamp fitnesses above zero.
            foreach (TIndividual individual in population)
                individual.Fitness = Math.Max(0, individual.Fitness);

            OnIterationEnd?.Invoke();

            isIterating = false;
        }

        /// <summary>
        /// Selects survivors from a population based on their fitness.
        /// </summary>
        /// <param name="population"></param>
        /// <param name="maxSurvivors"></param>
        /// <returns>
        /// A list of individuals of maximum length maxSurvivors, ordered by fitness in descending order.
        /// </returns>
        public List<TIndividual> SelectSurvivors(
            List<TIndividual> population,
            int maxSurvivors
        )
        {
            return population
            .Where(x => x.Fitness > 0)
            .OrderByDescending(x => x.Fitness)
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
        protected List<(TGenotype, TGenotype)> ChooseParents(IReadOnlyList<TIndividual> parents, int pairsWanted)
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
                running += parents[k].Fitness;
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
                    double fit1 = p1.Fitness;
                    // Pick in [0, totalFit-fit1).
                    double r = SharedRandom.NextDouble() * (totalFit - fit1);
                    // Translate into original wheel, skipping slice i.
                    double pick = r < cum[i] - fit1 ? r : r + fit1;
                    j = SelectIdx(cum, pick);
                }

                var p2 = parents[j];

                // Order as (more-fit, less-fit).
                if (p1.Fitness >= p2.Fitness)
                    result.Add((p1.Genotype, p2.Genotype));
                else
                    result.Add((p2.Genotype, p1.Genotype));
            }

            return result;
        }

        private static Task<List<TGenotype>> CreateInitialisedGenotypesAsync(IGenotypeFactory<TGenotype> genotypeFactory, int count, SeedGenotype<TGenotype> seedGenotype, CancellationToken token, IProgress<int> progress = null)
        {
            return Task.Run(() =>
            {
                List<TGenotype> genotypes = new(count);

                if (seedGenotype != null) // Use the seed genotype if provided.
                {
                    for (int i = 0; i < count; i++)
                        genotypes.Add(seedGenotype.Genotype);
                }
                else // Use default genotype factory initialiser with parallel execution.
                {
                    TGenotype[] genotypeArray = new TGenotype[count];
                    int completed = 0;

                    Parallel.For(0, count, new ParallelOptions
                    {
                        CancellationToken = token,
                        MaxDegreeOfParallelism = Environment.ProcessorCount
                    }, i =>
                    {
                        genotypeArray[i] = genotypeFactory.CreateInitialisedGenotype();

                        progress?.Report(Interlocked.Increment(ref completed));
                    });

                    genotypes.AddRange(genotypeArray);
                }
                return genotypes;
            });
        }

        private static Task<List<TGenotype>> RecombineAllAsync(IGenotypeFactory<TGenotype> genotypeFactory, IReadOnlyList<(TGenotype parent1, TGenotype parent2)> parents, float mutationRate, bool lockMorphologies, CancellationToken token, IProgress<int> progress = null)
        {
            return Task.Run(() =>
            {
                TGenotype[] offspring = new TGenotype[parents.Count];
                int completed = 0;

                Parallel.For(0, parents.Count, new ParallelOptions
                {
                    CancellationToken = token,
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                }, i =>
                {
                    offspring[i] = genotypeFactory.Recombine(parents[i].parent1, parents[i].parent2, mutationRate, lockMorphologies);

                    progress?.Report(Interlocked.Increment(ref completed));
                });

                return offspring.ToList();
            });
        }

        private static Task<List<TPhenotype>> ConstructPhenotypesAsync(IPhenotypeFactory<TGenotype, TPhenotype> phenotypeFactory, IReadOnlyList<TGenotype> genotypes, CancellationToken token, IProgress<int> progress = null)
        {
            return Task.Run(() =>
            {
                TPhenotype[] phenotypes = new TPhenotype[genotypes.Count];
                int completed = 0;

                Parallel.For(0, genotypes.Count, new ParallelOptions
                {
                    CancellationToken = token,
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                }, i =>
                {
                    token.ThrowIfCancellationRequested();

                    phenotypes[i] = phenotypeFactory.ConstructPhenotype(genotypes[i]);

                    progress?.Report(Interlocked.Increment(ref completed));
                });

                return phenotypes.ToList();
            });
        }
    }
}
