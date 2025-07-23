using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Evolution
{
    using Sims.Genotype;
    using Sims.Evolution;

    using Individual = Core.Evolution.Individual<Sims.Genotype.SimsGenotype, Sims.Phenotype.SimsPhenotype>;
    using TrialType = ECS.Components.Evolution.TrialType;
    using SimulationRateMode = ECS.Systems.Simulation.SimulationRate.SimulationRateMode;

    public struct EvolutionStatistics
    {
        public int Generation;
        public float BestFitness;
        public float AverageFitness;
        public float ElapsedTime;
    }

    public class EvolutionSimulator : MonoBehaviour
    {
        [Header("Evolution Parameters")]
        [SerializeField] private int populationSize = 100;
        [SerializeField] private int maxGenerations = 100;
        public int MaxGenerations => maxGenerations;
        [SerializeField] private float survivalRate = 0.2f;
        public int MaxSurvivors => (int)Mathf.Ceil(populationSize * survivalRate);
        [SerializeField] private float mutationRate = 1f;
        [SerializeField] private float settleSeconds = 5f;
        [SerializeField] private float assessmentSeconds = 10f;
        [SerializeField] private TrialType trialType = TrialType.GroundDistance;
        public TrialType TrialType => trialType;
        [SerializeField] private SimsGenotype seedGenotype;

        // Seeding isn't working yet on the ECS side.
        // [SerializeField] private int simulationSeed = 0;
        // [SerializeField] private bool useSimulationSeed = false;

        [Header("Runtime Status")]
        public bool IsRunning { get; private set; } = false;
        public int CurrentGeneration { get; private set; } = 0;

        public event Action<int> OnGenerationStart;
        public event Action<EvolutionStatistics> OnGenerationComplete;
        public event Action<World> OnEcsWorldCreated;
        public event Action<List<Individual>> OnPopulationEvaluated;
        public event Action OnEvolutionComplete;

        private World ecsWorld;
        private Coroutine evolutionCoroutine;

        private readonly List<EvolutionStatistics> statistics = new();
        public IReadOnlyList<EvolutionStatistics> Statistics => statistics;

        [Header("Speed Control")]
        public SimulationRateMode SimulationRate = SimulationRateMode.FullSpeed;

        [Header("Run?")]
        [SerializeField] bool run = false;

        private void Update()
        {
            if (!IsRunning && run)
            {
                StartEvolution();
            }
            run = false;
        }

        public void StartEvolution()
        {
            if (IsRunning)
            {
                Debug.LogWarning("Evolution is already running!");
                return;
            }

            evolutionCoroutine = StartCoroutine(EvolutionLoop());
        }

        public void StopEvolution()
        {
            if (evolutionCoroutine != null)
            {
                StopCoroutine(evolutionCoroutine);
                evolutionCoroutine = null;
            }

            IsRunning = false;
            ECS.API.WorldManagement.DestroyWorld(ecsWorld);
        }

        private IEnumerator EvolutionLoop()
        {
            Debug.Log("Starting evolution with population size: " + populationSize);

            // Factories...

            SimsEvolutionConfig config = new()
            {
                PopulationSize = populationSize,
                SurvivalRate = survivalRate,
                MutationRate = mutationRate,
            };
            // if (useSimulationSeed) config.Seed = simulationSeed;
            SimsEvolution evolution = new(config, AssessPhenotypesCoroutine);

            statistics.Clear();

            IsRunning = true;
            CurrentGeneration = 1;

            ecsWorld = ECS.API.WorldManagement.CreateWorld("EvolutionWorld");
            OnEcsWorldCreated?.Invoke(ecsWorld);

            while (CurrentGeneration <= maxGenerations && IsRunning)
            {
                Debug.Log($"Starting generation {CurrentGeneration}/{maxGenerations}");
                float startTime = Time.time;
                OnGenerationStart?.Invoke(CurrentGeneration);

                yield return StartCoroutine(evolution.Iterate());

                EvolutionStatistics stats = new()
                {
                    Generation = CurrentGeneration,
                    BestFitness = GetBestFitness(evolution.Population),
                    AverageFitness = GetAverageFitness(evolution.Population),
                    ElapsedTime = Time.time - startTime
                };
                statistics.Add(stats);

                OnGenerationComplete?.Invoke(stats);
                Debug.Log($"Generation {CurrentGeneration} complete. Best fitness: {stats.BestFitness}, Average fitness: {stats.AverageFitness}. Elapsed time: {stats.ElapsedTime:F2} seconds.");

                CurrentGeneration++;
            }

            IsRunning = false;
            ECS.API.WorldManagement.DestroyWorld(ecsWorld);

            OnEvolutionComplete?.Invoke();
        }

        private IEnumerator AssessPhenotypesCoroutine(List<Individual> population)
        {
            // Destroy all existing phenotype entities.
            yield return ECS.API.EntityManagement.DestroyAllPhenotypeEntities(ecsWorld);

            // Create ECS entities from phenotypes.
            yield return ECS.API.EntityManagement.CreateEntitiesFromPhenotypes(
                ecsWorld,
                population
                .Select((individual, i) => new ECS.API.PhenotypeEntityCreationInfo
                {
                    Phenotype = individual.phenotype,
                    PhysicsPositionOffset = System.Numerics.Vector3.Zero,
                    VisualOffset = System.Numerics.Vector3.Zero,
                    AllowInterPhenotypeCollisions = false
                }).ToList()
            );

            // Initialise the trial world.
            yield return ECS.API.Evolution.InitialiseTrial(ecsWorld, trialType, GetSimulationRateMode);

            // Let entities settle.
            yield return ECS.API.Evolution.SettlePhenotypes(ecsWorld, settleSeconds, GetSimulationRateMode);

            // Start assessment.
            yield return ECS.API.Evolution.AssessPhenotypes(ecsWorld, trialType, assessmentSeconds, GetSimulationRateMode);

            // Read back fitness values and assign to matching individuals.
            Dictionary<ulong, float> phenotypeFitnesses = ECS.API.Evolution.GetAssessmentResults(ecsWorld);
            foreach (Individual individual in population)
                if (phenotypeFitnesses.TryGetValue(individual.phenotype.Gid, out float fitness))
                    individual.fitness = Mathf.Max(fitness, 0f);
                else
                    individual.fitness = 0f;
        }

        private SimulationRateMode GetSimulationRateMode()
        {
            return SimulationRate;
        }

        private static float GetAverageFitness(IReadOnlyList<Individual> currentPopulation)
        {
            if (currentPopulation == null || currentPopulation.Count == 0)
                return 0f;

            return currentPopulation.Average(i => i.fitness);
        }

        private static float GetBestFitness(IReadOnlyList<Individual> currentPopulation)
        {
            if (currentPopulation == null || currentPopulation.Count == 0)
                return 0f;

            return currentPopulation.Max(i => i.fitness);
        }

        private void OnDestroy()
        {
            StopEvolution();
        }
    }
}
