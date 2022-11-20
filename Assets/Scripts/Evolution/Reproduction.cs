using System.Linq;
using System.Collections.Generic;
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
            child = Crossover(parent1, parent2);
        else if (methodChoice <= ReproductionParameters.CrossoverProbability + ReproductionParameters.GraftingProbability)
            child = Grafting(parent1, parent2);
        else
            child = parent1;

        // Apply mutations.
        child = Mutation.Mutate(child);

        return child;
    }

    private static Genotype Crossover(Genotype parent1, Genotype parent2)
    {
        List<LimbNode> newLimbNodes = new List<LimbNode>();

        // Copy nodes from a parent to the child, alternating the donor parent after each specified crossover interval.
        bool copyFromParent1 = parent1.limbNodes.Length < parent2.limbNodes.Length;
        for (int i = 0; i < Mathf.Max(parent1.limbNodes.Length, parent2.limbNodes.Length); i++)
        {
            if (i > 0 && i % ReproductionParameters.CrossoverInterval == 0)
                copyFromParent1 = !copyFromParent1;

            Genotype copySource = copyFromParent1 ? parent1 : parent2;
            if (copySource.limbNodes.Length > i)
                newLimbNodes.Add(copySource.limbNodes[i]);
            else
                break;
        }

        // Reassign node connections that now point out of bounds.
        foreach (LimbNode node in newLimbNodes)
        {
            for (int i = 0; i < node.connections.Length; i++)
            {
                if (node.connections[i].childNodeId >= newLimbNodes.Count)
                    node.connections[i] = new(
                        Random.Range(0, newLimbNodes.Count),
                        node.connections[i].parentFace,
                        node.connections[i].position,
                        node.connections[i].orientation,
                        node.connections[i].scale,
                        node.connections[i].reflection,
                        node.connections[i].terminalOnly
                    );
            }
        }

        return Genotype.RemoveUnconnectedNodes(new Genotype((NeuronDefinition[])parent1.brainNeuronDefinitions.Clone(), newLimbNodes.ToArray()));
    }

    private static Genotype Grafting(Genotype recipient, Genotype donor)
    {
        int numRecipientNodes = recipient.limbNodes.Length;
        int numDonorNodes = donor.limbNodes.Length;
        LimbNode[] nodeRow = new LimbNode[numRecipientNodes + numDonorNodes];

        for (int i = 0; i < numRecipientNodes; i++)
            nodeRow[i] = recipient.limbNodes[i].CreateCopy(null);

        // Randomly choose a source connection from the recipient side.
        int[] graftCandidateIds = nodeRow.Take(numRecipientNodes).Select((node, i) => node.connections.Length > 0 ? i : -1).Where(i => i != -1).ToArray();
        int graftCandidateId = graftCandidateIds[Random.Range(0, graftCandidateIds.Length)];
        int connectionId = Random.Range(0, nodeRow[graftCandidateId].connections.Length);

        // Randomly choose a destination node from the donor side.
        int graftDestination = Random.Range(numRecipientNodes, numDonorNodes);

        // Graft the source connection to the destination node.
        LimbNode graftCandidate = recipient.limbNodes[graftCandidateId];
        nodeRow[graftCandidateId] = graftCandidate.CreateCopy(graftCandidate.connections.Select((connection, i) =>
        {
            if (i == connectionId)
                return graftCandidate.connections[i].CreateCopy(graftDestination);
            else
                return graftCandidate.connections[i];
        }).ToArray());

        // Adjust nodes copied from graft donor to account for new length of node list.
        int idOffset = nodeRow.Length - numDonorNodes;
        for (int i = 0; i < numDonorNodes; i++)
        {
            LimbNode donorNode = donor.limbNodes[i];
            nodeRow[i + numRecipientNodes] = donorNode.CreateCopy(donorNode.connections.Select(connection =>
            {
                int adjustedChildNodeId = connection.childNodeId + numRecipientNodes;
                return connection.CreateCopy(adjustedChildNodeId);
            }).ToArray());
        }

        return Genotype.RemoveUnconnectedNodes(new Genotype((NeuronDefinition[])recipient.brainNeuronDefinitions.Clone(), nodeRow));
    }
}
