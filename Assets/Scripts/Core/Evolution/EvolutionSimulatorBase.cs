using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.Evolution
{
    using TheSimsulator.Core.Evolution;
    using TheSimsulator.Core.Genotype;
    using TheSimsulator.Core.Phenotype;

    using SimulationRateMode = ECS.Systems.Simulation.SimulationRate.SimulationRateMode;

    public enum TrialType : byte
    {
        GroundDistance,
        WaterDistance
    };

    public struct EvolutionParameters
    {
        public TrialType? TrialType;
        public int? PopulationSize;
        public int? MaxGenerations;
        public float? SurvivalRate;
        public float? MutationRate;
        public float? SettleSeconds;
        public float? AssessmentSeconds;
        public string SeedGenotypePath;
        public bool? LockMorphologies;
    }

    public struct EvolutionStatistics
    {
        public int Generation;
        public float BestFitness;
        public float AverageFitness;
        public float ElapsedTime;
    }

    public abstract class EvolutionSimulatorBase<TGenotype, TPhenotype, TEvolution, TEvolutionConfig, TECSAPI> : MonoBehaviour, IEvolutionSimulator
        where TGenotype : IGenotype<TGenotype>
        where TPhenotype : IPhenotype<TPhenotype>
        where TEvolution : EvolutionBase<TEvolutionConfig, TGenotype, TPhenotype, AssessableCreature<TGenotype, TPhenotype>>
        where TEvolutionConfig : EvolutionConfigBase, new()
        where TECSAPI : ECS.API.IECSAPI<TPhenotype>, new()
    {
        public delegate TEvolution EvolutionFactoryDelegate(
            TEvolutionConfig config,
            EvolutionBase<TEvolutionConfig, TGenotype, TPhenotype, AssessableCreature<TGenotype, TPhenotype>>.AssessIndividualsDelegate assessPhenotypesDelegate
        );
        private EvolutionFactoryDelegate evolutionFactory;
        public EvolutionSimulatorBase(EvolutionFactoryDelegate evolutionFactory)
        {
            this.evolutionFactory = evolutionFactory;
        }

        [Header("Evolution Parameters")]
        [SerializeField] private int populationSize = 100;
        public int PopulationSize => populationSize;
        [SerializeField] private int maxGenerations = 100;
        public int MaxGenerations => maxGenerations;
        [SerializeField] private float survivalRate = 0.2f;
        public float SurvivalRate => survivalRate;
        public int MaxSurvivors => (int)Mathf.Ceil(populationSize * survivalRate);
        [SerializeField] private float mutationRate = 1f;
        public float MutationRate => mutationRate;
        [SerializeField] private float settleSeconds = 10f;
        public float SettleSeconds => settleSeconds;
        [SerializeField] private float assessmentSeconds = 10f;
        public float AssessmentSeconds => assessmentSeconds;
        [SerializeField] private TrialType trialType = TrialType.GroundDistance;
        public TrialType TrialType => trialType;
        [SerializeField] private TGenotype seedGenotype; // TODO: Rig up
        public string SeedGenotypeName => seedGenotype?.ToString();
        [SerializeField] private bool lockMorphologies = false; // TODO: Rig up
        public bool LockMorphologies => lockMorphologies;

        // Seeding isn't working yet on the ECS side.
        // [SerializeField] private int simulationSeed = 0;
        // [SerializeField] private bool useSimulationSeed = false;

        [Header("Controls")]
        [SerializeField] private SimulationRateMode simulationRate = SimulationRateMode.FullSpeed;
        [SerializeField] private bool pauseEvolutionLoop = false;

        [Header("Runtime Status")]
        public bool IsRunning { get; private set; } = false;
        public bool IsEvolutionLoopPaused => pauseEvolutionLoop;
        public bool IsSimulationPaused => simulationRate == SimulationRateMode.Paused;
        public bool IsSimulationRealTime => simulationRate == SimulationRateMode.RealTime;
        public bool IsSimulationFullSpeed => simulationRate == SimulationRateMode.FullSpeed;
        public bool IsSimulationHeadless => simulationRate == SimulationRateMode.Headless;
        public int CurrentGeneration { get; private set; }
        public float SettleProgress { get; private set; }
        private Progress<float> settleProgressManager;
        public float AssessmentProgress { get; private set; }
        private Progress<float> assessmentProgressManager;
        private readonly List<EvolutionStatistics> statistics = new();
        public IReadOnlyList<EvolutionStatistics> Statistics => statistics;

        // Events.
        public event Action OnEvolutionStart;
        public event Action OnEvolutionStop;
        public event Action<World> OnEcsWorldCreated;
        public event Action<int> OnGenerationStart;
        public event Action<IReadOnlyList<EvolutionStatistics>> OnGenerationComplete;
        public event Action OnEvolutionComplete;

        private TEvolution evolution;
        private World ecsWorld;
        private TECSAPI ecsApi;
        private Coroutine evolutionCoroutine;

        private void Awake()
        {
            ecsApi = new TECSAPI();
        }

        public void SetEvolutionParameters(EvolutionParameters parameters)
        {
            if (IsRunning)
            {
                Debug.LogWarning("Cannot change evolution parameters while evolution is running!");
                return;
            }

            populationSize = parameters.PopulationSize ?? populationSize;
            maxGenerations = parameters.MaxGenerations ?? maxGenerations;
            survivalRate = parameters.SurvivalRate ?? survivalRate;
            mutationRate = parameters.MutationRate ?? mutationRate;
            settleSeconds = parameters.SettleSeconds ?? settleSeconds;
            assessmentSeconds = parameters.AssessmentSeconds ?? assessmentSeconds;
            trialType = parameters.TrialType ?? trialType;
            if (parameters.SeedGenotypePath != null)
            {
                GenotypeIO.DeserializeAsync<TGenotype>(parameters.SeedGenotypePath, (success, genotype) =>
                {
                    if (success)
                        seedGenotype = genotype;
                    else
                        Debug.LogError($"Failed to deserialize seed genotype from {parameters.SeedGenotypePath}");
                });
            }
            lockMorphologies = parameters.LockMorphologies ?? lockMorphologies;
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
            IsRunning = false;

            if (evolutionCoroutine != null)
            {
                StopCoroutine(evolutionCoroutine);
                evolutionCoroutine = null;
            }

            evolution.Dispose();

            ecsApi.World.DestroyWorld(ecsWorld);

            OnEvolutionStop?.Invoke();
        }

        public void SetEvolutionLoopPaused(bool paused) => pauseEvolutionLoop = paused;

        public void PauseSimulation() => simulationRate = SimulationRateMode.Paused;
        public void RealTimeSimulation() => simulationRate = SimulationRateMode.RealTime;
        public void FullSpeedSimulation() => simulationRate = SimulationRateMode.FullSpeed;
        public void HeadlessSimulation() => simulationRate = SimulationRateMode.Headless;

        public List<IAssessableCreature> GetBestCreatures(int count)
        {
            if (evolution == null || evolution.Population == null || evolution.Population.Count == 0)
                return new List<IAssessableCreature>();

            return evolution.Population
                .OrderByDescending(i => i.Fitness)
                .Take(count)
                .Cast<IAssessableCreature>()
                .ToList();
        }

        public ECS.API.ISimulationSettings GetSimulationSettingsAPI() => ecsApi.Simulation;
        public ECS.API.IPresentationSettings GetPresentationSettingsAPI() => ecsApi.Presentation;

        private IEnumerator EvolutionLoop()
        {
            Debug.Log($"Beginning evolution. Parameters: Population Size = {populationSize}, Max Generations = {maxGenerations}, Survival Rate = {survivalRate}, Mutation Rate = {mutationRate}, Settle Seconds = {settleSeconds}, Assessment Seconds = {assessmentSeconds}, Trial Type = {trialType}");
            OnEvolutionStart?.Invoke();

            TEvolutionConfig config = new()
            {
                PopulationSize = populationSize,
                SurvivalRate = survivalRate,
                MutationRate = mutationRate,
            };
            // if (useSimulationSeed) config.Seed = simulationSeed;
            evolution = evolutionFactory(
                config,
                (population, cancellationToken) =>
                    Utilities.AsyncUtils.CoroutineAsTask(this, AssessIndividualsCoroutine(population), cancellationToken));

            statistics.Clear();

            settleProgressManager = new Progress<float>(p => SettleProgress = p);
            assessmentProgressManager = new Progress<float>(p => AssessmentProgress = p);

            IsRunning = true;
            CurrentGeneration = 0;

            ecsWorld = ecsApi.World.GetOrCreateWorld("EvolutionWorld", (world) => OnEcsWorldCreated?.Invoke(world));

            bool runForever = maxGenerations <= 0;
            while ((runForever || CurrentGeneration <= maxGenerations) && IsRunning)
            {
                CurrentGeneration++;

                float startTime = Time.time;
                SettleProgress = -1f;
                AssessmentProgress = -1f;
                OnGenerationStart?.Invoke(CurrentGeneration);

                yield return Utilities.AsyncUtils.TaskAsCoroutine(evolution.IterateAsync());

                EvolutionStatistics stats = new()
                {
                    Generation = CurrentGeneration,
                    BestFitness = GetBestFitness(evolution.Population),
                    AverageFitness = GetAverageFitness(evolution.Population),
                    ElapsedTime = Time.time - startTime
                };
                statistics.Add(stats);

                string maxGenerationsString = runForever ? "∞" : maxGenerations.ToString();
                Debug.Log($"Generation {CurrentGeneration}/{maxGenerationsString} complete. Best fitness: {stats.BestFitness}. Average fitness: {stats.AverageFitness}. Elapsed time: {stats.ElapsedTime:F2} seconds.");
                OnGenerationComplete?.Invoke(statistics);

                while (pauseEvolutionLoop && IsRunning)
                    yield return null; // Wait until unpaused.
            }

            IsRunning = false;
            ecsApi.World.DestroyWorld(ecsWorld);

            Debug.Log("Evolution complete.");
            OnEvolutionComplete?.Invoke();
        }

        private IEnumerator AssessIndividualsCoroutine(List<AssessableCreature<TGenotype, TPhenotype>> population)
        {
            // Destroy all existing phenotype entities.
            yield return ecsApi.Phenotype.DestroyAllPhenotypeEntities(ecsWorld);

            // Create ECS entities from phenotypes.
            yield return ecsApi.Phenotype.CreateEntitiesFromPhenotypes(
                ecsWorld,
                population
                .Select((individual, i) => new ECS.API.PhenotypeEntityCreationInfo<TPhenotype>
                {
                    Phenotype = individual.Phenotype,
                    PhysicsPositionOffset = Vector3.zero,
                    AllowInterPhenotypeCollisions = false
                }).ToList()
            );

            // Assign the GetPhenotypeTransformData delegate to each individual.
            foreach (AssessableCreature<TGenotype, TPhenotype> individual in population)
                individual.GetPhenotypeTransformData = () => ecsApi.Phenotype.GetPhenotypeTransformData(ecsWorld, individual.Phenotype);

            // Initialise the trial world.
            yield return ecsApi.Evolution.InitialiseTrial(ecsWorld, trialType, () => simulationRate);

            // Let entities settle.
            yield return ecsApi.Evolution.SettlePhenotypes(ecsWorld, settleSeconds, () => simulationRate, settleProgressManager);

            // Start assessment.
            yield return ecsApi.Evolution.AssessPhenotypes(ecsWorld, trialType, assessmentSeconds, () => simulationRate, assessmentProgressManager);

            // Read back fitness values and assign to matching individuals.
            Dictionary<ulong, float> phenotypeFitnesses = ecsApi.Evolution.GetAssessmentResults(ecsWorld);
            float maxFitness = 0f;
            foreach (AssessableCreature<TGenotype, TPhenotype> individual in population)
                if (phenotypeFitnesses.TryGetValue(individual.Phenotype.Gid, out float fitness))
                {
                    individual.Fitness = Mathf.Max(fitness, 0f);
                    maxFitness = Mathf.Max(maxFitness, individual.Fitness);
                }
                else
                    individual.Fitness = 0f;

            // Set the fitness of each protected individual to 1% more than the maximum fitness.
            float protectionFitness = Mathf.Max(0.001f, maxFitness * 1.01f);
            foreach (AssessableCreature<TGenotype, TPhenotype> individual in population)
            {
                if (individual.IsProtected)
                    individual.Fitness = protectionFitness;
            }
        }

        private static float GetAverageFitness(IReadOnlyList<AssessableCreature<TGenotype, TPhenotype>> currentPopulation)
        {
            if (currentPopulation == null || currentPopulation.Count == 0)
                return 0f;

            return currentPopulation.Average(i => i.Fitness);
        }

        private static float GetBestFitness(IReadOnlyList<AssessableCreature<TGenotype, TPhenotype>> currentPopulation)
        {
            if (currentPopulation == null || currentPopulation.Count == 0)
                return 0f;

            return currentPopulation.Max(i => i.Fitness);
        }

        private void OnDestroy()
        {
            if (IsRunning)
                StopEvolution();
        }
    }
}
