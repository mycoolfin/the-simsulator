using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using mycoolfin.TheSimsulator.Sims.Evolution;
using mycoolfin.TheSimsulator.Sims.Genotype;
using mycoolfin.TheSimsulator.Sims.Phenotype;

namespace NewAssets
{
    using Individual = mycoolfin.TheSimsulator.Individual<SimsGenotype, SimsPhenotype>;

    public enum TrialType : byte
    {
        GroundDistance,
        WaterDistance
    };

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
        [SerializeField] private float survivalRate = 0.2f;
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
        [SerializeField] private SimulationRateMode simulationRate = SimulationRateMode.RealTime;
        public SimulationRateMode SimulationRate { get => simulationRate; set => simulationRate = value; }

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
            WorldAPI.DestroyWorld(ecsWorld);
        }

        private IEnumerator EvolutionLoop()
        {
            Debug.Log("Starting evolution with population size: " + populationSize);

            SimsEvolutionConfig config = new()
            {
                PopulationSize = populationSize,
                SurvivalRate = survivalRate,
                MutationRate = mutationRate,
            };
            // if (useSimulationSeed) config.Seed = simulationSeed;
            SimsEvolution evolution = new(AssessPhenotypesCoroutine, config);

            statistics.Clear();

            IsRunning = true;
            CurrentGeneration = 1;

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
            OnEvolutionComplete?.Invoke();
        }

        private IEnumerator AssessPhenotypesCoroutine(List<Individual> population)
        {
            // Completely reset the ECS world.
            WorldAPI.DestroyWorld(ecsWorld);
            ecsWorld = WorldAPI.CreateWorld("EvolutionWorld");
            OnEcsWorldCreated?.Invoke(ecsWorld);

            // Create ECS entities from phenotypes.
            EntityCreationAPI.CreateEntitiesFromPhenotypes(
                ecsWorld,
                population
                .Select((individual, i) => new PhenotypeEntityCreationInfo
                {
                    Phenotype = individual.phenotype,
                    PhysicsPositionOffset = System.Numerics.Vector3.Zero,
                    VisualOffset = System.Numerics.Vector3.Zero,
                    AllowInterPhenotypeCollisions = false
                }).ToList()
            );

            // Initialise trial.
            yield return EvolutionAPI.InitialiseTrial(ecsWorld, trialType, GetSimulationRateMode);

            // Let entities settle.
            yield return EvolutionAPI.SettlePhenotypes(ecsWorld, settleSeconds, GetSimulationRateMode);

            // Start assessment.
            yield return EvolutionAPI.AssessPhenotypes(ecsWorld, trialType, assessmentSeconds, GetSimulationRateMode);

            // Read back fitness values and assign to matching individuals.
            Dictionary<ulong, float> phenotypeFitnesses = EvolutionAPI.GetAssessmentResults(ecsWorld);
            foreach (Individual individual in population)
                if (phenotypeFitnesses.TryGetValue(individual.phenotype.Gid, out float fitness))
                    individual.fitness = Mathf.Max(fitness, 0f);
        }

        private SimulationRateMode GetSimulationRateMode()
        {
            return simulationRate;
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
