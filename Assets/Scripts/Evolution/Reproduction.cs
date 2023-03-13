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
        bool copyFromParent1 = parent1.limbNodes.Count < parent2.limbNodes.Count;
        for (int i = 0; i < Mathf.Max(parent1.limbNodes.Count, parent2.limbNodes.Count); i++)
        {
            if (i > 0 && i % ReproductionParameters.CrossoverInterval == 0)
                copyFromParent1 = !copyFromParent1;

            Genotype copySource = copyFromParent1 ? parent1 : parent2;
            if (copySource.limbNodes.Count > i)
                crossedOverLimbNodes.Add(copySource.limbNodes[i]);
            else
                break;
        }

        // Reassign node connections that now point out of bounds.
        ReadOnlyCollection<LimbNode> newLimbNodes = crossedOverLimbNodes.Select(node =>
        {
            ReadOnlyCollection<LimbConnection> newConnections = node.connections.Select(connection =>
            {
                if (connection.childNodeId >= crossedOverLimbNodes.Count)
                    return new(
                        Random.Range(0, crossedOverLimbNodes.Count),
                        connection.parentFace,
                        connection.position,
                        connection.orientation,
                        connection.scale,
                        connection.reflection,
                        connection.terminalOnly
                    );
                else
                    return connection;
            }).ToList().AsReadOnly();

            return node.CreateCopy(newConnections);
        }).ToList().AsReadOnly();

        return new Genotype
        (
            null,
            ConcatLineage(parent1.lineage, "G" + parent1.id + " X " + "G" + parent2.id),
            parent1.brainNeuronDefinitions,
            newLimbNodes
        );
    }

    private static Genotype Grafting(Genotype recipient, Genotype donor)
    {
        List<LimbNode> nodeRow = recipient.limbNodes.Select(recipientNode => recipientNode.CreateCopy(null)).ToList();

        // Randomly choose a source connection from the recipient side.
        List<int> graftCandidateIds = recipient.limbNodes.Select((node, i) => node.connections.Count > 0 ? i : -1).Where(i => i != -1).ToList();
        int graftCandidateId = graftCandidateIds[Random.Range(0, graftCandidateIds.Count)];
        int connectionId = Random.Range(0, recipient.limbNodes[graftCandidateId].connections.Count);

        // Randomly choose a destination node from the donor side.
        int graftDestinationId = Random.Range(recipient.limbNodes.Count, recipient.limbNodes.Count + donor.limbNodes.Count);

        // Graft the source connection to the destination node.
        LimbNode graftCandidate = recipient.limbNodes[graftCandidateId];
        nodeRow[graftCandidateId] = graftCandidate.CreateCopy(graftCandidate.connections.Select((connection, i) =>
        {
            if (i == connectionId)
                return graftCandidate.connections[i].CreateCopy(graftDestinationId);
            else
                return graftCandidate.connections[i];
        }).ToList().AsReadOnly());

        // Adjust nodes copied from graft donor to account for new length of node list.
        foreach (LimbNode donorNode in donor.limbNodes)
        {
            nodeRow.Add(donorNode.CreateCopy(donorNode.connections.Select(connection =>
            {
                int adjustedChildNodeId = connection.childNodeId + recipient.limbNodes.Count;
                return connection.CreateCopy(adjustedChildNodeId);
            }).ToList().AsReadOnly()));
        }

        return new Genotype
        (
            null,
            ConcatLineage(recipient.lineage, "G" + recipient.id + " <- " + "G" + donor.id),
            recipient.brainNeuronDefinitions,
            nodeRow.AsReadOnly()
        );
    }

    private static Genotype Asexual(Genotype parent1)
    {
        return new Genotype
        (
            null,
            ConcatLineage(parent1.lineage, "G" + parent1.id + " +"),
            parent1.brainNeuronDefinitions,
            parent1.limbNodes
        );
    }

    private static ReadOnlyCollection<string> ConcatLineage(ReadOnlyCollection<string> lineage, string nextEvent)
    {
        return lineage.ToList().Concat(new List<string>() { nextEvent }).ToList().AsReadOnly();
    }
}