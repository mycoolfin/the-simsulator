using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[System.Serializable]
public class Genotype : IDefinition
{
    private static int latestId = 1;

    [SerializeField] private string id;
    [SerializeField] private List<string> lineage;
    [SerializeField] private List<NeuronDefinition> brainNeuronDefinitions;
    [SerializeField] private List<LimbNode> limbNodes;

    public string Id => id;
    public ReadOnlyCollection<string> Lineage => lineage.AsReadOnly();
    public ReadOnlyCollection<NeuronDefinition> BrainNeuronDefinitions => brainNeuronDefinitions.AsReadOnly();
    public ReadOnlyCollection<LimbNode> LimbNodes => limbNodes.AsReadOnly();

    public Genotype(string id, IList<string> lineage, IList<NeuronDefinition> brainNeuronDefinitions, IList<LimbNode> limbNodes)
    {
        ValidateBrainNeuronDefinitions(brainNeuronDefinitions);
        ValidateLimbNodes(limbNodes);

        this.id = id != null ? id : "G" + latestId++;
        this.lineage = lineage == null ? (new List<string> { this.id + " created" }) : lineage.ToList();
        this.limbNodes = RemoveUnconnectedLimbNodes(limbNodes);
        this.brainNeuronDefinitions = brainNeuronDefinitions == null ? new List<NeuronDefinition>() : brainNeuronDefinitions.ToList();
    }

    private static void ValidateBrainNeuronDefinitions(IList<NeuronDefinition> brainNeuronDefinitions)
    {
        bool validBrainNeuronDefinitionsCount = brainNeuronDefinitions.Count >= GenotypeParameters.MinBrainNeurons && brainNeuronDefinitions.Count <= GenotypeParameters.MaxBrainNeurons;
        if (!validBrainNeuronDefinitionsCount)
            throw new System.ArgumentException("The number of brain neuron definitions must be between " + GenotypeParameters.MinBrainNeurons
            + " and " + GenotypeParameters.MaxBrainNeurons + ". Specified: " + brainNeuronDefinitions.Count);

        foreach (NeuronDefinition neuronDefinition in brainNeuronDefinitions)
            neuronDefinition.Validate();
    }

    private static void ValidateLimbNodes(IList<LimbNode> limbNodes)
    {
        bool validLimbNodesCount = limbNodes.Count >= GenotypeParameters.MinLimbNodes && limbNodes.Count <= GenotypeParameters.MaxLimbNodes;
        if (!validLimbNodesCount)
            throw new System.ArgumentException("The number of limb nodes must be between " + GenotypeParameters.MinLimbNodes
            + " and " + GenotypeParameters.MaxLimbNodes + ". Specified: " + limbNodes.Count);

        foreach (LimbNode limbNode in limbNodes)
            limbNode.Validate();
    }

    public void Validate()
    {
        ValidateBrainNeuronDefinitions(brainNeuronDefinitions);
        ValidateLimbNodes(limbNodes);
    }

    public static Genotype CreateRandom()
    {
        int numberOfLimbNodes = Random.Range(GenotypeParameters.MinLimbNodes, GenotypeParameters.MaxLimbNodes);
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
            List<LimbConnection> limbConnections = connectedNodeIds.Select(id => LimbConnection.CreateRandom(id)).ToList();
            limbNodes.Add(LimbNode.CreateRandom(limbConnections));
        }

        int numberOfBrainNeurons = Random.Range(GenotypeParameters.MinBrainNeurons, GenotypeParameters.MaxBrainNeurons);
        List<NeuronDefinition> brainNeuronDefinitions = new List<NeuronDefinition>();
        for (int i = 0; i < numberOfBrainNeurons; i++)
            brainNeuronDefinitions.Add(NeuronDefinition.CreateRandom());

        return new Genotype(null, null, brainNeuronDefinitions, limbNodes);
    }

    public static List<LimbNode> RemoveUnconnectedLimbNodes(IList<LimbNode> limbNodes)
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
            List<LimbConnection> connections = limbNodes[visitedNodeIds[i]].Connections
            .Select(oldConnection =>
            {
                int precedingUnconnectedNodesCount = unconnectedNodeIds.Count(id => id < oldConnection.ChildNodeId);
                return oldConnection.CreateCopy(oldConnection.ChildNodeId - precedingUnconnectedNodesCount);
            }).ToList();
            newLimbNodes.Add(limbNodes[visitedNodeIds[i]].CreateCopy(connections));
        }

        return newLimbNodes;
    }

    private static List<int> RecursivelyTraverseLimbNodes(IList<LimbNode> limbNodes, List<int> visitedNodeIds, int nodeId)
    {
        if (visitedNodeIds == null)
            visitedNodeIds = new List<int>();

        if (!visitedNodeIds.Contains(nodeId))
        {
            visitedNodeIds.Add(nodeId);

            foreach (LimbConnection connection in limbNodes[nodeId].Connections)
                RecursivelyTraverseLimbNodes(limbNodes, visitedNodeIds, connection.ChildNodeId);
        }

        return visitedNodeIds;
    }
}
