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
    [Header("Simulation Parameters")]
    public TrialType trial = TrialType.Running;
    public int numberOfIterations;
    public int populationSize;
    [Range(0f, 1f)]
    public float survivalRatio;
    [Range(0f, 5f)]
    public float timeScale;
    public bool exitEarly;

    [Header("Outputs")]
    public int iterationsRemaining;
    public float bestFitness;

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
        Time.timeScale = timeScale;
    }

    private IEnumerator Run(int numberOfIterations, int populationSize, float survivalRatio, AssessmentFunction assessmentFunction)
    {
        Genotype? bestGenotype = null;
        bestFitness = 0f;
        List<float> bestFitnesses = new List<float>();
        List<float> averageFitnesses = new List<float>();
        iterationsRemaining = numberOfIterations;

        Population population = new Population(populationSize);
        int maxSurvivors = Mathf.CeilToInt(populationSize * survivalRatio);

        for (int i = 0; i < numberOfIterations; i++)
        {
            yield return StartCoroutine(AssessFitnesses(population, assessmentFunction));

            Population survivors = SelectSurvivors(population, maxSurvivors);
            int survivorCount = survivors.assessableGenotypes.Count;

            if (survivorCount > 0 && survivors.assessableGenotypes[0].fitness > bestFitness)
            {
                bestGenotype = survivors.assessableGenotypes[0].genotype;
                bestFitness = (float)survivors.assessableGenotypes[0].fitness;
            }

            bestFitnesses.Add(bestFitness);
            averageFitnesses.Add(population.assessableGenotypes.Average(x => x.fitness ?? 0));

            Debug.Log("Iteration " + (i + 1) + ": Best fitness = " + bestFitnesses.Last() + ", Average Fitness = " + averageFitnesses.Last());

            population = ProduceNextGeneration(populationSize, populationSize - maxSurvivors, survivors);

            iterationsRemaining--;

            if (exitEarly)
                break;
        }

        if (bestGenotype != null)
            ParadeGenotype((Genotype)bestGenotype, assessmentFunction);
    }

    private IEnumerator AssessFitnesses(Population population, AssessmentFunction assessmentFunction)
    {
        List<IEnumerator> assessors = new List<IEnumerator>();

        Physics.simulationMode = SimulationMode.Script;

        for (int i = 0; i < population.assessableGenotypes.Count; i++)
        {
            Phenotype phenotype = PhenotypeBuilder.ConstructPhenotype(population.assessableGenotypes[i].genotype);
            if (phenotype.IsValid())
            {
                foreach (Collider col in phenotype.GetComponentsInChildren<Collider>())
                    foreach (Phenotype otherPhenotype in FindObjectsOfType<Phenotype>())
                        if (otherPhenotype != phenotype)
                            foreach (Collider otherCol in otherPhenotype.GetComponentsInChildren<Collider>())
                                Physics.IgnoreCollision(col, otherCol);

                int index = i;
                assessors.Add(assessmentFunction(phenotype, bestFitness, fitnessScore =>
                {
                    population.assessableGenotypes[index].fitness = fitnessScore;
                    UnityEngine.Object.Destroy(phenotype.gameObject);
                }
                ));
            }
            else
            {
                population.assessableGenotypes[i].fitness = 0f;
                UnityEngine.Object.Destroy(phenotype.gameObject);
            }
        }

        yield return null; // Ensure the phenotypes are completely destroyed.

        Physics.simulationMode = SimulationMode.FixedUpdate;

        List<Coroutine> coroutines = assessors.Select(x => StartCoroutine(x)).ToList();

        // Wait for all coroutines to finish.
        foreach (Coroutine coroutine in coroutines)
            yield return coroutine;

        yield return null; // Ensure the phenotypes are completely destroyed.
    }

    private Population SelectSurvivors(Population population, int maxSurvivors)
    {
        Population survivors = new Population();

        survivors.assessableGenotypes = population.assessableGenotypes
        .FindAll(x => x.fitness > 0)
        .OrderByDescending(x => x.fitness)
        .Take(maxSurvivors)
        .ToList();

        return survivors;
    }

    private Population ProduceNextGeneration(int populationSize, int maxChildren, Population seedPopulation)
    {
        Population nextGeneration = new Population();

        nextGeneration.assessableGenotypes = seedPopulation.assessableGenotypes.ToList();

        float totalFitness = nextGeneration.assessableGenotypes.Sum(x => x.fitness ?? 0);
        if (totalFitness > 0)
        {
            for (int i = 0; i < maxChildren; i++)
            {
                // Choose parents, weighted by relative fitness.
                Genotype? parent1 = null;
                Genotype? parent2 = null;
                float parent1Value = UnityEngine.Random.value;
                float parent2Value = UnityEngine.Random.value;

                float wheelLocation = 0f;
                for (int j = 0; j < nextGeneration.assessableGenotypes.Count; j++)
                {
                    float fitnessRatio = (nextGeneration.assessableGenotypes[j].fitness ?? 0) / totalFitness;
                    float segmentStart = wheelLocation;
                    float segmentEnd = wheelLocation + fitnessRatio;
                    if (parent1Value >= segmentStart && parent1Value <= segmentEnd)
                        parent1 = nextGeneration.assessableGenotypes[j].genotype;
                    if (parent2Value >= segmentStart && parent2Value <= segmentEnd)
                        parent2 = nextGeneration.assessableGenotypes[j].genotype;
                    wheelLocation = segmentEnd;

                    if (parent1 != null && parent2 != null)
                        break;
                }

                nextGeneration.assessableGenotypes.Add(new()
                {
                    genotype = Reproduction.CreateOffspring((Genotype)parent1, (Genotype)parent2)
                });
            }
        }

        // Ensure population size stays the same - fill gaps with randomly generated genotypes if necessary.
        int populationDiscrepancy = populationSize - nextGeneration.assessableGenotypes.Count;
        for (int j = 0; j < populationDiscrepancy; j++)
            nextGeneration.assessableGenotypes.Add(new()
            {
                genotype = Genotype.CreateRandom()
            });
        if (populationDiscrepancy > 0)
            Debug.Log("Added " + populationDiscrepancy + " seed genotypes to maintain population size.");

        return nextGeneration;
    }

    private void ParadeGenotype(Genotype genotype, AssessmentFunction assessmentFunction)
    {
        timeScale = 1f;
        Phenotype phenotype = PhenotypeBuilder.ConstructPhenotype(genotype);
        Debug.Break();
        StartCoroutine(assessmentFunction(phenotype, bestFitness, fitnessScore => { Debug.Log("Best Genotype's Fitness: " + fitnessScore); }));
    }
}
