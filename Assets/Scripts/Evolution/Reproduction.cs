using UnityEngine;

public static class Reproduction
{
    public static Genotype CreateRandomisedOffspring(Genotype parent1, Genotype parent2)
    {
        return CreateOffspringWithChance(
            parent1,
            parent2,
            ParameterManager.Instance.Recombination.AsexualProbability,
            ParameterManager.Instance.Recombination.GraftingProbability,
            ParameterManager.Instance.Recombination.CrossoverProbability
        );
    }

    // Use manual probabilities.
    public static Genotype CreateOffspringWithChance(Genotype parent1, Genotype parent2, float asexualChance, float graftingChance, float crossoverChance)
    {
        // If morphologies are locked, disable grafting.
        // This is because grafting can alter morphologies even if
        // both parents have the same morphology.
        graftingChance *= ParameterManager.Instance.Reproduction.LockMorphologies ? 0f : 1f;

        // Choose reproduction method with set probability.
        float methodChoice = Random.Range(
            0f,
            crossoverChance
            + graftingChance
            + asexualChance
        );

        // Recombination.
        Genotype child;
        if (methodChoice <= crossoverChance)
            child = Recombination.Crossover(parent1, parent2);
        else if (methodChoice <= crossoverChance + graftingChance)
            child = Recombination.Grafting(parent1, parent2);
        else
            child = Recombination.Asexual(parent1);

        // Mutation.
        int numberOfMutations = Mathf.Max(0, Mathf.RoundToInt(ParameterManager.Instance.Mutation.MutationRate + (Utilities.RandomGaussian() * ParameterManager.Instance.Mutation.MutationRate)));
        if (numberOfMutations > 0)
        {
            // Always add a new disconnected limb node if mutations are incoming.
            // This potential node will be pruned later if a connection does not mutate to point to it.
            child = Mutation.AddPotentialLimbNode(child);

            // Apply mutations.
            // Mutations are compounding.
            for (int i = 0; i < numberOfMutations; i++)
            {
                child = Mutation.Mutate(child);
            }
        }

        child.PruneUnconnectedLimbNodes();
        child.Validate();
        return child;
    }
}
