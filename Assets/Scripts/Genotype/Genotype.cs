using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[System.Serializable]
public struct Genotype
{
    static int latestId = 1;

    public readonly int id;
    public readonly ReadOnlyCollection<string> lineage;
    public readonly ReadOnlyCollection<NeuronDefinition> brainNeuronDefinitions;
    public readonly ReadOnlyCollection<LimbNode> limbNodes;

    public Genotype(int? id, ReadOnlyCollection<string> lineage, ReadOnlyCollection<NeuronDefinition> brainNeuronDefinitions, ReadOnlyCollection<LimbNode> limbNodes)
    {
        this.id = id != null ? (int)id : latestId++;
        this.lineage = lineage == null ? (new List<string> { "G" + this.id + " created" }).AsReadOnly() : lineage;
        this.limbNodes = RemoveUnconnectedLimbNodes(limbNodes);
        this.brainNeuronDefinitions = brainNeuronDefinitions == null ? new List<NeuronDefinition>().AsReadOnly() : brainNeuronDefinitions;
    }

    public static Genotype CreateRandom()
    {
        int numberOfLimbNodes = Random.Range(GenotypeGenerationParameters.MinLimbNodes, GenotypeGenerationParameters.MaxLimbNodes);
        List<LimbNode> limbNodes = new List<LimbNode>();

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
                    connectedNodeIds.Add(Random.Range(0, numberOfLimbNodes));
                else
                    break;
            }
            ReadOnlyCollection<LimbConnection> limbConnections = connectedNodeIds.Select(id => LimbConnection.CreateRandom(id)).ToList().AsReadOnly();
            limbNodes.Add(LimbNode.CreateRandom(limbConnections));
        }

        int numberOfBrainNeurons = Random.Range(GenotypeGenerationParameters.MinBrainNeurons, GenotypeGenerationParameters.MaxBrainNeurons);
        List<NeuronDefinition> brainNeuronDefinitions = new List<NeuronDefinition>();
        for (int i = 0; i < numberOfBrainNeurons; i++)
            brainNeuronDefinitions.Add(NeuronDefinition.CreateRandom());

        return new Genotype(null, null, brainNeuronDefinitions.AsReadOnly(), limbNodes.AsReadOnly());
    }

    public static ReadOnlyCollection<LimbNode> RemoveUnconnectedLimbNodes(ReadOnlyCollection<LimbNode> limbNodes)
    {
        LimbNode root = limbNodes[0];
        List<int> visitedNodeIds = RecursivelyTraverseLimbNodes(limbNodes, null, 0);
        List<int> unconnectedNodeIds = new List<int>();
        for (int i = 0; i < limbNodes.Count; i++)
            if (!visitedNodeIds.Contains(i))
                unconnectedNodeIds.Add(i);

        List<LimbNode> newLimbNodes = new List<LimbNode>();
        for (int i = 0; i < visitedNodeIds.Count; i++)
        {
            ReadOnlyCollection<LimbConnection> connections = limbNodes[visitedNodeIds[i]].connections
            .Select(oldConnection =>
            {
                int precedingUnconnectedNodesCount = unconnectedNodeIds.Count(id => id < oldConnection.childNodeId);
                return oldConnection.CreateCopy(oldConnection.childNodeId - precedingUnconnectedNodesCount);
            }).ToList().AsReadOnly();
            newLimbNodes.Add(limbNodes[visitedNodeIds[i]].CreateCopy(connections));
        }

        return newLimbNodes.AsReadOnly();
    }

    private static List<int> RecursivelyTraverseLimbNodes(ReadOnlyCollection<LimbNode> limbNodes, List<int> visitedNodeIds, int nodeId)
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
