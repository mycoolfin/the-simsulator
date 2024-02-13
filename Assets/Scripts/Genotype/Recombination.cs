using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class Recombination
{
    public static Genotype Crossover(Genotype parent1, Genotype parent2)
    {
        List<LimbNode> crossedOverLimbNodes = new();

        // Copy nodes from a parent to the child, alternating the donor parent after each specified crossover interval.
        bool copyFromParent1 = parent1.LimbNodes.Count < parent2.LimbNodes.Count;
        for (int i = 0; i < Mathf.Max(parent1.LimbNodes.Count, parent2.LimbNodes.Count); i++)
        {
            if (i > 0 && i % RecombinationParameters.CrossoverInterval == 0)
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
            parent1.BrainNeuronDefinitions,
            newLimbNodes
        );
    }

    public static Genotype Grafting(Genotype recipient, Genotype donor)
    {
        List<LimbNode> nodeRow = recipient.LimbNodes.Select(recipientNode => recipientNode.CreateCopy(null)).ToList();

        // Randomly choose a source connection from the recipient side.
        List<int> graftCandidateIds = recipient.LimbNodes.Select((node, i) => node.Connections.Count > 0 ? i : -1).Where(i => i != -1).ToList();
        int recipientNodeIndex = Mathf.RoundToInt(Mathf.Lerp(0, graftCandidateIds.Count - 1, Random.value));
        int graftCandidateId = graftCandidateIds[recipientNodeIndex];
        int recipientConnectionIndex = Mathf.RoundToInt(Mathf.Lerp(0, recipient.LimbNodes[graftCandidateId].Connections.Count - 1, Random.value));

        // Randomly choose a destination node from the donor side.
        List<LimbNode> donorNodes = donor.LimbNodes.ToList();
        int donorNodeIndex = Mathf.RoundToInt(Mathf.Lerp(recipient.LimbNodes.Count, recipient.LimbNodes.Count + donorNodes.Count - 1, Random.value));

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
                newGenotype.BrainNeuronDefinitions,
                newGenotype.LimbNodes.Take(GenotypeParameters.MaxLimbNodes).Select(node => // Remove connections that point out of bounds.
                    node.CreateCopy(node.Connections.Where(c => c.ChildNodeId < GenotypeParameters.MaxLimbNodes).ToList())).ToList()
            );
        }
        else
            return newGenotype;
    }

    public static Genotype Asexual(Genotype parent)
    {
        return Genotype.Construct
        (
            null,
            parent.BrainNeuronDefinitions,
            parent.LimbNodes
        );
    }
}
