using System;
using System.Linq;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public class SimsGenotypeFactory : IGenotypeFactory<SimsGenotype>
    {
        private readonly float asexualProbability;
        private readonly float crossoverProbability;
        private readonly float graftingProbability;
        private readonly int crossoverInterval;

        public SimsGenotypeFactory(float asexualProbability, float crossoverProbability, float graftingProbability, int crossoverInterval)
        {
            this.asexualProbability = asexualProbability;
            this.crossoverProbability = crossoverProbability;
            this.graftingProbability = graftingProbability;
            this.crossoverInterval = crossoverInterval;

            if (Math.Abs(asexualProbability + crossoverProbability + graftingProbability - 1.0f) > 1e-6f)
                throw new ArgumentException("Recombination probabilities must sum to 1.0");

            if (crossoverInterval <= 0)
                throw new ArgumentException("Crossover interval must be greater than 0");
        }

        public SimsGenotype CreateInitialisedGenotype()
        {
            // Sims creatures initialise with a random genotype.
            SimsGenotypeCreationContext context = new(new List<Node>(), new List<Connection>(), new List<NeuronDefinition>());

            int nodeCount = SharedRandom.Next(SimsGenotype.MinNodeCount, SimsGenotype.MaxNodeCount + 1);
            for (int i = 0; i < nodeCount; i++)
                SimsGenotypeMutator.AddNode(context);

            int connectionCount = SharedRandom.Next(SimsGenotype.MinConnectionCount, SimsGenotype.MaxConnectionCount + 1);
            for (int i = 0; i < connectionCount; i++)
                SimsGenotypeMutator.AddConnection(context);

            int neuronDefinitionCount = SharedRandom.Next(SimsGenotype.MinNeuronDefinitionCount, SimsGenotype.MaxNeuronDefinitionCount + 1);
            for (int i = 0; i < neuronDefinitionCount; i++)
                SimsGenotypeMutator.AddNeuronDefinition(context);

            // Re-randomise neural components now that we have a full structure.
            for (int i = 0; i < context.Nodes.Count; i++)
            {
                Node oldNode = context.Nodes[i];
                JointDefinition randomisedJointDefinition = JointDefinition.RandomiseSignalInputs(oldNode.Gid, oldNode.JointDefinition, context.Nodes, context.Connections, context.NeuronDefinitions);
                context.Nodes[i] = oldNode.CopyWithSameGid(randomisedJointDefinition);
            }
            for (int i = 0; i < context.NeuronDefinitions.Count; i++)
            {
                NeuronDefinition oldNeuronDefinition = context.NeuronDefinitions[i];
                NeuronDefinition randomisedNeuronDefinition = NeuronDefinition.RandomiseSignalInputs(oldNeuronDefinition.Gid, oldNeuronDefinition, context.Nodes, context.Connections, context.NeuronDefinitions);
                context.NeuronDefinitions[i] = randomisedNeuronDefinition;
            }

            return context.CreateGenotypeFromContext(false); // No need to prune if 'add' mutations worked correctly.
        }

        public SimsGenotypeCreationContext Recombine(SimsGenotype parent1, SimsGenotype parent2)
        {
            double randomValue = SharedRandom.NextDouble();
            if (randomValue < asexualProbability)
                return AsexualRecombination(parent1);
            else if (randomValue < asexualProbability + crossoverProbability)
                return CrossoverRecombination(parent1, parent2);
            else if (randomValue < asexualProbability + crossoverProbability + graftingProbability)
                return GraftingRecombination(parent1, parent2);
            else
                throw new InvalidOperationException("Recombination probabilities do not sum to 1.0");
        }
        IGenotypeCreationContext<SimsGenotype> IGenotypeFactory<SimsGenotype>.Recombine(SimsGenotype parent1, SimsGenotype parent2)
        {
            return Recombine(parent1, parent2);
        }

        private SimsGenotypeCreationContext AsexualRecombination(SimsGenotype parent1)
        {
            return new(parent1.Nodes, parent1.Connections, parent1.NeuronDefinitions);
        }

        private SimsGenotypeCreationContext CrossoverRecombination(SimsGenotype parent1, SimsGenotype parent2)
        {
            SimsGenotypeCreationContext offspringContext = new(
                new List<Node>(),
                new List<Connection>(),
                new List<NeuronDefinition>()
            );

            bool copyFromParent1 = SharedRandom.Next(0, 2) == 0;
            Dictionary<ulong, ulong> nodeGidMap = new();
            for (int i = 0; i < Math.Max(parent1.Nodes.Count, parent2.Nodes.Count); i++)
            {
                if (i > 0 && i % crossoverInterval == 0)
                    copyFromParent1 = !copyFromParent1;

                SimsGenotype source = copyFromParent1 ? parent1 : parent2;
                if (source.Nodes.Count > i)
                {
                    Node chosenNode = source.Nodes[i];
                    Node copiedNode = chosenNode.CopyWithNewGid();
                    offspringContext.Nodes.Add(copiedNode);
                    nodeGidMap[chosenNode.Gid] = copiedNode.Gid;
                }
                else
                    break;
            }
            CopyOverRelatedConnections(parent1, parent2, offspringContext, nodeGidMap);
            CopyOverRelatedNeuronDefinitions(parent1, parent2, offspringContext, nodeGidMap);

            return offspringContext;
        }

        private SimsGenotypeCreationContext GraftingRecombination(SimsGenotype recipient, SimsGenotype donor)
        {
            SimsGenotypeCreationContext offspringContext = new(
                new List<Node>(),
                new List<Connection>(),
                new List<NeuronDefinition>()
            );

            // Randomly choose a source connection from the recipient side and a destination node from the donor side.
            int recipientConnectionIndex = SharedRandom.Next(0, recipient.Connections.Count);
            int donorNodeIndex = SharedRandom.Next(0, donor.Nodes.Count);

            // Copy over nodes from the recipient, up to and including the recipient node.
            int recipientNodeIndex = recipient.Nodes
                .Where(n => n.Gid == recipient.Connections[recipientConnectionIndex].ParentNodeGid)
                .Select(n => recipient.Nodes.IndexOf(n))
                .FirstOrDefault();
            Dictionary<ulong, ulong> nodeGidMap = new();
            for (int i = 0; i <= recipientNodeIndex; i++)
            {
                Node chosenNode = recipient.Nodes[i];
                Node copiedNode = recipient.Nodes[i].CopyWithNewGid();
                offspringContext.Nodes.Add(copiedNode);
                nodeGidMap[chosenNode.Gid] = copiedNode.Gid;
            }

            // Copy over nodes from the donor, starting from the donor node index.
            for (int i = donorNodeIndex; i < donor.Nodes.Count; i++)
            {
                Node chosenNode = donor.Nodes[i];
                Node copiedNode = donor.Nodes[i].CopyWithNewGid();
                offspringContext.Nodes.Add(copiedNode);
                nodeGidMap[chosenNode.Gid] = copiedNode.Gid;
            }

            Dictionary<ulong, ulong> connectionGidMap = CopyOverRelatedConnections(recipient, donor, offspringContext, nodeGidMap);
            CopyOverRelatedNeuronDefinitions(recipient, donor, offspringContext, nodeGidMap);

            // Change the recipient connection to point to the donor node.
            ulong offspringRcGid = connectionGidMap[recipient.Connections[recipientConnectionIndex].Gid];
            int offspringRcIndex = offspringContext.Connections.FindIndex(c => c.Gid == offspringRcGid);
            offspringContext.Connections[offspringRcIndex] = new Connection(
                offspringContext.Connections[offspringRcIndex].ParentNodeGid,
                offspringContext.Nodes[recipientNodeIndex + 1].Gid,
                offspringContext.Connections[offspringRcIndex].ParentFace,
                offspringContext.Connections[offspringRcIndex].Position,
                offspringContext.Connections[offspringRcIndex].Orientation,
                offspringContext.Connections[offspringRcIndex].Scale,
                offspringContext.Connections[offspringRcIndex].ReflectionX,
                offspringContext.Connections[offspringRcIndex].ReflectionY,
                offspringContext.Connections[offspringRcIndex].ReflectionZ,
                offspringContext.Connections[offspringRcIndex].TerminalOnly
            );

            return offspringContext;
        }

        private Dictionary<ulong, ulong> CopyOverRelatedConnections(SimsGenotype parent1, SimsGenotype parent2, SimsGenotypeCreationContext offspringContext, Dictionary<ulong, ulong> nodeGidMap)
        {
            Dictionary<ulong, ulong> connectionGidMap = new();
            IEnumerable<Connection> sourceConnections = parent1.Connections.Concat(parent2.Connections);
            foreach (var (sourceConnection, i) in sourceConnections.Select((c, i) => (c, i)))
            {
                if (connectionGidMap.ContainsKey(sourceConnection.Gid))
                    continue;

                ulong newParentGid = nodeGidMap.TryGetValue(sourceConnection.ParentNodeGid, out ulong newGid) ? newGid : sourceConnection.ParentNodeGid;
                ulong newChildGid = nodeGidMap.TryGetValue(sourceConnection.ChildNodeGid, out newGid) ? newGid : sourceConnection.ChildNodeGid;

                Connection copiedConnection = new(
                    newParentGid,
                    newChildGid,
                    sourceConnection.ParentFace,
                    sourceConnection.Position,
                    sourceConnection.Orientation,
                    sourceConnection.Scale,
                    sourceConnection.ReflectionX,
                    sourceConnection.ReflectionY,
                    sourceConnection.ReflectionZ,
                    sourceConnection.TerminalOnly
                );
                offspringContext.Connections.Add(copiedConnection);

                connectionGidMap[sourceConnection.Gid] = copiedConnection.Gid;
            }
            return connectionGidMap;
        }

        private Dictionary<ulong, ulong> CopyOverRelatedNeuronDefinitions(SimsGenotype parent1, SimsGenotype parent2, SimsGenotypeCreationContext offspringContext, Dictionary<ulong, ulong> nodeGidMap)
        {
            Dictionary<ulong, ulong> neuronDefinitionGidMap = new();
            IEnumerable<NeuronDefinition> sourceNeuronDefinitions = parent1.NeuronDefinitions.Concat(parent2.NeuronDefinitions);
            foreach (var sourceNeuron in sourceNeuronDefinitions)
            {
                if (neuronDefinitionGidMap.ContainsKey(sourceNeuron.Gid))
                    continue;

                ulong newContainerGid = nodeGidMap.TryGetValue(sourceNeuron.ContainerGid, out ulong newGid) ? newGid : sourceNeuron.ContainerGid;

                NeuronDefinition copiedNeuronDefinition = new(
                    newContainerGid,
                    sourceNeuron.ActivationFunction,
                    sourceNeuron.Inputs
                );
                offspringContext.NeuronDefinitions.Add(copiedNeuronDefinition);

                neuronDefinitionGidMap[sourceNeuron.Gid] = copiedNeuronDefinition.Gid;
            }
            return neuronDefinitionGidMap;
        }
    }
}
