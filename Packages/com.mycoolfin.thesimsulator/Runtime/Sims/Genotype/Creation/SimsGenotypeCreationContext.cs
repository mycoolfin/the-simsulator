using System.Collections.Generic;
using System.Linq;

namespace mycoolfin.TheSimsulator.Sims
{
    public class SimsGenotypeCreationContext : IGenotypeCreationContext<SimsGenotype>
    {
        public List<Node> Nodes { get; }
        public List<Connection> Connections { get; }
        public List<NeuronDefinition> NeuronDefinitions { get; }

        public SimsGenotypeCreationContext(IReadOnlyList<Node> nodes, IReadOnlyList<Connection> connections, IReadOnlyList<NeuronDefinition> neuronDefinitions)
        {
            Nodes = nodes?.ToList() ?? throw new System.ArgumentNullException(nameof(nodes), "Nodes cannot be null.");
            Connections = connections?.ToList() ?? throw new System.ArgumentNullException(nameof(connections), "Connections cannot be null.");
            NeuronDefinitions = neuronDefinitions?.ToList() ?? throw new System.ArgumentNullException(nameof(neuronDefinitions), "NeuronDefinitions cannot be null.");
        }

        public SimsGenotype CreateGenotypeFromContext(bool pruneMissingReferences = true)
        {
            if (pruneMissingReferences)
                PruneMissingReferences();

            return new SimsGenotype(Nodes, Connections, NeuronDefinitions);
        }

        /// <summary>
        /// Mutates the genotype creation context in place.
        /// The mutation rate determines the average number of mutations to be applied to the genotype.
        /// For each mutation, the decision tree is traversed to select a mutation based on defined probabilities.
        /// </summary>
        /// <param name="mutationRate"></param>
        public void Mutate(float mutationRate)
        {
            int mutationCount = SharedRandom.DrawPoisson(mutationRate);
            for (int i = 0; i < mutationCount; i++)
                SimsGenotypeMutator.MutateGenotype(this);
        }

        private void PruneMissingReferences()
        {
            // Remove any nodes that cannot be reached by graph traversal from the first node.
            if (Nodes.Count > 1)
            {
                Dictionary<ulong, List<ulong>> childMap = Connections
                    .GroupBy(c => c.ParentNodeGid)
                    .ToDictionary(g => g.Key, g => g.Select(c => c.ChildNodeGid).ToList());

                HashSet<ulong> reachableNodeIds = new();
                Queue<ulong> nodeQueue = new();
                nodeQueue.Enqueue(Nodes[0].Gid);

                while (nodeQueue.Count > 0)
                {
                    ulong currentNodeId = nodeQueue.Dequeue();

                    if (reachableNodeIds.Add(currentNodeId) && childMap.TryGetValue(currentNodeId, out List<ulong> childNodeIds))
                        foreach (ulong cid in childNodeIds) if (!reachableNodeIds.Contains(cid)) nodeQueue.Enqueue(cid);
                }

                Nodes.RemoveAll(n => !reachableNodeIds.Contains(n.Gid));
            }

            HashSet<ulong> validNodeIds = Nodes.Select(n => n.Gid).ToHashSet();

            // Remove any connections that reference invalid nodes.
            Connections.RemoveAll(c => !validNodeIds.Contains(c.ParentNodeGid) || !validNodeIds.Contains(c.ChildNodeGid));

            // Remove any neuron definitions that reference invalid containers.
            HashSet<ulong> validContainerIds = new(validNodeIds) { SimsGenotype.BRAIN_GID };
            NeuronDefinitions.RemoveAll(nd => !validContainerIds.Contains(nd.ContainerGid));
        }
    }
}
