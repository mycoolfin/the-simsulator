using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace mycoolfin.TheSimsulator.Sims.Genotype
{
    using Core.Genotype;

    public class SimsGenotypeFactory : IGenotypeFactory<SimsGenotype>
    {
        public readonly float AsexualProbability;
        public readonly float CrossoverProbability;
        public readonly float GraftingProbability;
        public readonly int CrossoverInterval;

        private readonly int createYieldAfterCount;
        private readonly int recombineYieldAfterCount;

        public SimsGenotypeFactory(
            float asexualProbability = 0.4f,
            float crossoverProbability = 0.3f,
            float graftingProbability = 0.3f,
            int crossoverInterval = 2,
            int createYieldAfterCount = 100,
            int recombineYieldAfterCount = 100
        )
        {
            if (Math.Abs(asexualProbability + crossoverProbability + graftingProbability - 1.0f) > 1e-6f)
                throw new ArgumentException("Recombination probabilities must sum to 1.0");

            if (crossoverInterval <= 0)
                throw new ArgumentException("Crossover interval must be greater than 0");

            AsexualProbability = asexualProbability;
            CrossoverProbability = crossoverProbability;
            GraftingProbability = graftingProbability;
            CrossoverInterval = crossoverInterval;

            this.createYieldAfterCount = createYieldAfterCount;
            this.recombineYieldAfterCount = recombineYieldAfterCount;
        }

        public virtual IEnumerator CreateInitialisedGenotypes(int count, IGenotypeFactory<SimsGenotype>.OnGenotypeCreatedDelegate onCreated)
        {
            for (int i = 0; i < count; i++)
            {
                onCreated?.Invoke(i, CreateInitialisedGenotype());
                if (i % createYieldAfterCount == 0 && i > 0)
                    yield return null;
            }
        }

        public virtual IEnumerator Recombine(IReadOnlyList<(SimsGenotype parent1, SimsGenotype parent2)> parents, float mutationRate, IGenotypeFactory<SimsGenotype>.OnGenotypeCreatedDelegate onRecombined)
        {
            for (int i = 0; i < parents.Count; i++)
            {
                onRecombined?.Invoke(i, Recombine(parents[i].parent1, parents[i].parent2, mutationRate));
                if (i % recombineYieldAfterCount == 0 && i > 0)
                    yield return null;
            }
        }

        protected SimsGenotype CreateInitialisedGenotype()
        {
            // Sims creatures initialise with a random genotype.
            SimsGenotypeCreationContext context = new(new List<Node>(), new List<Connection>(), new List<NeuronDefinition>());

            int nodeCount = SharedRandom.Next(SimsGenotype.MIN_NODES, SimsGenotype.MAX_NODES + 1);
            for (int i = 0; i < nodeCount; i++)
                SimsGenotypeMutator.AddNode(context);

            int minConnections = Node.MIN_CONNECTIONS * context.Nodes.Count;
            int maxConnections = Node.MAX_CONNECTIONS * context.Nodes.Count;
            int connectionCount = SharedRandom.Next(Math.Max(minConnections, 1), maxConnections + 1);
            for (int i = 0; i < connectionCount; i++)
                SimsGenotypeMutator.AddConnection(context);

            int minNeuronDefinitions = SimsGenotype.MIN_BRAIN_NEURON_DEFINITIONS + Node.MIN_NEURON_DEFINITIONS * context.Nodes.Count;
            int maxNeuronDefinitions = SimsGenotype.MAX_BRAIN_NEURON_DEFINITIONS + Node.MAX_NEURON_DEFINITIONS * context.Nodes.Count;
            int neuronDefinitionCount = SharedRandom.Next(minNeuronDefinitions, maxNeuronDefinitions + 1);
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

        protected SimsGenotype Recombine(SimsGenotype parent1, SimsGenotype parent2, float mutationRate)
        {
            double randomValue = SharedRandom.NextDouble();
            SimsGenotypeCreationContext offspringContext;
            if (randomValue < AsexualProbability)
                offspringContext = AsexualRecombination(parent1);
            else if (randomValue < AsexualProbability + CrossoverProbability)
                offspringContext = CrossoverRecombination(parent1, parent2);
            else if (randomValue < AsexualProbability + CrossoverProbability + GraftingProbability)
                offspringContext = GraftingRecombination(parent1, parent2);
            else
                throw new InvalidOperationException("Recombination probabilities do not sum to 1.0");

            offspringContext.Mutate(mutationRate);

            return offspringContext.CreateGenotypeFromContext(true);
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
            Dictionary<ulong, ulong> parent1OriginalToCopiedNodeMap = new();
            Dictionary<ulong, ulong> parent2OriginalToCopiedNodeMap = new();
            for (int i = 0; i < Math.Max(parent1.Nodes.Count, parent2.Nodes.Count); i++)
            {
                if (i > 0 && i % CrossoverInterval == 0)
                    copyFromParent1 = !copyFromParent1;

                SimsGenotype source = copyFromParent1 ? parent1 : parent2;
                if (source.Nodes.Count > i)
                {
                    Node chosenNode = source.Nodes[i];
                    Node copiedNode = chosenNode.CopyWithNewGid();
                    offspringContext.Nodes.Add(copiedNode);
                    if (copyFromParent1)
                        parent1OriginalToCopiedNodeMap[chosenNode.Gid] = copiedNode.Gid;
                    else
                        parent2OriginalToCopiedNodeMap[chosenNode.Gid] = copiedNode.Gid;
                }
                else
                    break;
            }

            CopyOverRelatedConnections(parent1, parent2, offspringContext, parent1OriginalToCopiedNodeMap, parent2OriginalToCopiedNodeMap);
            CopyOverRelatedNeuronDefinitions(parent1, parent2, offspringContext, parent1OriginalToCopiedNodeMap, parent2OriginalToCopiedNodeMap);

            return offspringContext;
        }

        private SimsGenotypeCreationContext GraftingRecombination(SimsGenotype recipient, SimsGenotype donor)
        {
            if (recipient.Connections.Count == 0)
                return AsexualRecombination(donor);

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
            Dictionary<ulong, ulong> parent1OriginalToCopiedNodeMap = new();
            for (int i = 0; i <= recipientNodeIndex; i++)
            {
                Node chosenNode = recipient.Nodes[i];
                Node copiedNode = recipient.Nodes[i].CopyWithNewGid();
                offspringContext.Nodes.Add(copiedNode);
                parent1OriginalToCopiedNodeMap[chosenNode.Gid] = copiedNode.Gid;
            }

            if (recipientNodeIndex + 1 == SimsGenotype.MAX_NODES)
                return AsexualRecombination(recipient); // Impossible to graft more nodes.

            // Copy over nodes from the donor, starting from the donor node index.
            int remainingNodeBudget = SimsGenotype.MAX_NODES - offspringContext.Nodes.Count;
            Dictionary<ulong, ulong> parent2OriginalToCopiedNodeMap = new();
            for (int i = donorNodeIndex; i < donor.Nodes.Count; i++)
            {
                if (remainingNodeBudget <= 0)
                    break;
                Node chosenNode = donor.Nodes[i];
                Node copiedNode = donor.Nodes[i].CopyWithNewGid();
                offspringContext.Nodes.Add(copiedNode);
                parent2OriginalToCopiedNodeMap[chosenNode.Gid] = copiedNode.Gid;
                remainingNodeBudget--;
            }

            CopyOverRelatedConnections(recipient, donor, offspringContext, parent1OriginalToCopiedNodeMap, parent2OriginalToCopiedNodeMap, recipientNodeIndex, recipientConnectionIndex, donor.Nodes[donorNodeIndex].Gid);
            CopyOverRelatedNeuronDefinitions(recipient, donor, offspringContext, parent1OriginalToCopiedNodeMap, parent2OriginalToCopiedNodeMap);

            return offspringContext;
        }

        private void CopyOverRelatedConnections(
            SimsGenotype parent1,
            SimsGenotype parent2,
            SimsGenotypeCreationContext offspringContext,
            Dictionary<ulong, ulong> parent1OriginalToCopiedNodeMap,
            Dictionary<ulong, ulong> parent2OriginalToCopiedNodeMap,
            int recipientNodeIndex = -1,
            int recipientConnectionIndex = -1,
            ulong oldDonorNodeGid = 0UL
        )
        {
            int donorOffset = recipientNodeIndex + 1; // Donor nodes start after all recipient nodes.
            for (int i = 0; i < parent1.Connections.Count; i++)
            {
                Connection c = parent1.Connections[i];

                // Does the copied version of my original parent node exist?
                if (!parent1OriginalToCopiedNodeMap.TryGetValue(c.ParentNodeGid, out ulong newParentGid))
                    continue; // Parent node does not exist in the offspring context.

                // What child node are we now pointing to?
                ulong newChildGid;
                if (recipientConnectionIndex == i) // This is the connection we are grafting from the recipient.
                {
                    if (!parent2OriginalToCopiedNodeMap.TryGetValue(oldDonorNodeGid, out newChildGid))
                        throw new InvalidOperationException($"Failed to find new child GID for donor connection. oldDonorNodeGid: {oldDonorNodeGid}");
                }
                else
                {
                    // What is the index of the child node relative to the parent (can be negative)?
                    int oldAbsoluteParentNodeIndex = parent1.Nodes.FindIndex(n => n.Gid == c.ParentNodeGid);
                    int oldAbsoluteChildNodeIndex = parent1.Nodes.FindIndex(n => n.Gid == c.ChildNodeGid);
                    int relativeChildIndex = oldAbsoluteChildNodeIndex - oldAbsoluteParentNodeIndex;

                    int newAbsoluteParentNodeIndex = offspringContext.Nodes.FindIndex(n => n.Gid == newParentGid);
                    int newAbsoluteChildNodeIndex = newAbsoluteParentNodeIndex + relativeChildIndex;
                    if (newAbsoluteChildNodeIndex < 0 || newAbsoluteChildNodeIndex >= offspringContext.Nodes.Count)
                        continue; // Connection now points out of bounds.
                    newChildGid = offspringContext.Nodes[newAbsoluteChildNodeIndex].Gid;
                }

                Connection copiedConnection = new(
                    newParentGid,
                    newChildGid,
                    c.ParentFace,
                    c.Position,
                    c.Orientation,
                    c.Scale,
                    c.ReflectionX,
                    c.ReflectionY,
                    c.ReflectionZ,
                    c.TerminalOnly
                );
                offspringContext.Connections.Add(copiedConnection);
            }

            for (int i = 0; i < parent2.Connections.Count; i++)
            {
                Connection c = parent2.Connections[i];

                // Does the copied version of my original parent node exist?
                if (!parent2OriginalToCopiedNodeMap.TryGetValue(c.ParentNodeGid, out ulong newParentGid))
                    continue; // Parent node does not exist in the offspring context.

                // What child node are we now pointing to?
                // What is the index of the child node relative to the parent (can be negative)?
                int oldAbsoluteParentNodeIndex = parent2.Nodes.FindIndex(n => n.Gid == c.ParentNodeGid);
                int oldAbsoluteChildNodeIndex = parent2.Nodes.FindIndex(n => n.Gid == c.ChildNodeGid);
                int relativeChildIndex = oldAbsoluteChildNodeIndex - oldAbsoluteParentNodeIndex;

                int newAbsoluteParentNodeIndex = offspringContext.Nodes.FindIndex(n => n.Gid == newParentGid);
                int newAbsoluteChildNodeIndex = newAbsoluteParentNodeIndex + relativeChildIndex + donorOffset; // Adjust for any grafting offset.
                if (newAbsoluteChildNodeIndex < 0 || newAbsoluteChildNodeIndex >= offspringContext.Nodes.Count)
                    continue; // Connection now points out of bounds.
                ulong newChildGid = offspringContext.Nodes[newAbsoluteChildNodeIndex].Gid;

                Connection copiedConnection = new(
                    newParentGid,
                    newChildGid,
                    c.ParentFace,
                    c.Position,
                    c.Orientation,
                    c.Scale,
                    c.ReflectionX,
                    c.ReflectionY,
                    c.ReflectionZ,
                    c.TerminalOnly
                );
                offspringContext.Connections.Add(copiedConnection);
            }
        }

        private void CopyOverRelatedNeuronDefinitions(
            SimsGenotype parent1, SimsGenotype parent2,
            SimsGenotypeCreationContext offspringContext,
            Dictionary<ulong, ulong> parent1OriginalToCopiedNodeMap,
            Dictionary<ulong, ulong> parent2OriginalToCopiedNodeMap
        )
        {
            for (int i = 0; i < parent1.NeuronDefinitions.Count; i++)
            {
                NeuronDefinition sourceNeuron = parent1.NeuronDefinitions[i];
                ulong newContainerGid;
                if (sourceNeuron.ContainerGid == SimsGenotype.BRAIN_GID)
                    newContainerGid = SimsGenotype.BRAIN_GID; // We only take brain neurons from parent1.
                else if (!parent1OriginalToCopiedNodeMap.TryGetValue(sourceNeuron.ContainerGid, out newContainerGid))
                    continue; // Skip this neuron definition if its container node wasn't copied.

                NeuronDefinition copiedNeuronDefinition = new(
                    newContainerGid,
                    sourceNeuron.ActivationFunction,
                    sourceNeuron.Inputs
                );
                offspringContext.NeuronDefinitions.Add(copiedNeuronDefinition);
            }

            for (int i = 0; i < parent2.NeuronDefinitions.Count; i++)
            {
                NeuronDefinition sourceNeuron = parent2.NeuronDefinitions[i];
                if (sourceNeuron.ContainerGid == SimsGenotype.BRAIN_GID)
                    continue; // We only take brain neurons from parent1.
                if (!parent2OriginalToCopiedNodeMap.TryGetValue(sourceNeuron.ContainerGid, out ulong newContainerGid))
                    continue; // Skip this neuron definition if its container node wasn't copied.

                NeuronDefinition copiedNeuronDefinition = new(
                    newContainerGid,
                    sourceNeuron.ActivationFunction,
                    sourceNeuron.Inputs
                );
                offspringContext.NeuronDefinitions.Add(copiedNeuronDefinition);
            }
        }
    }
}
