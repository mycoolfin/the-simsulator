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
            ConnectionMutations.Choose().Invoke(context);
        }

        public static void AddConnection(SimsGenotypeCreationContext context)
        {
            if (context.Connections.Count < SimsGenotype.MAX_CONNECTIONS)
            {
                ulong parentNodeId = context.Nodes[SharedRandom.Next(context.Nodes.Count)].Gid;
                ulong childNodeId = context.Nodes[SharedRandom.Next(context.Nodes.Count)].Gid;
                context.Connections.Add(Connection.CreateRandom(parentNodeId, childNodeId));
            }
        }

        public static void RemoveConnection(SimsGenotypeCreationContext context)
        {
            if (context.Connections.Count > SimsGenotype.MIN_CONNECTIONS)
            {
                int index = SharedRandom.Next(context.Connections.Count);
                context.Connections.RemoveAt(index);
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
            NeuronDefinitionMutations.Choose().Invoke(context);
        }

        public static void AddNeuronDefinition(SimsGenotypeCreationContext context)
        {
            if (context.NeuronDefinitions.Count < SimsGenotype.MAX_NEURON_DEFINITIONS)
            {
                List<ulong> existingContainerIds = new(context.Nodes.Select(n => n.Gid)) { SimsGenotype.BRAIN_GID };
                ulong randomContainerId = existingContainerIds[SharedRandom.Next(existingContainerIds.Count)];
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
            if (context.NeuronDefinitions.Count > SimsGenotype.MIN_NEURON_DEFINITIONS)
            {
                int index = SharedRandom.Next(context.NeuronDefinitions.Count);
                context.NeuronDefinitions.RemoveAt(index);
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
    }
}
