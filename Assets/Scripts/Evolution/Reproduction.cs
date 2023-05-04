using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class Reproduction
{
    // Use set parameters.
    public static Genotype CreateOffspring(Genotype parent1, Genotype parent2)
    {
        return CreateOffspring(
            parent1,
            parent2,
            ReproductionParameters.AsexualProbability,
            ReproductionParameters.GraftingProbability,
            ReproductionParameters.CrossoverProbability
        );
    }

    // Use manual probabilities.
    public static Genotype CreateOffspring(Genotype parent1, Genotype parent2, float asexualChance, float graftingChance, float crossoverChance)
    {
        Genotype child;

        // Choose reproduction method with set probability.
        float methodChoice = Random.Range(
            0f,
            crossoverChance
            + graftingChance
            + asexualChance
        );

        // Apply reproduction method.
        if (methodChoice <= crossoverChance)
            child = Crossover(parent1, parent2);
        else if (methodChoice <= crossoverChance + graftingChance)
            child = Grafting(parent1, parent2);
        else
            child = Asexual(parent1);

        child.PruneUnconnectedLimbNodes();
        child.FixBrokenNeuralConnections();

        // Apply mutations.
        child = Mutation.Mutate(child);

        child.PruneUnconnectedLimbNodes();
        child.FixBrokenNeuralConnections();

        child.Validate();

        return child;
    }

    private static Genotype Crossover(Genotype parent1, Genotype parent2)
    {
        List<LimbNode> crossedOverLimbNodes = new List<LimbNode>();

        // Copy nodes from a parent to the child, alternating the donor parent after each specified crossover interval.
        bool copyFromParent1 = parent1.LimbNodes.Count < parent2.LimbNodes.Count;
        for (int i = 0; i < Mathf.Max(parent1.LimbNodes.Count, parent2.LimbNodes.Count); i++)
        {
            if (i > 0 && i % ReproductionParameters.CrossoverInterval == 0)
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
            ConcatLineage(parent1.Lineage, parent1.Id + " X " + parent2.Id),
            parent1.BrainNeuronDefinitions,
            newLimbNodes
        );
    }

    private static Genotype Grafting(Genotype recipient, Genotype donor)
    {
        List<LimbNode> nodeRow = recipient.LimbNodes.Select(recipientNode => recipientNode.CreateCopy(null)).ToList();

        // Randomly choose a source connection from the recipient side.
        List<int> graftCandidateIds = recipient.LimbNodes.Select((node, i) => node.Connections.Count > 0 ? i : -1).Where(i => i != -1).ToList();
        int graftCandidateId = graftCandidateIds[Random.Range(0, graftCandidateIds.Count)];
        int connectionId = Random.Range(0, recipient.LimbNodes[graftCandidateId].Connections.Count);

        // Randomly choose a destination node from the donor side.
        List<LimbNode> donorNodes = donor.LimbNodes.ToList();
        int graftDestinationId = Random.Range(recipient.LimbNodes.Count, recipient.LimbNodes.Count + donorNodes.Count);

        // Graft the source connection to the destination node.
        LimbNode graftCandidate = recipient.LimbNodes[graftCandidateId];
        nodeRow[graftCandidateId] = graftCandidate.CreateCopy(graftCandidate.Connections.Select((connection, i) =>
        {
            if (i == connectionId)
                return graftCandidate.Connections[i].CreateCopy(graftDestinationId);
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
            ConcatLineage(recipient.Lineage, recipient.Id + " <- " + donor.Id),
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
                newGenotype.Lineage,
                newGenotype.BrainNeuronDefinitions,
                newGenotype.LimbNodes.Take(GenotypeParameters.MaxLimbNodes).Select(node => // Remove connections that point out of bounds.
                    node.CreateCopy(node.Connections.Where(c => c.ChildNodeId < GenotypeParameters.MaxLimbNodes).ToList())).ToList()
            );
        }
        else
            return newGenotype;
    }

    private static Genotype Asexual(Genotype parent1)
    {
        return Genotype.Construct
        (
            null,
            ConcatLineage(parent1.Lineage, parent1.Id + " +"),
            parent1.BrainNeuronDefinitions,
            parent1.LimbNodes
        );
    }

    private static List<string> ConcatLineage(IList<string> lineage, string nextEvent)
    {
        return lineage.ToList().Concat(new List<string>() { nextEvent }).ToList();
    }


}
