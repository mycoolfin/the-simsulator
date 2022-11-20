using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionSimulator : MonoBehaviour
{
    [Header("Simulation Parameters")]
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
        StartCoroutine(Run(numberOfIterations, populationSize, survivalRatio, Assessment.WaterDistance));
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
            int survivorCount = survivors.genotypes.Length;

            if (survivorCount > 0 && survivors.fitnesses[0] > bestFitness)
            {
                bestGenotype = survivors.genotypes[0];
                bestFitness = survivors.fitnesses[0];
            }

            bestFitnesses.Add(bestFitness);
            averageFitnesses.Add(population.fitnesses.Average());

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
        float[] fitnesses = new float[population.genotypes.Length];
        List<IEnumerator> assessors = new List<IEnumerator>();

        Physics.autoSimulation = false;

        for (int i = 0; i < population.genotypes.Length; i++)
        {
            Phenotype phenotype = PhenotypeBuilder.ConstructPhenotype(population.genotypes[i]);
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
                    fitnesses[index] = fitnessScore;
                    UnityEngine.Object.Destroy(phenotype.gameObject);
                }
                ));
            }
            else
            {
                fitnesses[i] = 0f;
                UnityEngine.Object.Destroy(phenotype.gameObject);
            }
        }

        yield return null; // Ensure the phenotypes are completely destroyed.

        Physics.autoSimulation = true;

        List<Coroutine> coroutines = new List<Coroutine>();
        foreach (IEnumerator assessor in assessors)
            coroutines.Add(StartCoroutine(assessor));

        // Wait for all coroutines to finish.
        foreach (Coroutine coroutine in coroutines)
            yield return coroutine;

        yield return null; // Ensure the phenotypes are completely destroyed.

        population.fitnesses = fitnesses;
    }

    private Population SelectSurvivors(Population population, int maxSurvivors)
    {
        Array.Sort(population.fitnesses, population.genotypes);

        List<(Genotype, float)> survivors = new List<(Genotype, float)>();
        for (int i = population.genotypes.Length - 1; survivors.Count < maxSurvivors && i >= 0; i--)
        {
            if (population.fitnesses[i] > 0)
                survivors.Add((population.genotypes[i], population.fitnesses[i]));
        }

        Genotype[] genotypes = new Genotype[survivors.Count];
        float[] fitnesses = new float[survivors.Count];
        for (int i = 0; i < survivors.Count; i++)
        {
            genotypes[i] = survivors[i].Item1;
            fitnesses[i] = survivors[i].Item2;
        }

        Population survivorPopulation = new Population(genotypes);
        survivorPopulation.fitnesses = fitnesses;

        return survivorPopulation;
    }

    private Population ProduceNextGeneration(int populationSize, int maxChildren, Population seedPopulation)
    {
        List<Genotype> nextGeneration = seedPopulation.genotypes.ToList();

        float totalFitness = seedPopulation.fitnesses.Sum();
        if (totalFitness > 0)
        {
            for (int j = 0; j < maxChildren; j++)
            {
                // Choose parents, weighted by relative fitness.
                Genotype? parent1 = null;
                Genotype? parent2 = null;
                float parent1Value = UnityEngine.Random.value;
                float parent2Value = UnityEngine.Random.value;

                float wheelLocation = 0f;
                for (int k = 0; k < seedPopulation.genotypes.Length; k++)
                {
                    float fitnessRatio = seedPopulation.fitnesses[k] / totalFitness;
                    float segmentStart = wheelLocation;
                    float segmentEnd = wheelLocation + fitnessRatio;
                    if (parent1Value >= segmentStart && parent1Value <= segmentEnd)
                        parent1 = seedPopulation.genotypes[k];
                    if (parent2Value >= segmentStart && parent2Value <= segmentEnd)
                        parent2 = seedPopulation.genotypes[k];
                    wheelLocation = segmentEnd;
                }

                nextGeneration.Add(Reproduction.CreateOffspring((Genotype)parent1, (Genotype)parent2));
            }
        }

        // Ensure population size stays the same - fill gaps with randomly generated genotypes if necessary.
        int populationDiscrepancy = populationSize - nextGeneration.Count;
        for (int j = 0; j < populationDiscrepancy; j++)
            nextGeneration.Add(Genotype.CreateRandom());
        if (populationDiscrepancy > 0)
            Debug.Log("Added " + populationDiscrepancy + " seed genotypes to maintain population size.");

        return new Population(nextGeneration.ToArray());
    }

    private void ParadeGenotype(Genotype genotype, AssessmentFunction assessmentFunction)
    {
        timeScale = 1f;
        Phenotype phenotype = PhenotypeBuilder.ConstructPhenotype(genotype);
        Debug.Break();
        StartCoroutine(assessmentFunction(phenotype, bestFitness, fitnessScore => { Debug.Log("Best Genotype's Fitness: " + fitnessScore); }));
    }
}
