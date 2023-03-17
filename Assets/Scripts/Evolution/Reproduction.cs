using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public static class Reproduction
{
    public static Genotype CreateOffspring(Genotype parent1, Genotype parent2)
    {
        Genotype child;

        // Choose reproduction method with set probability.
        float methodChoice = Random.Range(
            0f,
            ReproductionParameters.CrossoverProbability
            + ReproductionParameters.GraftingProbability
            + ReproductionParameters.AsexualProbability
        );

        // Apply reproduction method.
        if (methodChoice <= ReproductionParameters.CrossoverProbability)
        {
            child = Crossover(parent1, parent2);
        }
        else if (methodChoice <= ReproductionParameters.CrossoverProbability + ReproductionParameters.GraftingProbability)
        {
            child = Grafting(parent1, parent2);
        }
        else
        {
            child = Asexual(parent1);
        }

        // Apply mutations.
        child = Mutation.Mutate(child);

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

        return new Genotype
        (
            null,
            ConcatLineage(parent1.Lineage, parent1.Id + " X " + parent2.Id),
            parent1.BrainNeuronDefinitions,
            newLimbNodes
        );
    }

    private static Genotype Grafting(Genotype recipient, Genotype donor)
    {
        int canAdd = GenotypeParameters.MaxLimbNodes - recipient.LimbNodes.Count;

        if (canAdd == 0)
            return new Genotype
            (
                null,
                ConcatLineage(recipient.Lineage, recipient.Id + " <- " + donor.Id + "[FAILED]"),
                recipient.BrainNeuronDefinitions,
                recipient.LimbNodes
            );

        int willAdd = Mathf.Min(canAdd, donor.LimbNodes.Count);
        List<LimbNode> donorNodes = donor.LimbNodes.Take(willAdd).ToList();

        List<LimbNode> nodeRow = recipient.LimbNodes.Select(recipientNode => recipientNode.CreateCopy(null)).ToList();

        // Randomly choose a source connection from the recipient side.
        List<int> graftCandidateIds = recipient.LimbNodes.Select((node, i) => node.Connections.Count > 0 ? i : -1).Where(i => i != -1).ToList();
        int graftCandidateId = graftCandidateIds[Random.Range(0, graftCandidateIds.Count)];
        int connectionId = Random.Range(0, recipient.LimbNodes[graftCandidateId].Connections.Count);

        // Randomly choose a destination node from the donor side.
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
                if (adjustedChildNodeId < totalNodes)
                    newConnections.Add(c.CreateCopy(adjustedChildNodeId));
                else // Donor node list was truncated, the destination node no longer exists.
                    continue;
            };
            LimbNode newDonorNode = donorNode.CreateCopy(newConnections);
            nodeRow.Add(newDonorNode);
        }

        return new Genotype
        (
            null,
            ConcatLineage(recipient.Lineage, recipient.Id + " <- " + donor.Id),
            recipient.BrainNeuronDefinitions,
            nodeRow
        );
    }

    private static Genotype Asexual(Genotype parent1)
    {
        return new Genotype
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
