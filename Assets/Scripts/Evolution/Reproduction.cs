using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class Reproduction
{
    public static Genotype CreateRandomisedOffspring(Genotype parent1, Genotype parent2)
    {
        return CreateOffspringWithChance(
            parent1,
            parent2,
            ReproductionParameters.AsexualProbability,
            ReproductionParameters.GraftingProbability,
            ReproductionParameters.CrossoverProbability
        );
    }

    // Use manual probabilities.
    public static Genotype CreateOffspringWithChance(Genotype parent1, Genotype parent2, float asexualChance, float graftingChance, float crossoverChance)
    {
        // Choose reproduction method with set probability.
        float methodChoice = Random.Range(
            0f,
            crossoverChance
            + graftingChance
            + asexualChance
        );

        // Generate and apply a recombination operation.
        RecombinationOperation recombinationOperation;
        if (methodChoice <= crossoverChance)
            recombinationOperation = RecombinationOperation.CreateCrossover(parent2);
        else if (methodChoice <= crossoverChance + graftingChance)
            recombinationOperation = RecombinationOperation.CreateGrafting(parent2);
        else
            recombinationOperation = RecombinationOperation.CreateAsexual();
        Genotype child = ApplyRecombinationOperation(parent1, recombinationOperation);

        // Determine the number of mutations.
        List<MutationOperation> mutationOperations = new();
        int numberOfMutations = Mathf.Max(0, Mathf.RoundToInt(MutationParameters.MutationRate + (Utilities.RandomGaussian() * MutationParameters.MutationRate)));

        // Always add a new disconnected limb node if mutations are incoming.
        // This potential node will be pruned later if a connection does not mutate to point to it.
        if (numberOfMutations > 0)
        {
            MutationOperation newLimbNodeOperation = Mutation.AddPotentialLimbNode(child);
            if (!newLimbNodeOperation.invalid)
            {
                mutationOperations.Add(newLimbNodeOperation);
                child = Mutation.ParseMutationFunction(newLimbNodeOperation)(child);
            }
        }

        // Generate and apply mutation operations.
        for (int i = 0; i < numberOfMutations; i++) // Mutations are compounding.
        {
            MutationOperation mutationOperation = Mutation.CreateRandomMutation(child);
            if (!mutationOperation.invalid)
            {
                mutationOperations.Add(mutationOperation);
                child = Mutation.ParseMutationFunction(mutationOperation)(child);
            }
        }

        child.PruneUnconnectedLimbNodes();

        // Update ancestry.
        Ancestry childAncestry = child.Ancestry;
        childAncestry.RecordOffspringSpecification(new(recombinationOperation, mutationOperations));
        child = Genotype.Construct(child.Id, childAncestry, child.BrainNeuronDefinitions, child.LimbNodes);

        child.Validate();

        return child;
    }

    public static Genotype CreateOffspringFromSpecification(Genotype genotype, OffspringSpecification offspringSpecification, string childName)
    {
        Genotype child = ApplyRecombinationOperation(genotype, offspringSpecification.RecombinationOperation);
        foreach (MutationOperation mutationOperation in offspringSpecification.MutationOperations)
            child = Mutation.ParseMutationFunction(mutationOperation)(child);

        child.PruneUnconnectedLimbNodes();

        child.Validate();

        child = Genotype.Construct(childName, child.Ancestry, child.BrainNeuronDefinitions, child.LimbNodes);

        return child;
    }

    private static Genotype ApplyRecombinationOperation(Genotype genotype, RecombinationOperation recombinationOperation)
    {
        Genotype child;
        switch (recombinationOperation.Type)
        {
            case RecombinationOperationType.Crossover:
                child = Crossover
                (
                    genotype,
                    recombinationOperation.Mate.ToGenotype(),
                    recombinationOperation.CrossoverInterval
                );
                break;
            case RecombinationOperationType.Grafting:
                child = Grafting
                (
                    genotype,
                    recombinationOperation.Mate.ToGenotype(),
                    recombinationOperation.RecipientNodeChoice,
                    recombinationOperation.RecipientConnectionChoice,
                    recombinationOperation.DonorNodeChoice
                );
                break;
            case RecombinationOperationType.Asexual:
                child = Asexual(genotype);
                break;
            default:
                throw new System.ArgumentOutOfRangeException("Unknown recombination operation type '" + recombinationOperation.Type + "'.");
        }
        return child;
    }

    private static Genotype Crossover(Genotype parent1, Genotype parent2, int crossoverInterval)
    {
        List<LimbNode> crossedOverLimbNodes = new List<LimbNode>();

        // Copy nodes from a parent to the child, alternating the donor parent after each specified crossover interval.
        bool copyFromParent1 = parent1.LimbNodes.Count < parent2.LimbNodes.Count;
        for (int i = 0; i < Mathf.Max(parent1.LimbNodes.Count, parent2.LimbNodes.Count); i++)
        {
            if (i > 0 && i % crossoverInterval == 0)
                copyFromParent1 = !copyFromParent1;

            Genotype copySource = copyFromParent1 ? parent1 : parent2;
            if (copySource.LimbNodes.Count > i)
                crossedOverLimbNodes.Add(copySource.LimbNodes[i]);
            else
                break;
        }

        // Reassign node connections that now point out of bounds.
        List<LimbNode> newLimbNodes = crossedOverLimbNodes.Select(node =>
        {
            List<LimbConnection> newConnections = node.Connections.Select(connection =>
            {
                if (connection.ChildNodeId >= crossedOverLimbNodes.Count)
                    return connection.CreateCopy(Random.Range(0, crossedOverLimbNodes.Count));
                else
                    return connection;
            }).ToList();

            return node.CreateCopy(newConnections);
        }).ToList();

        return Genotype.Construct
        (
            null,
            parent1.Ancestry,
            parent1.BrainNeuronDefinitions,
            newLimbNodes
        );
    }

    private static Genotype Grafting(Genotype recipient, Genotype donor, float recipientNodeChoice, float recipientConnectionChoice, float donorNodeChoice)
    {
        List<LimbNode> nodeRow = recipient.LimbNodes.Select(recipientNode => recipientNode.CreateCopy(null)).ToList();

        // Randomly choose a source connection from the recipient side.
        List<int> graftCandidateIds = recipient.LimbNodes.Select((node, i) => node.Connections.Count > 0 ? i : -1).Where(i => i != -1).ToList();
        int recipientNodeIndex = Mathf.RoundToInt(Mathf.Lerp(0, graftCandidateIds.Count - 1, recipientNodeChoice));
        int graftCandidateId = graftCandidateIds[recipientNodeIndex];
        int recipientConnectionIndex = Mathf.RoundToInt(Mathf.Lerp(0, recipient.LimbNodes[graftCandidateId].Connections.Count - 1, recipientConnectionChoice));

        // Randomly choose a destination node from the donor side.
        List<LimbNode> donorNodes = donor.LimbNodes.ToList();
        int donorNodeIndex = Mathf.RoundToInt(Mathf.Lerp(recipient.LimbNodes.Count, recipient.LimbNodes.Count + donorNodes.Count - 1, donorNodeChoice));

        // Graft the source connection to the destination node.
        LimbNode graftCandidate = recipient.LimbNodes[graftCandidateId];
        nodeRow[graftCandidateId] = graftCandidate.CreateCopy(graftCandidate.Connections.Select((connection, i) =>
        {
            if (i == recipientConnectionIndex)
                return graftCandidate.Connections[i].CreateCopy(donorNodeIndex);
            else
                return graftCandidate.Connections[i];
        }).ToList());

        // Adjust nodes copied from graft donor to account for new length of node list.
        int totalNodes = recipient.LimbNodes.Count + donorNodes.Count;
        foreach (LimbNode donorNode in donorNodes)
        {
            List<LimbConnection> newConnections = new();
            foreach (LimbConnection c in donorNode.Connections)
            {
                int adjustedChildNodeId = c.ChildNodeId + recipient.LimbNodes.Count;
                newConnections.Add(c.CreateCopy(adjustedChildNodeId));
            };
            LimbNode newDonorNode = donorNode.CreateCopy(newConnections);
            nodeRow.Add(newDonorNode);
        }

        Genotype newGenotype = Genotype.Construct
        (
            null,
            recipient.Ancestry,
            recipient.BrainNeuronDefinitions,
            nodeRow
        );

        // Truncate new limb node set if too large.
        bool tooManyLimbNodes = newGenotype.LimbNodes.Count > GenotypeParameters.MaxLimbNodes;
        if (tooManyLimbNodes)
        {
            return Genotype.Construct
            (
                newGenotype.Id,
                newGenotype.Ancestry,
                newGenotype.BrainNeuronDefinitions,
                newGenotype.LimbNodes.Take(GenotypeParameters.MaxLimbNodes).Select(node => // Remove connections that point out of bounds.
                    node.CreateCopy(node.Connections.Where(c => c.ChildNodeId < GenotypeParameters.MaxLimbNodes).ToList())).ToList()
            );
        }
        else
            return newGenotype;
    }

    private static Genotype Asexual(Genotype parent)
    {
        return Genotype.Construct
        (
            null,
            parent.Ancestry,
            parent.BrainNeuronDefinitions,
            parent.LimbNodes
        );
    }
}
