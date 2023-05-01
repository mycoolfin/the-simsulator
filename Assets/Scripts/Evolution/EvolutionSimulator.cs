using System.Linq;
using System;
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

public enum SpeedControl
{
    Pause,
    Play,
    FastForward
}

public class EvolutionSimulator : MonoBehaviour
{
    [Header("Initialisation Parameters")]
    [SerializeField] private TrialType trialType = TrialType.GroundDistance;
    [SerializeField] private int numberOfIterations;
    [SerializeField] private int populationSize;
    [Range(0f, 1f)]
    [SerializeField] private float survivalRatio;
    [SerializeField] private Genotype seedGenotype;
    [SerializeField] private bool run;

    [Header("Runtime Parameters")]
    public SpeedControl speedControl;
    public bool pauseIterating;
    public bool stopSimulation;

    [Header("Visualisation Parameters")]
    public bool colourByRelativeFitness;
    public bool filterByPotentialSurvivors;

    [Header("Outputs")]
    public int currentIteration;
    public Individual bestIndividual;
    public List<float> bestFitnesses;
    public List<float> averageFitnesses;

    private Population population;
    public event Action OnIterationStart = delegate { };
    public event Action OnIterationEnd = delegate { };

    public bool running => population != null;

    private void Update()
    {
        if (running)
            VisualisePopulation();
    }

    public IEnumerator Run(TrialType trialType, int numberOfIterations, int populationSize, float survivalRatio, Genotype seedGenotype)
    {
        this.trialType = trialType;
        this.numberOfIterations = numberOfIterations;
        this.populationSize = populationSize;
        this.survivalRatio = survivalRatio;
        this.seedGenotype = seedGenotype;

        AssessmentFunction assessmentFunction = SetUpTrial(trialType);

        // Reset outputs.
        bestIndividual = null;
        bestFitnesses = new List<float>();
        averageFitnesses = new List<float>();

        if (seedGenotype != null)
            population = new Population(Enumerable.Range(0, populationSize).Select(_ => seedGenotype).ToList());
        else
            population = new Population(populationSize);

        int maxSurvivors = Mathf.CeilToInt(populationSize * survivalRatio);

        currentIteration = 0;
        while (currentIteration < numberOfIterations || numberOfIterations == -1)
        {
            currentIteration++;

            OnIterationStart();

            yield return StartCoroutine(AssessFitnesses(population, assessmentFunction));

            List<Individual> survivors = SelectSurvivors(population, maxSurvivors);
            int survivorCount = survivors.Count;

            if (bestIndividual == null || (survivorCount > 0 && survivors[0].fitness > bestIndividual.fitness))
            {
                bestIndividual = survivors[0];
            }

            bestFitnesses.Add(bestIndividual.fitness);
            averageFitnesses.Add(population.individuals.Average(x => x.fitness));

            Debug.Log("Iteration " + currentIteration + ": Best fitness = " + bestFitnesses.Last() + ", Average Fitness = " + averageFitnesses.Last());

            while (pauseIterating)
                yield return null;

            OnIterationEnd();

            ResetWorld();

            yield return null; // Gives time for cleanup to complete.

            population = ProduceNextGeneration(populationSize, populationSize - maxSurvivors, survivors);
        }

        currentIteration = -1;
        Debug.Log("Finished.");

        OnIterationStart();
        yield return StartCoroutine(AssessFitnesses(population, assessmentFunction));
    }

    public bool TogglePhenotypeProtection(Phenotype phenotype)
    {
        Individual individual = population.individuals.Find(i => i.phenotype == phenotype);
        individual.isProtected = !individual.isProtected;
        return individual.isProtected;
    }

    private AssessmentFunction SetUpTrial(TrialType trialType)
    {
        AssessmentFunction assessmentFunction;
        switch (trialType)
        {
            case TrialType.GroundDistance:
                assessmentFunction = Assessment.GroundDistance;
                WorldManager.Instance.world.transform.position = -WorldManager.Instance.groundOrigin.transform.localPosition;
                WorldManager.Instance.simulateFluid = false;
                WorldManager.Instance.gravity = true;
                WorldManager.Instance.pointLight.SetActive(false);
                break;
            case TrialType.WaterDistance:
                assessmentFunction = Assessment.WaterDistance;
                WorldManager.Instance.world.transform.position = -WorldManager.Instance.waterOrigin.transform.localPosition;
                WorldManager.Instance.simulateFluid = true;
                WorldManager.Instance.gravity = false;
                WorldManager.Instance.pointLight.SetActive(false);
                break;
            case TrialType.GroundLightFollowing:
                assessmentFunction = Assessment.LightCloseness;
                WorldManager.Instance.world.transform.position = -WorldManager.Instance.groundOrigin.transform.localPosition;
                WorldManager.Instance.simulateFluid = false;
                WorldManager.Instance.gravity = true;
                WorldManager.Instance.pointLight.SetActive(true);
                OnIterationStart += () =>
                    WorldManager.Instance.pointLight.transform.position =
                        Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0)
                        * Vector3.forward * UnityEngine.Random.Range(0f, 20f)
                        + Vector3.up * 1f;
                break;
            case TrialType.WaterLightFollowing:
                assessmentFunction = Assessment.LightCloseness;
                WorldManager.Instance.world.transform.position = -WorldManager.Instance.waterOrigin.transform.localPosition;
                WorldManager.Instance.simulateFluid = true;
                WorldManager.Instance.gravity = false;
                WorldManager.Instance.pointLight.SetActive(true);
                OnIterationStart += () =>
                    WorldManager.Instance.pointLight.transform.position =
                    Quaternion.Euler(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360))
                    * Vector3.forward * UnityEngine.Random.Range(0f, 20f);
                break;
            default:
                throw new Exception();
        }
        return assessmentFunction;
    }

    private IEnumerator AssessFitnesses(Population population, AssessmentFunction assessmentFunction)
    {
        List<IEnumerator> assessors = new List<IEnumerator>();

        Physics.simulationMode = SimulationMode.Script;

        foreach (Individual individual in population.individuals)
        {
            individual.phenotype = Phenotype.Construct(individual.genotype);
            individual.phenotype.gameObject.SetActive(false);
            if (individual.phenotype.IsValid())
                assessors.Add(assessmentFunction(individual));
            else
                UnityEngine.Object.Destroy(individual.phenotype.gameObject);
        }

        yield return null; // Ensure the invalid phenotypes are completely destroyed.

        Physics.simulationMode = SimulationMode.FixedUpdate;

        List<Coroutine> coroutines = assessors.Select(x => StartCoroutine(x)).ToList();

        // Wait for all coroutines to finish.
        foreach (Coroutine coroutine in coroutines)
            yield return coroutine;
    }

    private List<Individual> SelectSurvivors(Population population, int maxSurvivors)
    {
        return population.individuals
        .FindAll(x => x.phenotype != null && (x.fitness > 0 || x.isProtected))
        .OrderByDescending(x => x.isProtected ? Mathf.Infinity : x.fitness)
        .Take(maxSurvivors)
        .ToList();
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

                nextGeneration.individuals.Add(new()
                {
                    genotype = Reproduction.CreateOffspring(parent1, parent2)
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
        // Destroy all phenotypes.
        foreach (Individual individual in population.individuals)
        {
            if (individual.phenotype != null)
                Destroy(individual.phenotype.gameObject);
        }

        // Empty the trash can.
        WorldManager.Instance.EmptyTrashCan();
    }

    private void VisualisePopulation()
    {
        if (population == null)
            return;

        List<Individual> survivors = SelectSurvivors(population, Mathf.CeilToInt(populationSize * survivalRatio));
        Individual currentBestIndividual = survivors.Count > 0 ? survivors[0] : null;

        float currentAverageFitness = 0f;
        if (colourByRelativeFitness)
            currentAverageFitness = population.individuals.Average(x => x.fitness);

        foreach (Individual individual in population.individuals)
        {
            if (individual.phenotype == null)
                continue;

            if (individual.isProtected)
            {
                individual.phenotype.SetVisible(true);
                individual.phenotype.SetRGB(1f, 0.84f, 0f);
            }
            else
            {
                if (filterByPotentialSurvivors)
                    individual.phenotype.SetVisible(survivors.Contains(individual));
                else
                    individual.phenotype.SetVisible(true);

                if (colourByRelativeFitness)
                {
                    if (individual.fitness == 0f)
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
                    individual.phenotype.SetRGB(1f, 1f, 1f);
            }
        }
    }
}
