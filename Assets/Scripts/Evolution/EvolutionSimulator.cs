using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TrialType
{
    Running,
    Swimming
};

public class EvolutionSimulator : MonoBehaviour
{
    [Header("Initialisation Parameters")]
    public TrialType trial = TrialType.Running;
    public int numberOfIterations;
    public int populationSize;
    [Range(0f, 1f)]
    public float survivalRatio;
    private int maxSurvivors;

    [Header("Runtime Parameters")]
    public bool pauseIterating;
    public bool exitEarly;

    [Header("Visualisation Parameters")]
    public bool colourByRelativeFitness;
    public bool filterByPotentialSurvivors;

    [Header("Outputs")]
    public int iterationsRemaining;
    public float bestFitness;

    private Population population;

    private void Start()
    {
        AssessmentFunction assessmentFunction;
        switch (trial)
        {
            case TrialType.Running:
                assessmentFunction = Assessment.GroundDistance;
                WorldManager.Instance.simulateFluid = false;
                WorldManager.Instance.gravity = true;
                WorldManager.Instance.EnableGround(true);
                break;
            case TrialType.Swimming:
                assessmentFunction = Assessment.WaterDistance;
                WorldManager.Instance.simulateFluid = true;
                WorldManager.Instance.gravity = false;
                WorldManager.Instance.EnableGround(false);
                break;
            default:
                throw new Exception();
        }
        StartCoroutine(Run(numberOfIterations, populationSize, survivalRatio, assessmentFunction));
    }

    private void Update()
    {
        VisualisePopulation();
    }

    private IEnumerator Run(int numberOfIterations, int populationSize, float survivalRatio, AssessmentFunction assessmentFunction)
    {
        Individual bestIndividual = null;
        bestFitness = 0f;
        List<float> bestFitnesses = new List<float>();
        List<float> averageFitnesses = new List<float>();
        iterationsRemaining = numberOfIterations;

        population = new Population(populationSize);
        maxSurvivors = Mathf.CeilToInt(populationSize * survivalRatio);

        for (int i = 0; i < numberOfIterations; i++)
        {
            yield return StartCoroutine(AssessFitnesses(population, assessmentFunction));

            List<Individual> survivors = SelectSurvivors(population, maxSurvivors);
            int survivorCount = survivors.Count;

            if (survivorCount > 0 && survivors[0].fitness > bestFitness)
            {
                bestIndividual = survivors[0];
                bestFitness = (float)survivors[0].fitness;
            }

            bestFitnesses.Add(bestFitness);
            averageFitnesses.Add(population.individuals.Average(x => x.fitness));

            Debug.Log("Iteration " + (i + 1) + ": Best fitness = " + bestFitnesses.Last() + ", Average Fitness = " + averageFitnesses.Last());

            while (pauseIterating)
                yield return null;

            ResetWorld();

            yield return null; // Gives time for cleanup to complete.

            iterationsRemaining--;

            if (exitEarly)
                break;

            population = ProduceNextGeneration(populationSize, populationSize - maxSurvivors, survivors);
        }

        Debug.Log("Finished.");

        if (bestIndividual != null)
        {
            bestIndividual.phenotype = PhenotypeBuilder.ConstructPhenotype(bestIndividual.genotype);
            StartCoroutine(assessmentFunction(bestIndividual));
            Time.timeScale = 1f;
            Debug.Break();
        }
    }

    private IEnumerator AssessFitnesses(Population population, AssessmentFunction assessmentFunction)
    {
        List<IEnumerator> assessors = new List<IEnumerator>();

        Physics.simulationMode = SimulationMode.Script;

        foreach (Individual individual in population.individuals)
        {
            individual.phenotype = PhenotypeBuilder.ConstructPhenotype(individual.genotype);
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
        .FindAll(x => x.fitness > 0)
        .OrderByDescending(x => x.fitness)
        .Take(maxSurvivors)
        .ToList();
    }

    private Population ProduceNextGeneration(int populationSize, int maxChildren, List<Individual> seedIndividuals)
    {
        Population nextGeneration = new Population(seedIndividuals.Select(x => x.genotype).ToList());

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

        List<Individual> survivors = SelectSurvivors(population, maxSurvivors);
        Individual currentBestIndividual = survivors.Count > 0 ? survivors[0] : null;

        float currentAverageFitness = 0f;
        if (colourByRelativeFitness)
            currentAverageFitness = population.individuals.Average(x => x.fitness);

        foreach (Individual individual in population.individuals)
        {
            if (individual.phenotype == null)
                continue;

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
