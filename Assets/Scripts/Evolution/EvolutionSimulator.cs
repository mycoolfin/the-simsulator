using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TrialType
{
    GroundDistance,
    WaterDistance,
    GroundLightFollowing,
    WaterLightFollowing
};

public enum SpeedControl { Pause, Play, FastForward }

public enum Status { Pending, InProgress, Complete }

public class EvolutionSimulator : MonoBehaviour
{
    [Header("Initialisation Parameters")]
    [SerializeField] private TrialType trial = TrialType.GroundDistance;
    public TrialType Trial => trial;
    [SerializeField] private int maxIterations;
    public int MaxIterations => maxIterations;
    [SerializeField] private int populationSize;
    public int PopulationSize => populationSize;
    [Range(0f, 1f)]
    [SerializeField] private float survivalPercentage;
    public float SurvivalPercentage => survivalPercentage;
    public float MutationRate => ParameterManager.Instance.Mutation.MutationRate;
    [SerializeField] private Genotype seedGenotype;
    public Genotype SeedGenotype => seedGenotype;
    [SerializeField] private bool run;

    [Header("Runtime Parameters")]
    public SpeedControl speedControl;
    public bool pauseIterating;
    public bool stopSimulation;

    [Header("Visualisation Parameters")]
    public bool colourByRelativeFitness;
    public int focusBestCreatures;

    [Header("Outputs")]
    public int currentIteration;
    public Status simulationStatus;
    public Status iterationStatus;
    public Status preparationStatus;
    public Status assessmentStatus;
    public float assessmentProgress;
    public Individual bestIndividual;
    public List<float> bestFitnesses;
    public List<float> averageFitnesses;
    public DateTime lastIterationTime;
    public List<float> iterationElapsedSeconds;

    public event Action OnSimulationStart = delegate { };
    public event Action OnIterationStart = delegate { };
    public event Action OnAssessmentStart = delegate { };
    public event Action OnAssessmentEnd = delegate { };
    public event Action OnIterationEnd = delegate { };
    public event Action OnSimulationEnd = delegate { };

    private Population population;
    private int maxSurvivors;
    private List<Individual> survivors;
    private Assessment assessment;

    public bool Running => population != null;

    private void Awake()
    {
        simulationStatus = Status.Pending;
    }

    private void Update()
    {
        if (Running)
        {
            VisualisePopulation();
            CalculateAssessmentProgress();
        }

        if (simulationStatus == Status.InProgress && iterationStatus == Status.Complete && !pauseIterating)
            StartNextIteration();
    }

    public void StartSimulation(TrialType trial, int maxIterations, int populationSize, float survivalPercentage, float mutationRate, Genotype seedGenotype)
    {
        this.trial = trial;
        this.maxIterations = maxIterations;
        this.populationSize = populationSize;
        this.survivalPercentage = survivalPercentage;
        ParameterManager.Instance.Mutation.MutationRate = mutationRate;
        this.seedGenotype = seedGenotype;

        maxSurvivors = Mathf.CeilToInt(populationSize * survivalPercentage);
        assessment = PickAssessment(trial);

        EnableInterPhenotypeCollisions(false);

        // Reset outputs.
        bestIndividual = null;
        bestFitnesses = new List<float>();
        averageFitnesses = new List<float>();
        iterationElapsedSeconds = new List<float>();

        Debug.Log("Starting simulation...");
        simulationStatus = Status.InProgress;
        assessment.BeforeSimulationStart();
        OnSimulationStart();

        lastIterationTime = DateTime.Now;

        currentIteration = 0;
        StartNextIteration();
    }

    private void EndSimulation()
    {
        Debug.Log("Finished simulation.");
        simulationStatus = Status.Complete;
        OnSimulationEnd();
    }

    private void StartNextIteration()
    {
        currentIteration++;
        iterationStatus = Status.InProgress;
        preparationStatus = Status.Pending;
        assessmentStatus = Status.Pending;

        ResetWorld();

        population = currentIteration == 1
            ? ProduceInitialPopulation(seedGenotype, maxSurvivors)
            : ProduceNextGeneration(populationSize, populationSize - maxSurvivors, survivors);

        ConstructPhenotypes(population);

        Debug.Log("Starting iteration " + currentIteration + "...");
        assessment.BeforeIterationStart();
        OnIterationStart();

        StartPreparation();
    }

    private void EndCurrentIteration()
    {
        if (currentIteration > 0)
        {
            survivors = SelectSurvivors(population, maxSurvivors, includeZeroFitness: false);
            int survivorCount = survivors.Count;

            if (survivorCount > 0)
                bestIndividual = survivors.OrderByDescending(x => x.fitness).First();

            bestFitnesses.Add(bestIndividual?.fitness ?? 0f);
            averageFitnesses.Add(population.individuals.Average(x => x.fitness));
            float elapsedSeconds = (float)(DateTime.Now - lastIterationTime).TotalSeconds;
            iterationElapsedSeconds.Add(elapsedSeconds);
            lastIterationTime = DateTime.Now;

            Debug.Log("Finished iteration " + currentIteration + ".");
            Debug.Log("STATS: " + "Best fitness = " + bestFitnesses.Last() + ", Average Fitness = " + averageFitnesses.Last() + ", Elapsed Time = " + elapsedSeconds + " secs");
            iterationStatus = Status.Complete;
            OnIterationEnd();

            if (currentIteration == maxIterations)
                EndSimulation();
        }
    }

    private void StartPreparation()
    {
        Debug.Log("Starting preparation...");
        preparationStatus = Status.InProgress;
        StartCoroutine(PreparePopulation(population, assessment));
    }

    private void EndPreparation()
    {
        Debug.Log("Finished preparation.");
        preparationStatus = Status.Complete;

        StartAssessment();
    }

    private void StartAssessment()
    {
        Debug.Log("Starting assessment...");
        assessmentStatus = Status.InProgress;
        assessment.BeforeAssessmentStart();
        OnAssessmentStart();
        StartCoroutine(AssessFitnesses(population, assessment));
    }

    private void EndAssessment()
    {
        Debug.Log("Finished assessment.");
        assessmentStatus = Status.Complete;
        OnAssessmentEnd();

        EndCurrentIteration();
    }

    public Transform GetSimulationOrigin()
    {
        return assessment?.trialOrigin;
    }

    public Individual GetIndividualByPhenotype(Phenotype phenotype) => population?.individuals.Find((i) => i.phenotype == phenotype);

    public List<Individual> GetTopIndividuals(int count)
    {
        if (population == null)
            return new();
        else
            return SelectSurvivors(population, count, includeZeroFitness: true);
    }

    private void EnableInterPhenotypeCollisions(bool enable)
    {
        string[] phenotypeLayers = new string[] {
            "Phenotype",
            "Best Individual 1",
            "Best Individual 2",
            "Best Individual 3",
            "Best Individual 4",
            "Best Individual 5",
            "Best Individual 6",
            "Best Individual 7",
            "Best Individual 8",
            "Best Individual 9",
            "Best Individual 10",
            "Best Individual 11",
            "Best Individual 12"
        };
        foreach (string layerName in phenotypeLayers)
        {
            LayerMask layerMask = LayerMask.NameToLayer(layerName);
            Physics.IgnoreLayerCollision(layerMask, layerMask, !enable);
        }
    }

    private Assessment PickAssessment(TrialType trialType)
    {
        Assessment assessment;
        switch (trialType)
        {
            case TrialType.GroundDistance:
                assessment = new GroundDistanceAssessment();
                break;
            case TrialType.WaterDistance:
                assessment = new WaterDistanceAssessment();
                break;
            case TrialType.GroundLightFollowing:
                assessment = new GroundLightClosenessAssessment();
                break;
            case TrialType.WaterLightFollowing:
                assessment = new WaterLightClosenessAssessment();
                break;
            default:
                throw new Exception();
        }
        return assessment;
    }

    private void ConstructPhenotypes(Population population)
    {
        Physics.simulationMode = SimulationMode.Script;

        foreach (Individual individual in population.individuals)
        {
            individual.phenotype = Phenotype.Construct(individual.genotype);
            individual.phenotype.gameObject.SetActive(false);
            if (!individual.phenotype.IsValid())
                individual.Cull();
        }

        Physics.simulationMode = SimulationMode.FixedUpdate;
    }

    private IEnumerator PreparePopulation(Population population, Assessment assessment)
    {
        yield return ProcessPopulationAsync(population, (individual) => assessment.PreProcess(individual, population));
        yield return ProcessPopulationAsync(population, (individual) => assessment.WaitUntilSettled(individual));
        EndPreparation();
    }

    private IEnumerator AssessFitnesses(Population population, Assessment assessment)
    {
        yield return ProcessPopulationAsync(population, (individual) => assessment.Assess(individual));
        EndAssessment();
    }

    private IEnumerator ProcessPopulationAsync(Population population, Func<Individual, IEnumerator> function)
    {
        List<Coroutine> coroutines = new List<Coroutine>();
        foreach (Individual individual in population.individuals)
            coroutines.Add(StartCoroutine(function(individual)));

        // Wait for all coroutines to finish.
        foreach (Coroutine coroutine in coroutines)
            yield return coroutine;
    }

    private List<Individual> SelectSurvivors(Population population, int maxSurvivors, bool includeZeroFitness)
    {
        return population.individuals
        .FindAll(x => x.phenotype != null && (x.isProtected || includeZeroFitness || x.fitness > 0))
        .OrderByDescending(x => x.isProtected ? Mathf.Infinity : x.fitness)
        .Take(maxSurvivors)
        .ToList();
    }

    private Population ProduceInitialPopulation(Genotype seedGenotype, int maxSurvivors)
    {
        Population population;
        if (seedGenotype != null)
        {
            population = new Population(Enumerable.Range(0, maxSurvivors).Select(_ => seedGenotype).ToList());
            population.individuals.ForEach(i => i.fitness = 1f);
            population = ProduceNextGeneration(populationSize, populationSize - maxSurvivors, population.individuals);
        }
        else
            population = new Population(populationSize);
        return population;
    }

    private Population ProduceNextGeneration(int populationSize, int maxChildren, List<Individual> seedIndividuals)
    {
        Population nextGeneration = new Population(seedIndividuals);

        float totalFitness = seedIndividuals.Sum(x => x.fitness);
        if (totalFitness > 0)
        {
            for (int i = 0; i < maxChildren; i++)
            {
                // Choose parents, weighted by relative fitness.
                Genotype parent1 = null;
                Genotype parent2 = null;
                float parent1Value = UnityEngine.Random.value;
                float parent2Value = UnityEngine.Random.value;

                float wheelLocation = 0f;
                foreach (Individual seedIndividual in seedIndividuals)
                {
                    float fitnessRatio = seedIndividual.fitness / totalFitness;
                    float segmentStart = wheelLocation;
                    float segmentEnd = wheelLocation + fitnessRatio;
                    if (parent1Value >= segmentStart && parent1Value <= segmentEnd)
                        parent1 = seedIndividual.genotype;
                    if (parent2Value >= segmentStart && parent2Value <= segmentEnd)
                        parent2 = seedIndividual.genotype;
                    wheelLocation = segmentEnd;

                    if (parent1 != null && parent2 != null)
                        break;
                }

                // Not sure how this is happening - destroying a Phenotype seems to also destroy the
                // attached Genotype object, despite the Individual object having a reference to it.
                if (parent1 == null || parent2 == null)
                    Debug.Log("Something went wrong during reproduction (was a parent destroyed?).");
                else
                    nextGeneration.individuals.Add(new()
                    {
                        genotype = Reproduction.CreateRandomisedOffspring(parent1, parent2)
                    });
            }
        }

        // Ensure population size stays the same - fill gaps with randomly generated genotypes if necessary.
        int populationDiscrepancy = populationSize - nextGeneration.individuals.Count;
        for (int j = 0; j < populationDiscrepancy; j++)
            nextGeneration.individuals.Add(new()
            {
                genotype = Genotype.CreateRandom()
            });
        if (populationDiscrepancy > 0)
            Debug.Log("Added " + populationDiscrepancy + " seed genotypes to maintain population size.");

        return nextGeneration;
    }

    private void ResetWorld()
    {
        foreach (Individual individual in population?.individuals ?? new())
        {
            if (individual.phenotype != null)
                WorldManager.Instance.SendGameObjectToTheVoid(individual.phenotype.gameObject);
        }

        WorldManager.Instance.EmptyTrashCan();
    }

    private void VisualisePopulation()
    {
        if (population == null)
            return;

        List<Individual> survivors = SelectSurvivors(population, Mathf.CeilToInt(populationSize * survivalPercentage), includeZeroFitness: false);
        Individual currentBestIndividual = survivors.Count > 0 ? survivors[0] : null;

        float currentAverageFitness = 0f;
        if (colourByRelativeFitness)
            currentAverageFitness = population.individuals.Average(x => x.fitness);

        foreach (Individual individual in population.individuals)
        {
            if (individual.phenotype == null)
                continue;

            if (individual.isProtected)
                individual.phenotype.SetRGB(1f, 0.84f, 0f);
            else
            {
                if (colourByRelativeFitness)
                {
                    if (individual.assessmentProgress == -1f)
                        individual.phenotype.ClearRGB();
                    else if (individual.fitness == 0f)
                        individual.phenotype.SetRGB(1f, 0f, 0f);
                    else if (individual == currentBestIndividual)
                        individual.phenotype.SetRGB(0f, 1f, 1f);
                    else
                    {
                        float x = (float)Math.Exp(-(1 / currentAverageFitness) * individual.fitness);
                        individual.phenotype.SetRGB(x, 1f - x, 0f);
                    }
                }
                else
                    individual.phenotype.ClearRGB();
            }
        }
    }

    private void CalculateAssessmentProgress()
    {
        if (population == null)
            return;

        assessmentProgress = population.individuals.Min(i => i.assessmentProgress);
    }
}
