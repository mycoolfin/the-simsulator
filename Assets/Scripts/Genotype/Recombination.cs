using System;
using UnityEngine;

public enum RecombinationOperationType
{
    Asexual,
    Crossover,
    Grafting
}

[Serializable]
public class RecombinationOperation
{
    [SerializeField] private RecombinationOperationType type;
    [SerializeField] private GenotypeWithoutAncestry mate;
    [SerializeField] private int crossoverInterval;
    [SerializeField] private float recipientNodeChoice;
    [SerializeField] private float recipientConnectionChoice;
    [SerializeField] private float donorNodeChoice;

    public RecombinationOperationType Type => type;
    public GenotypeWithoutAncestry Mate => mate;
    public int CrossoverInterval => crossoverInterval;
    public float RecipientNodeChoice => recipientNodeChoice;
    public float RecipientConnectionChoice => recipientConnectionChoice;
    public float DonorNodeChoice => donorNodeChoice;

    private RecombinationOperation(RecombinationOperationType type, GenotypeWithoutAncestry mate, int crossoverInterval,
        float recipientNodeChoice, float recipientConnectionChoice, float donorNodeChoice)
    {
        this.type = type;
        this.mate = mate;
        this.crossoverInterval = crossoverInterval;
        this.recipientNodeChoice = recipientNodeChoice;
        this.recipientConnectionChoice = recipientConnectionChoice;
        this.donorNodeChoice = donorNodeChoice;
    }

    public static RecombinationOperation CreateAsexual()
    {
        return new(RecombinationOperationType.Asexual, null, -1, -1, -1, -1);
    }

    public static RecombinationOperation CreateCrossover(Genotype mate)
    {
        return new(RecombinationOperationType.Crossover, new(mate), ReproductionParameters.CrossoverInterval, -1, -1, -1);
    }

    public static RecombinationOperation CreateGrafting(Genotype mate)
    {
        // Randomly choose a destination node from the donor side.
        return new(RecombinationOperationType.Grafting, new(mate), -1, UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
    }
}
