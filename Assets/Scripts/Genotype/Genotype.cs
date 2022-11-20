using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Genotype
{
    public readonly NeuronDefinition[] brainNeuronDefinitions;
    public readonly LimbNode[] limbNodes;

    public Genotype(NeuronDefinition[] brainNeuronDefinitions, LimbNode[] limbNodes)
    {
        if (limbNodes == null || limbNodes.Length == 0)
            throw new System.ArgumentException("Genotype cannot be specified without limbs");

        this.limbNodes = limbNodes;
        this.brainNeuronDefinitions = brainNeuronDefinitions == null ? new NeuronDefinition[0] : brainNeuronDefinitions;
    }

    public static Genotype CreateRandom()
    {
        int numberOfLimbNodes = Random.Range(GenotypeGenerationParameters.MinLimbNodes, GenotypeGenerationParameters.MaxLimbNodes);
        LimbNode[] limbNodes = new LimbNode[numberOfLimbNodes];

        for (int i = 0; i < numberOfLimbNodes; i++)
        {
            List<int> connectedNodeIds = new List<int>();
            for (int attemptNum = 0; attemptNum < GenotypeGenerationParameters.MaxConnectionAttempts; attemptNum++)
            {
                bool attemptSuccess;
                if (i == 0 && attemptNum == 0) // Guarantee at least one connection.
                    attemptSuccess = true;
                else
                    attemptSuccess = Random.Range(0f, 1f) > GenotypeGenerationParameters.ConnectionAttemptChance;
                if (attemptSuccess)
                    connectedNodeIds.Add(Random.Range(0, limbNodes.Length));
                else
                    break;
            }
            LimbConnection[] limbConnections = new LimbConnection[connectedNodeIds.Count];
            for (int j = 0; j < limbConnections.Length; j++)
                limbConnections[j] = LimbConnection.CreateRandom(connectedNodeIds[j]);
            limbNodes[i] = LimbNode.CreateRandom(limbConnections);
        }

        int numberOfBrainNeurons = Random.Range(GenotypeGenerationParameters.MinBrainNeurons, GenotypeGenerationParameters.MaxBrainNeurons);
        NeuronDefinition[] brainNeuronDefinitions = new NeuronDefinition[numberOfBrainNeurons];
        for (int i = 0; i < numberOfBrainNeurons; i++)
            brainNeuronDefinitions[i] = NeuronDefinition.CreateRandom();

        return RemoveUnconnectedNodes(new Genotype(brainNeuronDefinitions, limbNodes));
    }

    public static Genotype RemoveUnconnectedNodes(Genotype genotype)
    {
        LimbNode root = genotype.limbNodes[0];
        List<int> visitedNodeIds = RecursivelyTraverseLimbNodes(genotype.limbNodes, null, 0);
        List<int> unconnectedNodeIds = new List<int>();
        for (int i = 0; i < genotype.limbNodes.Length; i++)
            if (!visitedNodeIds.Contains(i))
                unconnectedNodeIds.Add(i);

        LimbNode[] newLimbNodes = new LimbNode[visitedNodeIds.Count];
        for (int i = 0; i < visitedNodeIds.Count; i++)
        {
            newLimbNodes[i] = genotype.limbNodes[visitedNodeIds[i]];
            for (int j = 0; j < newLimbNodes[i].connections.Length; j++)
            {
                LimbConnection oldConnection = newLimbNodes[i].connections[j];
                int precedingUnconnectedNodesCount = unconnectedNodeIds.Count(id => id < oldConnection.childNodeId);
                newLimbNodes[i].connections[j] = oldConnection.CreateCopy(oldConnection.childNodeId - precedingUnconnectedNodesCount);
            }
        }

        return new Genotype(genotype.brainNeuronDefinitions, newLimbNodes);
    }

    private static List<int> RecursivelyTraverseLimbNodes(LimbNode[] limbNodes, List<int> visitedNodeIds, int nodeId)
    {
        if (visitedNodeIds == null)
            visitedNodeIds = new List<int>();

        if (!visitedNodeIds.Contains(nodeId))
        {
            visitedNodeIds.Add(nodeId);

            foreach (LimbConnection connection in limbNodes[nodeId].connections)
                RecursivelyTraverseLimbNodes(limbNodes, visitedNodeIds, connection.childNodeId);
        }

        return visitedNodeIds;
    }
}
