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

    public class EvolutionSimulator : MonoBehaviour
    {
        [Header("Environment SubScenes")]
        [SerializeField] private EntitySceneReference groundEnvironment;
        [SerializeField] private EntitySceneReference waterEnvironment;

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

            isRunning = true;
            currentGeneration = 0;

            while (currentGeneration < maxGenerations && isRunning)
            {
                Debug.Log($"Starting generation {currentGeneration + 1}/{maxGenerations}");
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

        private IEnumerator AssessPhenotypesCoroutine(List<Individual> population)
        {
            // Step 0: Completely reset the ECS world.
            // TODO: Overkill.
            WorldAPI.DestroyWorld(ecsWorld);
            ecsWorld = WorldAPI.CreateWorld("EvolutionWorld");

            // Step 1: Create ECS entities from phenotypes.
            Debug.Log("Creating entities from phenotypes...");
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

            // Step 2: Initialise trial.
            Debug.Log("Initialising trial...");
            yield return EvolutionAPI.InitialiseTrial(ecsWorld, trialType, groundEnvironment, waterEnvironment);

            // Step 3: Let entities settle.
            Debug.Log("Settling entities...");
            yield return EvolutionAPI.SettlePhenotypes(ecsWorld, settleSeconds, GetSimulationRateMode);

            // Step 4: Start assessment.
            Debug.Log("Beginning assessment...");
            yield return EvolutionAPI.AssessPhenotypes(ecsWorld, trialType, assessmentSeconds, GetSimulationRateMode);

            // Step 5: Read back fitness values and assign to matching individuals.
            Debug.Log("Reading assessment results...");
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
