using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Entities;
using mycoolfin.TheSimsulator.Sims.Evolution;
using mycoolfin.TheSimsulator.Sims.Genotype;
using mycoolfin.TheSimsulator.Sims.Phenotype;

namespace NewAssets
{
    using Individual = mycoolfin.TheSimsulator.Individual<SimsGenotype, SimsPhenotype>;

    public enum TrialType : byte
    {
        GroundDistance
    };

    public class EvolutionSimulatorConfig
    {
        public int PopulationSize;
        public int MaxGenerations;
        public float SurvivalRate;
        public float MutationRate;
        public float SettleTime;
        public float AssessmentTime;
        public TrialType TrialType;
        public SimsGenotype SeedGenotype;
    }

    public class EvolutionSimulator : MonoBehaviour
    {
        [Header("Evolution Parameters")]
        [SerializeField] private int populationSize = 100;
        [SerializeField] private int maxGenerations = 100;
        [SerializeField] private float survivalRate = 0.3f;
        [SerializeField] private float mutationRate = 0.1f;
        [SerializeField] private float settleTime = 5f;
        [SerializeField] private float assessmentTime = 10f;
        [SerializeField] private TrialType trialType = TrialType.GroundDistance;
        [SerializeField] private SimsGenotype seedGenotype;

        [Header("Runtime Status")]
        [SerializeField] private bool isRunning = false;
        [SerializeField] private int currentGeneration = 0;
        [SerializeField] private float currentGenerationProgress = 0f;

        public event Action<int> OnGenerationStart;
        public event Action<int, float, float> OnGenerationComplete;
        public event Action<List<Individual>> OnPopulationEvaluated;
        public event Action OnEvolutionComplete;

        private World ecsWorld;
        private Coroutine evolutionCoroutine;

        public bool IsRunning => isRunning;
        public int CurrentGeneration => currentGeneration;
        public float CurrentGenerationProgress => currentGenerationProgress;

        [Header("Run?")]
        [SerializeField] bool run = false;

        private void Awake()
        {
            ecsWorld = World.DefaultGameObjectInjectionWorld;
        }

        private void Update()
        {
            if (!isRunning && run)
            {
                EvolutionSimulatorConfig config = new()
                {
                    PopulationSize = populationSize,
                    MaxGenerations = maxGenerations,
                    SurvivalRate = survivalRate,
                    MutationRate = mutationRate,
                    SettleTime = settleTime,
                    AssessmentTime = assessmentTime,
                    TrialType = trialType,
                    SeedGenotype = seedGenotype
                };
                StartEvolution(config);
            }
            run = false;
        }

        public void StartEvolution(EvolutionSimulatorConfig config)
        {
            if (isRunning)
            {
                Debug.LogWarning("Evolution is already running!");
                return;
            }

            evolutionCoroutine = StartCoroutine(EvolutionLoop(config));
        }

        public void StopEvolution()
        {
            if (evolutionCoroutine != null)
            {
                StopCoroutine(evolutionCoroutine);
                evolutionCoroutine = null;
            }

            isRunning = false;
            EntityCreationAPI.DestroyAllPhenotypeEntities(ecsWorld);
        }

        private IEnumerator EvolutionLoop(EvolutionSimulatorConfig config)
        {
            Debug.Log("Starting evolution with population size: " + config.PopulationSize);

            SimsEvolution evolution = new(
                (population) => AssessPhenotypesCoroutine(population, ecsWorld, config.SettleTime, config.AssessmentTime, config.TrialType),
                new SimsEvolutionConfig
                {
                    PopulationSize = config.PopulationSize,
                    SurvivalRate = config.SurvivalRate,
                    MutationRate = config.MutationRate
                }
            );

            isRunning = true;
            currentGeneration = 0;

            while (currentGeneration < config.MaxGenerations && isRunning)
            {
                Debug.Log($"Starting generation {currentGeneration + 1}/{config.MaxGenerations}");
                OnGenerationStart?.Invoke(currentGeneration);

                yield return StartCoroutine(evolution.Iterate());

                currentGeneration++;

                float bestFitness = GetBestFitness(evolution.Population);
                float averageFitness = GetAverageFitness(evolution.Population);
                OnGenerationComplete?.Invoke(currentGeneration, bestFitness, averageFitness);
                Debug.Log($"Generation {currentGeneration} complete. Best fitness: {bestFitness}, Average fitness: {averageFitness}");
            }

            isRunning = false;
            OnEvolutionComplete?.Invoke();
        }

        private static IEnumerator AssessPhenotypesCoroutine(List<Individual> population, World ecsWorld, float settleTime, float assessmentTime, TrialType trialType)
        {
            EvolutionAPI.FreezeSimulationTime(ecsWorld);

            // Step 1: Create ECS entities from phenotypes.
            Debug.Log("Creating entities from phenotypes...");
            EntityCreationAPI.CreateEntitiesFromPhenotypes(
                ecsWorld,
                population
                .Select(individual => new PhenotypeEntityCreationInfo
                {
                    Phenotype = individual.phenotype,
                    VisualOffset = System.Numerics.Vector3.Zero,
                    AllowInterPhenotypeCollisions = false
                }).ToList()
            );

            // Step 2: Let entities settle.
            Debug.Log("Settling entities...");
            SystemSettingsAPI.SetJointBreakSystemEnabled(ecsWorld, true);
            yield return EvolutionAPI.RunSimulationForSeconds(ecsWorld, settleTime, fullSpeed: false);
            EvolutionAPI.ZeroAllLimbVelocities(ecsWorld);

            // TODO: place entities above ground in ground trials. Will need to calc bounding box once for this.
            // Delegate responsibility to InitialiseAssessmentSystem?

            // Step 3: Start assessment.
            Debug.Log("Beginning assessment...");
            EvolutionAPI.BeginAssessment(ecsWorld, trialType);
            yield return EvolutionAPI.RunSimulationForSeconds(ecsWorld, assessmentTime, fullSpeed: false);

            // Step 4: Read back fitness values and assign to matching individuals.
            Debug.Log("Reading assessment results...");
            Dictionary<ulong, float> phenotypeFitnesses = EvolutionAPI.GetAssessmentResults(ecsWorld);
            foreach (Individual individual in population)
                if (phenotypeFitnesses.TryGetValue(individual.phenotype.Gid, out float fitness))
                    individual.fitness = Mathf.Max(fitness, 0f);

            // Step 5: Destroy ECS entities created from phenotypes.
            Debug.Log("Destroying phenotype entities...");
            EntityCreationAPI.DestroyAllPhenotypeEntities(ecsWorld);
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
