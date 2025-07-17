using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Entities.Serialization;
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
        [Header("Environment SubScenes")]
        [SerializeField] private EntitySceneReference groundEnvironment;
        [SerializeField] private EntitySceneReference waterEnvironment;

        [Header("Presentation")]
        [SerializeField] private WorldContainer worldContainer;

        [Header("Evolution Parameters")]
        [SerializeField] private int populationSize = 100;
        [SerializeField] private int maxGenerations = 100;
        [SerializeField] private float survivalRate = 0.2f;
        [SerializeField] private float mutationRate = 1f;
        [SerializeField] private float settleSeconds = 5f;
        [SerializeField] private float assessmentSeconds = 10f;
        [SerializeField] private TrialType trialType = TrialType.GroundDistance;
        [SerializeField] private SimsGenotype seedGenotype;

        [Header("Runtime Status")]
        [SerializeField] private bool isRunning = false;
        [SerializeField] private int currentGeneration = 0;

        public event Action<int> OnGenerationStart;
        public event Action<EvolutionStatistics> OnGenerationComplete;
        public event Action<List<Individual>> OnPopulationEvaluated;
        public event Action OnEvolutionComplete;

        private World ecsWorld;
        private Coroutine evolutionCoroutine;

        private readonly List<EvolutionStatistics> statistics = new();
        public IReadOnlyList<EvolutionStatistics> Statistics => statistics;

        [Header("Speed Control")]
        [SerializeField] private SimulationRateMode simulationRate = SimulationRateMode.RealTime;

        [Header("Run?")]
        [SerializeField] bool run = false;

        private void Update()
        {
            if (!isRunning && run)
            {
                StartEvolution();
            }
            run = false;

            if (isRunning)
            {
                UpdatePresentation();
            }
        }

        public void StartEvolution()
        {
            if (isRunning)
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

            isRunning = false;
            WorldAPI.DestroyWorld(ecsWorld);
        }

        private IEnumerator EvolutionLoop()
        {
            Debug.Log("Starting evolution with population size: " + populationSize);

            SimsEvolution evolution = new(
                AssessPhenotypesCoroutine,
                new SimsEvolutionConfig
                {
                    PopulationSize = populationSize,
                    SurvivalRate = survivalRate,
                    MutationRate = mutationRate
                }
            );

            statistics.Clear();

            isRunning = true;
            currentGeneration = 1;

            while (currentGeneration <= maxGenerations && isRunning)
            {
                Debug.Log($"Starting generation {currentGeneration}/{maxGenerations}");
                float startTime = Time.time;
                OnGenerationStart?.Invoke(currentGeneration);

                yield return StartCoroutine(evolution.Iterate());

                EvolutionStatistics stats = new()
                {
                    Generation = currentGeneration,
                    BestFitness = GetBestFitness(evolution.Population),
                    AverageFitness = GetAverageFitness(evolution.Population),
                    ElapsedTime = Time.time - startTime
                };
                statistics.Add(stats);

                OnGenerationComplete?.Invoke(stats);
                Debug.Log($"Generation {currentGeneration} complete. Best fitness: {stats.BestFitness}, Average fitness: {stats.AverageFitness}. Elapsed time: {stats.ElapsedTime:F2} seconds.");

                currentGeneration++;
            }

            isRunning = false;
            OnEvolutionComplete?.Invoke();
        }

        private IEnumerator AssessPhenotypesCoroutine(List<Individual> population)
        {
            // Completely reset the ECS world.
            // TODO: Overkill?
            WorldAPI.DestroyWorld(ecsWorld);
            ecsWorld = WorldAPI.CreateWorld("EvolutionWorld");
            UpdatePresentation(initialise: true);

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
            yield return EvolutionAPI.InitialiseTrial(ecsWorld, trialType, groundEnvironment, waterEnvironment, GetSimulationRateMode);

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

        private void UpdatePresentation(bool initialise = false)
        {
            if (worldContainer == null) return;

            if (initialise)
            {
                switch (trialType)
                {
                    case TrialType.GroundDistance:
                        worldContainer.SetGroundEnabled(true);
                        worldContainer.SetWaterEnabled(false);
                        break;
                    case TrialType.WaterDistance:
                        worldContainer.SetGroundEnabled(false);
                        worldContainer.SetWaterEnabled(true);
                        break;
                }
            }

            if (isRunning)
            {
                float frequency = simulationRate switch
                {
                    SimulationRateMode.Paused => 0f,
                    SimulationRateMode.RealTime => 5f,
                    SimulationRateMode.FullSpeed60FPS => 20f,
                    SimulationRateMode.FullSpeed10FPS => 30f,
                    _ => 1f,
                };
                float fluxFactor = 0.8f + 0.2f * Mathf.Sin(frequency * Time.time);
                float maxIntensity = 20f;
                worldContainer.SetEmitterIntensities(maxIntensity * fluxFactor);
            }

            if (ecsWorld != null && ecsWorld.IsCreated && (initialise || worldContainer.HasChanged))
                {
                    SystemSettingsAPI.SetWorldVisualOffset(ecsWorld, worldContainer);
                    worldContainer.HasChanged = false;
                }
        }

        private void OnDestroy()
        {
            StopEvolution();
        }
    }
}
