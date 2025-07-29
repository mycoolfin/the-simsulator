using System;
using System.Collections.Generic;
using System.Linq;

namespace mycoolfin.TheSimsulator.Sims.Genotype
{
    public static class SimsGenotypeMutator
    {
        private static readonly WeightedChoiceList<Action<SimsGenotypeCreationContext>> GenotypeMutations = new()
        {
            { MutateNodes, 1f },
            { MutateConnections, 1f },
            { MutateNeuronDefinitions, 1f }
        };

        private static readonly WeightedChoiceList<Action<SimsGenotypeCreationContext>> NodeMutations = new()
        {
            { AddNode, 1f },
            { RemoveNode, 1f },
            { MutateRandomNode, 1f }
        };

        private static readonly WeightedChoiceList<Action<SimsGenotypeCreationContext>> ConnectionMutations = new()
        {
            { AddConnection, 1f },
            { RemoveConnection, 1f },
            { MutateRandomConnection, 1f }
        };

        private static readonly WeightedChoiceList<Action<SimsGenotypeCreationContext>> NeuronDefinitionMutations = new()
        {
            { AddNeuronDefinition, 1f },
            { RemoveNeuronDefinition, 1f },
            { MutateRandomNeuronDefinition, 1f }
        };

        public static void MutateGenotype(SimsGenotypeCreationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            GenotypeMutations.Choose().Invoke(context);
        }

        public static void MutateNodes(SimsGenotypeCreationContext context)
        {
            NodeMutations.Choose().Invoke(context);
        }

        public static void AddNode(SimsGenotypeCreationContext context)
        {
            if (context.Nodes.Count < SimsGenotype.MAX_NODES)
                context.Nodes.Add(Node.CreateRandom(context.Nodes, context.Connections, context.NeuronDefinitions));
        }

        public static void RemoveNode(SimsGenotypeCreationContext context)
        {
            if (context.Nodes.Count > SimsGenotype.MIN_NODES)
            {
                int index = SharedRandom.Next(context.Nodes.Count);

                ulong nodeGid = context.Nodes[index].Gid;

                // Remove all connections associated with this node.
                context.Connections.RemoveAll(c => c.ParentNodeGid == nodeGid || c.ChildNodeGid == nodeGid);

                // Remove all neuron definitions associated with this node.
                context.NeuronDefinitions.RemoveAll(nd => nd.ContainerGid == nodeGid);

                context.Nodes.RemoveAt(index);
            }
        }

        public static void MutateRandomNode(SimsGenotypeCreationContext context)
        {
            if (context.Nodes.Count > 0)
            {
                int index = SharedRandom.Next(context.Nodes.Count);
                NodeMutator.MutateNode(context, index);
            }
        }

        public static void MutateConnections(SimsGenotypeCreationContext context)
        {
            RemoveDanglingConnections(context);
            ConnectionMutations.Choose().Invoke(context);
        }

        public static void AddConnection(SimsGenotypeCreationContext context)
        {
            int totalMaxConnections = context.Nodes.Count * Node.MAX_CONNECTIONS;
            if (context.Connections.Count < totalMaxConnections)
            {
                Dictionary<ulong, int> parentCounts = context.Nodes.ToDictionary(node => node.Gid, node => 0);
                foreach (var connection in context.Connections)
                    parentCounts[connection.ParentNodeGid]++;
                List<ulong> eligibleParentIds = parentCounts
                    .Where(kvp => kvp.Value < Node.MAX_CONNECTIONS)
                    .Select(kvp => kvp.Key)
                    .ToList();

                if (eligibleParentIds.Count == 0)
                    return;

                ulong parentNodeId = eligibleParentIds[SharedRandom.Next(eligibleParentIds.Count)];
                ulong childNodeId = context.Nodes[SharedRandom.Next(context.Nodes.Count)].Gid;
                context.Connections.Add(Connection.CreateRandom(parentNodeId, childNodeId));
            }
        }

        public static void RemoveConnection(SimsGenotypeCreationContext context)
        {
            int totalMinConnections = context.Nodes.Count * Node.MIN_CONNECTIONS;
            if (context.Connections.Count > totalMinConnections)
            {
                Dictionary<ulong, List<int>> parentIndices = new();
                for (int i = 0; i < context.Connections.Count; i++)
                {
                    Connection connection = context.Connections[i];
                    if (!parentIndices.ContainsKey(connection.ParentNodeGid))
                        parentIndices[connection.ParentNodeGid] = new List<int>();
                    parentIndices[connection.ParentNodeGid].Add(i);
                }
                List<ulong> eligibleParentIds = parentIndices
                    .Where(kvp => kvp.Value.Count > Node.MIN_CONNECTIONS)
                    .Select(kvp => kvp.Key)
                    .ToList();

                if (eligibleParentIds.Count == 0)
                    return;

                ulong parentNodeId = eligibleParentIds[SharedRandom.Next(eligibleParentIds.Count)];
                int randomIndexWithinParent = SharedRandom.Next(parentIndices[parentNodeId].Count);
                context.Connections.RemoveAt(parentIndices[parentNodeId][randomIndexWithinParent]);
            }
        }

        public static void MutateRandomConnection(SimsGenotypeCreationContext context)
        {
            if (context.Connections.Count > 0)
            {
                int index = SharedRandom.Next(context.Connections.Count);
                ConnectionMutator.MutateConnection(context, index);
            }
        }

        public static void MutateNeuronDefinitions(SimsGenotypeCreationContext context)
        {
            RemoveDanglingNeuronDefinitions(context);
            NeuronDefinitionMutations.Choose().Invoke(context);
        }

        public static void AddNeuronDefinition(SimsGenotypeCreationContext context)
        {
            int totalMaxNeuronDefinitions = context.Nodes.Count * Node.MAX_NEURON_DEFINITIONS + SimsGenotype.MAX_BRAIN_NEURON_DEFINITIONS;
            if (context.NeuronDefinitions.Count < totalMaxNeuronDefinitions)
            {
                Dictionary<ulong, int> containerCounts = context.Nodes.ToDictionary(node => node.Gid, node => 0);
                containerCounts[SimsGenotype.BRAIN_GID] = 0; // Include brain.
                foreach (var neuron in context.NeuronDefinitions)
                    containerCounts[neuron.ContainerGid]++;
                List<ulong> eligibleContainerIds = containerCounts.Where(kvp =>
                    kvp.Key == SimsGenotype.BRAIN_GID
                            ? kvp.Value < SimsGenotype.MAX_BRAIN_NEURON_DEFINITIONS
                            : kvp.Value < Node.MAX_NEURON_DEFINITIONS
                    ).Select(kvp => kvp.Key).ToList();

                if (eligibleContainerIds.Count == 0)
                    return;

                ulong randomContainerId = eligibleContainerIds[SharedRandom.Next(eligibleContainerIds.Count)];
                NeuronDefinition newNeuronDefinition = NeuronDefinition.CreateRandom(
                    randomContainerId,
                    context.Nodes,
                    context.Connections,
                    context.NeuronDefinitions
                );
                context.NeuronDefinitions.Add(newNeuronDefinition);
            }
        }

        public static void RemoveNeuronDefinition(SimsGenotypeCreationContext context)
        {
            int totalMinNeuronDefinitions = context.Nodes.Count * Node.MIN_NEURON_DEFINITIONS + SimsGenotype.MIN_BRAIN_NEURON_DEFINITIONS;
            if (context.NeuronDefinitions.Count > totalMinNeuronDefinitions)
            {
                Dictionary<ulong, List<int>> containerIndices = new();
                for (int i = 0; i < context.NeuronDefinitions.Count; i++)
                {
                    NeuronDefinition neuron = context.NeuronDefinitions[i];
                    if (!containerIndices.ContainsKey(neuron.ContainerGid))
                        containerIndices[neuron.ContainerGid] = new List<int>();
                    containerIndices[neuron.ContainerGid].Add(i);
                }
                List<ulong> eligibleContainerIds = containerIndices.Where(kvp =>
                    kvp.Key == SimsGenotype.BRAIN_GID
                            ? kvp.Value.Count > SimsGenotype.MIN_BRAIN_NEURON_DEFINITIONS
                            : kvp.Value.Count > Node.MIN_NEURON_DEFINITIONS
                    ).Select(kvp => kvp.Key).ToList();

                if (eligibleContainerIds.Count == 0)
                    return;

                ulong randomContainerId = eligibleContainerIds[SharedRandom.Next(eligibleContainerIds.Count)];
                int randomIndexWithinContainer = SharedRandom.Next(containerIndices[randomContainerId].Count);
                context.NeuronDefinitions.RemoveAt(containerIndices[randomContainerId][randomIndexWithinContainer]);
            }
        }

        public static void MutateRandomNeuronDefinition(SimsGenotypeCreationContext context)
        {
            if (context.NeuronDefinitions.Count > 0)
            {
                int index = SharedRandom.Next(context.NeuronDefinitions.Count);
                NeuronDefinitionMutator.MutateNeuronDefinition(context, index);
            }
        }

        private static void RemoveDanglingConnections(SimsGenotypeCreationContext context)
        {
            // Remove connections that reference nodes that no longer exist.
            HashSet<ulong> validNodeIds = context.Nodes.Select(n => n.Gid).ToHashSet();
            context.Connections.RemoveAll(c => !validNodeIds.Contains(c.ParentNodeGid) || !validNodeIds.Contains(c.ChildNodeGid));
        }

        private static void RemoveDanglingNeuronDefinitions(SimsGenotypeCreationContext context)
        {
            // Remove neuron definitions that reference nodes that no longer exist.
            HashSet<ulong> validNodeIds = context.Nodes.Select(n => n.Gid).ToHashSet();
            validNodeIds.Add(SimsGenotype.BRAIN_GID); // Include brain as a valid container.
            context.NeuronDefinitions.RemoveAll(nd => !validNodeIds.Contains(nd.ContainerGid));
        }
    }
}
