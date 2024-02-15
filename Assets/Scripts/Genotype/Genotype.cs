using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[Serializable]
public class Genotype
{
    private static int latestId = 1;

    [SerializeField] private string id;
    [SerializeField] private List<NeuronDefinition> brainNeuronDefinitions;
    [SerializeField] private List<LimbNode> limbNodes;

    public string Id => id;
    public ReadOnlyCollection<NeuronDefinition> BrainNeuronDefinitions => brainNeuronDefinitions.AsReadOnly();
    public ReadOnlyCollection<LimbNode> LimbNodes => limbNodes.AsReadOnly();

    private List<InstancedLimbNode> instancedLimbNodes;
    public ReadOnlyCollection<InstancedLimbNode> InstancedLimbNodes
    {
        get
        {
            if (instancedLimbNodes == null)
                instancedLimbNodes = InstancedLimbNode.DeriveInstancedLimbNodes(limbNodes);
            return instancedLimbNodes.AsReadOnly();
        }
    }

    private Genotype(string id, IList<NeuronDefinition> brainNeuronDefinitions, IList<LimbNode> limbNodes, IList<InstancedLimbNode> instancedLimbNodes)
    {
        this.id = !string.IsNullOrEmpty(id) ? id : (latestId++).ToString();
        this.limbNodes = limbNodes.ToList();
        this.brainNeuronDefinitions = brainNeuronDefinitions == null ? new List<NeuronDefinition>() : brainNeuronDefinitions.ToList();
        this.instancedLimbNodes = instancedLimbNodes == null ? null : instancedLimbNodes.ToList();
    }

    public static Genotype Construct(string id, IList<NeuronDefinition> brainNeuronDefinitions, IList<LimbNode> limbNodes)
    {
        Genotype genotype = new Genotype(id, brainNeuronDefinitions, limbNodes, null);
        return genotype;
    }

    public void Validate()
    {
        bool validNeuronDefinitionsCount = brainNeuronDefinitions.Count >= ParameterManager.Instance.Genotype.MinBrainNeurons && brainNeuronDefinitions.Count <= ParameterManager.Instance.Genotype.MaxBrainNeurons;
        if (!validNeuronDefinitionsCount)
            throw new System.ArgumentException("The number of brain neuron definitions must be between " + ParameterManager.Instance.Genotype.MinBrainNeurons
            + " and " + ParameterManager.Instance.Genotype.MaxBrainNeurons + ". Specified: " + brainNeuronDefinitions.Count);

        foreach (NeuronDefinition neuronDefinition in brainNeuronDefinitions)
            neuronDefinition.Validate(EmitterAvailabilityMap.GenerateMapForBrain(brainNeuronDefinitions.Count, InstancedLimbNodes));

        bool validLimbNodesCount = limbNodes.Count >= ParameterManager.Instance.Genotype.MinLimbNodes && limbNodes.Count <= ParameterManager.Instance.Genotype.MaxLimbNodes;
        if (!validLimbNodesCount)
            throw new System.ArgumentException("The number of limb nodes must be between " + ParameterManager.Instance.Genotype.MinLimbNodes
            + " and " + ParameterManager.Instance.Genotype.MaxLimbNodes + ". Specified: " + limbNodes.Count);

        for (int i = 0; i < limbNodes.Count; i++)
            limbNodes[i].Validate(EmitterAvailabilityMap.GenerateMapForLimbNode(brainNeuronDefinitions.Count, limbNodes.Cast<ILimbNodeEssentialInfo>().ToList(), i));
    }

    public static Genotype CreateRandom()
    {
        int numberOfBrainNeurons = UnityEngine.Random.Range(ParameterManager.Instance.Genotype.MinBrainNeurons, ParameterManager.Instance.Genotype.MaxBrainNeurons + 1);
        int numberOfLimbNodes = UnityEngine.Random.Range(ParameterManager.Instance.Genotype.MinLimbNodes, ParameterManager.Instance.Genotype.MaxLimbNodes + 1);

        List<ILimbNodeEssentialInfo> unfinishedLimbNodes = new();
        for (int i = 0; i < numberOfLimbNodes; i++)
        {
            List<int> connectedNodeIds = new List<int>();
            for (int attemptNum = 0; attemptNum < ParameterManager.Instance.GenotypeGeneration.MaxConnectionAttempts; attemptNum++)
            {
                if (connectedNodeIds.Count == ParameterManager.Instance.LimbNode.MaxLimbConnections)
                    break;
                bool attemptSuccess;
                if (i == 0 && attemptNum == 0) // Guarantee at least one connection for the root node.
                    attemptSuccess = true;
                else
                    attemptSuccess = UnityEngine.Random.value > ParameterManager.Instance.GenotypeGeneration.ConnectionAttemptChance;
                if (attemptSuccess)
                    connectedNodeIds.Add(UnityEngine.Random.Range(0, numberOfLimbNodes));
                else
                    break;
            }
            List<LimbConnection> connections = connectedNodeIds.Select(id => LimbConnection.CreateRandom(id)).ToList();
            unfinishedLimbNodes.Add(UnfinishedLimbNode.CreateRandom(connections));
        }

        List<LimbNode> limbNodes = new List<LimbNode>();
        for (int i = 0; i < unfinishedLimbNodes.Count; i++)
        {
            EmitterAvailabilityMap emitterAvailabilityMap = EmitterAvailabilityMap.GenerateMapForLimbNode(numberOfBrainNeurons, unfinishedLimbNodes, i);
            UnfinishedLimbNode unfinishedLimbNode = (UnfinishedLimbNode)unfinishedLimbNodes[i];
            limbNodes.Add(LimbNode.CreateRandom(emitterAvailabilityMap, unfinishedLimbNode));
        }

        List<InstancedLimbNode> instancedLimbNodes = InstancedLimbNode.DeriveInstancedLimbNodes(limbNodes);

        List<NeuronDefinition> brainNeuronDefinitions = new();
        for (int i = 0; i < numberOfBrainNeurons; i++)
            brainNeuronDefinitions.Add(NeuronDefinition.CreateRandom(EmitterAvailabilityMap.GenerateMapForBrain(numberOfBrainNeurons, instancedLimbNodes)));

        Genotype randomGenotype = Genotype.Construct(null, brainNeuronDefinitions, limbNodes);
        randomGenotype.instancedLimbNodes = instancedLimbNodes; // Cache for later.
        return randomGenotype;
    }

    public string SaveToFile(string path)
    {
        return GenotypeSerializer.WriteGenotypeToFile(this, path);
    }

    public void PruneUnconnectedLimbNodes()
    {
        List<int> visitedNodeIds = RecursivelyTraverseLimbNodes(limbNodes, null, 0);
        List<int> unconnectedNodeIds = new List<int>();
        for (int i = 0; i < limbNodes.Count; i++)
            if (!visitedNodeIds.Contains(i))
                unconnectedNodeIds.Add(i);

        if (unconnectedNodeIds.Count == 0)
            return;

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

        limbNodes = newLimbNodes;
        instancedLimbNodes = null;
    }

    private static List<int> RecursivelyTraverseLimbNodes(IList<LimbNode> limbNodes, List<int> visitedNodeIds, int nodeId)
    {
        if (visitedNodeIds == null)
            visitedNodeIds = new();

        if (!visitedNodeIds.Contains(nodeId))
        {
            visitedNodeIds.Add(nodeId);

            foreach (LimbConnection connection in limbNodes[nodeId].Connections)
                RecursivelyTraverseLimbNodes(limbNodes, visitedNodeIds, connection.ChildNodeId);
        }

        return visitedNodeIds;
    }
}
