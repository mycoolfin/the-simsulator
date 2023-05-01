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
    [SerializeField] private List<string> lineage;
    [SerializeField] private List<NeuronDefinition> brainNeuronDefinitions;
    [SerializeField] private List<LimbNode> limbNodes;

    public string Id => id;
    public ReadOnlyCollection<string> Lineage => lineage.AsReadOnly();
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

    private Genotype(string id, IList<string> lineage, IList<NeuronDefinition> brainNeuronDefinitions, IList<LimbNode> limbNodes, IList<InstancedLimbNode> instancedLimbNodes)
    {
        this.id = !string.IsNullOrEmpty(id) ? id : "G" + latestId++;
        this.lineage = lineage == null ? (new List<string> { this.id + " created" }) : lineage.ToList();
        this.limbNodes = limbNodes.ToList();
        this.brainNeuronDefinitions = brainNeuronDefinitions == null ? new List<NeuronDefinition>() : brainNeuronDefinitions.ToList();
        this.instancedLimbNodes = instancedLimbNodes == null ? null : instancedLimbNodes.ToList();
    }

    public static Genotype Construct(string id, IList<string> lineage, IList<NeuronDefinition> brainNeuronDefinitions, IList<LimbNode> limbNodes)
    {
        Genotype genotype = new Genotype(id, lineage, brainNeuronDefinitions, limbNodes, null);
        genotype.PruneUnconnectedLimbNodes();
        genotype.FixBrokenNeuralConnections();
        return genotype;
    }

    public void Validate()
    {
        bool validNeuronDefinitionsCount = brainNeuronDefinitions.Count >= GenotypeParameters.MinBrainNeurons && brainNeuronDefinitions.Count <= GenotypeParameters.MaxBrainNeurons;
        if (!validNeuronDefinitionsCount)
            throw new System.ArgumentException("The number of brain neuron definitions must be between " + GenotypeParameters.MinBrainNeurons
            + " and " + GenotypeParameters.MaxBrainNeurons + ". Specified: " + brainNeuronDefinitions.Count);

        foreach (NeuronDefinition neuronDefinition in brainNeuronDefinitions)
            neuronDefinition.Validate(EmitterAvailabilityMap.GenerateMapForBrain(brainNeuronDefinitions.Count, InstancedLimbNodes));

        bool validLimbNodesCount = limbNodes.Count >= GenotypeParameters.MinLimbNodes && limbNodes.Count <= GenotypeParameters.MaxLimbNodes;
        if (!validLimbNodesCount)
            throw new System.ArgumentException("The number of limb nodes must be between " + GenotypeParameters.MinLimbNodes
            + " and " + GenotypeParameters.MaxLimbNodes + ". Specified: " + limbNodes.Count);

        for (int i = 0; i < limbNodes.Count; i++)
            limbNodes[i].Validate(EmitterAvailabilityMap.GenerateMapForLimbNode(brainNeuronDefinitions.Count, limbNodes.Cast<ILimbNodeEssentialInfo>().ToList(), i));
    }

    public static Genotype CreateRandom()
    {
        int numberOfBrainNeurons = UnityEngine.Random.Range(GenotypeParameters.MinBrainNeurons, GenotypeParameters.MaxBrainNeurons + 1);
        int numberOfLimbNodes = UnityEngine.Random.Range(GenotypeParameters.MinLimbNodes, GenotypeParameters.MaxLimbNodes + 1);

        List<ILimbNodeEssentialInfo> unfinishedLimbNodes = new();
        for (int i = 0; i < numberOfLimbNodes; i++)
        {
            List<int> connectedNodeIds = new List<int>();
            for (int attemptNum = 0; attemptNum < GenotypeGenerationParameters.MaxConnectionAttempts; attemptNum++)
            {
                bool attemptSuccess;
                if (i == 0 && attemptNum == 0) // Guarantee at least one connection for the root node.
                    attemptSuccess = true;
                else
                    attemptSuccess = UnityEngine.Random.value > GenotypeGenerationParameters.ConnectionAttemptChance;
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

        return new Genotype(null, null, brainNeuronDefinitions, limbNodes, instancedLimbNodes);
    }

    public string SaveToFile(string path)
    {
        return GenotypeSerializer.WriteGenotypeToFile(this, path);
    }

    private void PruneUnconnectedLimbNodes()
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

    private void FixBrokenNeuralConnections()
    {
        EmitterAvailabilityMap brainMap = EmitterAvailabilityMap.GenerateMapForBrain(BrainNeuronDefinitions.Count, InstancedLimbNodes);
        List<NeuronDefinition> newBrainNeuronDefinitions = BrainNeuronDefinitions.Select(n => new NeuronDefinition(
            n.Type,
            n.InputDefinitions.Select(i => FixInputDefinition(i, brainMap)).ToList()
        )).ToList();

        List<LimbNode> newLimbNodes = new();
        for (int i = 0; i < LimbNodes.Count; i++)
        {
            LimbNode limbNode = LimbNodes[i];

            EmitterAvailabilityMap limbNodeMap = EmitterAvailabilityMap.GenerateMapForLimbNode(
                BrainNeuronDefinitions.Count,
                LimbNodes.Cast<ILimbNodeEssentialInfo>().ToList(),
                i
            );

            List<JointAxisDefinition> newJointAxisDefinitions = limbNode.JointDefinition.AxisDefinitions.Select(a => new JointAxisDefinition(
                a.Limit,
                FixInputDefinition(a.InputDefinition, limbNodeMap)
            )).ToList();
            List<NeuronDefinition> newNeuronDefinitions = limbNode.NeuronDefinitions.Select(n => new NeuronDefinition(
                n.Type,
                n.InputDefinitions.Select(i => FixInputDefinition(i, limbNodeMap)).ToList()
            )).ToList();

            newLimbNodes.Add(new(
                limbNode.Dimensions,
                new JointDefinition(limbNode.JointDefinition.Type, newJointAxisDefinitions),
                limbNode.RecursiveLimit,
                newNeuronDefinitions,
                limbNode.Connections
            ));
        }

        brainNeuronDefinitions = newBrainNeuronDefinitions;
        limbNodes = newLimbNodes;
        instancedLimbNodes = null;
    }

    private static InputDefinition FixInputDefinition(InputDefinition inputDefinition, EmitterAvailabilityMap map)
    {
        try
        {
            inputDefinition.Validate(map);
            return inputDefinition; // If validation succeeds, return the original input definition.
        }
        catch
        {
            InputDefinition randomDefinition = InputDefinition.CreateRandom(map);
            return new(
                randomDefinition.EmitterSetLocation,
                randomDefinition.ChildLimbIndex,
                randomDefinition.InstanceId,
                randomDefinition.EmitterIndex,
                inputDefinition.Weight // Preserve original weight.
            );
        }
    }
}
