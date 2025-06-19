using System;
using System.Linq;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public class SimsPhenotypeFactory : IPhenotypeFactory<SimsGenotype, SimsPhenotype>
    {
        public SimsPhenotype ConstructPhenotype(SimsGenotype genotype)
        {
            Dictionary<ulong, Node> nodeMap = genotype.Nodes.ToDictionary(n => n.Gid);
            Dictionary<ulong, List<Connection>> connectionMap = genotype.Connections
                .GroupBy(c => c.ParentNodeGid)
                .ToDictionary(g => g.Key, g => g.ToList());
            Dictionary<ulong, List<NeuronDefinition>> neuronDefinitionMap = genotype.NeuronDefinitions
                .GroupBy(nd => nd.ContainerGid)
                .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<Limb, ulong> limbToNodeGidMap = new();
            Dictionary<Limb, List<Limb>> parentToChildLimbsMap = new();
            Dictionary<Limb, Limb> childToParentLimbMap = new();
            Dictionary<ISignalReceiver, InputSetDefinition> receiverToInputDefinitionSetGidMap = new();

            // Create the brain.
            Brain brain = BrainCreator.CreateBrain(genotype.NeuronDefinitions, receiverToInputDefinitionSetGidMap);

            // Create the limbs.
            Queue<Action> buildQueue = new(); // For breadth-first limb creation.
            List<Limb> limbs = new();
            Vector3 initialScale = Vector3.One;
            BuildLimbTree(
                nodeMap,
                connectionMap,
                neuronDefinitionMap,
                genotype.Nodes[0], // Start with the first node.
                null, // No parent limb for the root.
                null, // No parent connection for the root.
                initialScale,
                buildQueue,
                limbs,
                limbToNodeGidMap,
                parentToChildLimbsMap,
                childToParentLimbMap,
                receiverToInputDefinitionSetGidMap
            );
            while (buildQueue.Count > 0)
                buildQueue.Dequeue().Invoke();

            // Wire up nervous system.
            foreach (Neuron neuron in brain.Neurons)
            {
                InputSetDefinition inputsDef = receiverToInputDefinitionSetGidMap[neuron];
                WiringTool.WireUp(neuron, inputsDef.A, inputsDef.B, inputsDef.C, null, null, null, brain);
            }
            foreach (Limb thisLimb in limbs)
            {
                childToParentLimbMap.TryGetValue(thisLimb, out Limb parentLimb);
                parentToChildLimbsMap.TryGetValue(thisLimb, out List<Limb> childLimbs);
                foreach (Neuron neuron in thisLimb.Neurons)
                {
                    InputSetDefinition inputsDef = receiverToInputDefinitionSetGidMap[neuron];
                    WiringTool.WireUp(neuron, inputsDef.A, inputsDef.B, inputsDef.C, thisLimb, parentLimb, childLimbs, brain);
                }
                foreach (ActuatorBase actuator in thisLimb.Actuators)
                {
                    InputSetDefinition inputsDef = receiverToInputDefinitionSetGidMap[actuator];
                    WiringTool.WireUp(actuator, inputsDef.A, inputsDef.B, inputsDef.C, thisLimb, parentLimb, childLimbs, brain);
                }
            }

            return new SimsPhenotype(brain, limbs);
        }

        private void BuildLimbTree(
            Dictionary<ulong, Node> nodeMap, // Node Gid -> Node.
            Dictionary<ulong, List<Connection>> connectionMap, // Parent Node Gid -> List<Connection>.
            Dictionary<ulong, List<NeuronDefinition>> neuronDefinitionMap, // Node Gid -> List<NeuronDefinition>.
            Node node,
            Connection? connectionToParent,
            Limb parentLimb,
            Vector3 parentScale,
            Queue<Action> buildQueue,
            List<Limb> limbs,
            Dictionary<Limb, ulong> limbToNodeGidMap,
            Dictionary<Limb, List<Limb>> parentToChildLimbsMap,
            Dictionary<Limb, Limb> childToParentLimbMap,
            Dictionary<ISignalReceiver, InputSetDefinition> receiverToInputDefinitionSetGidMap,
            int nodeRecursionDepth = 0,
            bool isParentLimbFaceRightMirrored = false,
            bool isParentLimbFaceUpMirrored = false,
            bool isParentLimbFaceForwardMirrored = false,
            bool isFaceRightMirrored = false,
            bool isFaceUpMirrored = false,
            bool isFaceForwardMirrored = false
        )
        {
            neuronDefinitionMap.TryGetValue(node.Gid, out List<NeuronDefinition> neuronDefinitions);
            CreatedLimb createdLimb = LimbCreator.CreateChildLimb(parentLimb, parentScale, node, connectionToParent, neuronDefinitions, isParentLimbFaceRightMirrored, isParentLimbFaceUpMirrored, isParentLimbFaceForwardMirrored, isFaceRightMirrored, isFaceUpMirrored, isFaceForwardMirrored, receiverToInputDefinitionSetGidMap);

            // Skip this limb if it would overlap with an existing, non-parent limb.
            // List<Limb> limbCollisions = SpawnCollisionHandler.GetLimbCollisions(newLimb, limbs);
            // if (limbCollisions.Any(l => l != parentLimb))
            //     return; // TODO: uncomment

            // Add the new limb to the lists and maps.
            Limb newLimb = createdLimb.Limb;
            limbs.Add(newLimb);
            limbToNodeGidMap[newLimb] = node.Gid;
            if (parentLimb != null)
            {
                parentToChildLimbsMap.TryGetValue(parentLimb, out List<Limb> childLimbs);
                if (childLimbs == null)
                {
                    childLimbs = new List<Limb>();
                    parentToChildLimbsMap[parentLimb] = childLimbs;
                }
                childLimbs.Add(newLimb);
                childToParentLimbMap[newLimb] = parentLimb;
            }

            // Iterate over the connections for this node and recursively build child limbs.
            bool nodeRecursionLimitReached = nodeRecursionDepth == node.RecursiveLimit;
            if (connectionMap.TryGetValue(node.Gid, out List<Connection> connections))
            {
                foreach (Connection c in connections)
                {
                    if (c.TerminalOnly && !nodeRecursionLimitReached)
                        continue;

                    Node childNode = nodeMap[c.ChildNodeGid];
                    int newRecursionDepth = childNode.Gid == node.Gid ? nodeRecursionDepth + 1 : 0;
                    // Skip this connection if we have exceeded the node recursion limit.
                    if (newRecursionDepth > childNode.RecursiveLimit)
                        return;

                    // Calculate all child limb variants.
                    List<(bool mirrorX, bool mirrorY, bool mirrorZ)> childLimbVariants = new()
                    {
                        (false, false, false) // Start with no additional mirroring for this connection.
                    };
                    if (c.ReflectionX)
                    {
                        int count = childLimbVariants.Count;
                        for (int i = 0; i < count; i++)
                            childLimbVariants.Add((!childLimbVariants[i].mirrorX, childLimbVariants[i].mirrorY, childLimbVariants[i].mirrorZ));
                    }
                    if (c.ReflectionY)
                    {
                        int count = childLimbVariants.Count;
                        for (int i = 0; i < count; i++)
                            childLimbVariants.Add((childLimbVariants[i].mirrorX, !childLimbVariants[i].mirrorY, childLimbVariants[i].mirrorZ));
                    }
                    if (c.ReflectionZ)
                    {
                        int count = childLimbVariants.Count;
                        for (int i = 0; i < count; i++)
                            childLimbVariants.Add((childLimbVariants[i].mirrorX, childLimbVariants[i].mirrorY, !childLimbVariants[i].mirrorZ));
                    }

                    // If adding these limbs would exceed the max limb count, skip the entire connection.
                    // This way, we avoid partially created connections that might lead to inconsistent states.
                    // if (childLimbVariants.Count + limbs.Count > SimsPhenotype.MaxLimbCount)
                    //     continue; // TODO: uncomment

                    foreach (var v in childLimbVariants)
                    {
                        buildQueue.Enqueue(() => BuildLimbTree(
                            nodeMap, connectionMap, neuronDefinitionMap, childNode, c, newLimb, createdLimb.CurrentScale,
                            buildQueue, limbs, limbToNodeGidMap, parentToChildLimbsMap, childToParentLimbMap, receiverToInputDefinitionSetGidMap,
                            newRecursionDepth, 
                            // Pass the current limb's mirroring state as the accumulated parent context
                            isFaceRightMirrored, isFaceUpMirrored, isFaceForwardMirrored, 
                            // The variant flags determine the new mirroring to apply to this specific child
                            v.mirrorX, v.mirrorY, v.mirrorZ
                        ));
                    }
                }
            }
        }
    }
}
