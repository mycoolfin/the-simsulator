using System;
using System.Collections.Generic;
using System.Linq;

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
            if (!neuronDefinitionMap.TryGetValue(SimsGenotype.BRAIN_GID, out List<NeuronDefinition> brainNeuronDefs)) brainNeuronDefs = new();
            Brain brain = new(brainNeuronDefs.Select(nd => new Neuron(nd.ActivationFunction)).ToList());

            // Create the limbs.
            List<Limb> limbs = new();
            Vector3 initialScale = Vector3.One;
            RecursivelyBuildLimbTree(
                nodeMap,
                connectionMap,
                neuronDefinitionMap,
                genotype.Nodes[0], // Start with the first node.
                null, // No parent limb for the root.
                null, // No parent connection for the root.
                initialScale,
                limbs,
                limbToNodeGidMap,
                parentToChildLimbsMap,
                childToParentLimbMap,
                receiverToInputDefinitionSetGidMap
            );

            // Wire up nervous system. (sensors and neurons to neurons and actuators)
            foreach (Neuron neuron in brain.Neurons)
            {
                InputSetDefinition inputsDef = receiverToInputDefinitionSetGidMap[neuron];
                WiringTool.WireUp(neuron, inputsDef.A, inputsDef.B, inputsDef.C, null, null, null, brain);
            }
            foreach (Limb thisLimb in limbs)
            {
                childToParentLimbMap.TryGetValue(thisLimb, out Limb parentLimb);
                parentToChildLimbsMap.TryGetValue(parentLimb, out List<Limb> childLimbs);
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

        private void RecursivelyBuildLimbTree(
            Dictionary<ulong, Node> nodeMap, // Node Gid -> Node.
            Dictionary<ulong, List<Connection>> connectionMap, // Parent Node Gid -> List<Connection>.
            Dictionary<ulong, List<NeuronDefinition>> neuronDefinitionMap, // Node Gid -> List<NeuronDefinition>.
            Node node,
            Connection? connectionToParent,
            Limb parentLimb,
            Vector3 parentScale,
            List<Limb> limbs,
            Dictionary<Limb, ulong> limbToNodeGidMap,
            Dictionary<Limb, List<Limb>> parentToChildLimbsMap,
            Dictionary<Limb, Limb> childToParentLimbMap,
            Dictionary<ISignalReceiver, InputSetDefinition> receiverToInputDefinitionSetGidMap,
            int nodeRecursionDepth = 0,
            bool mirrorX = false,
            bool mirrorY = false,
            bool mirrorZ = false
        )
        {
            // Skip this node if we have exceeded its recursion limit.
            if (nodeRecursionDepth > node.RecursiveLimit)
                return;

            Limb newLimb = LimbCreator.CreateChildLimb(parentLimb, ref parentScale, node, connectionToParent, neuronDefinitionMap[node.Gid], mirrorX, mirrorY, mirrorZ, receiverToInputDefinitionSetGidMap);

            // Skip this limb if it would overlap with an existing, non-parent limb.
            List<Limb> limbCollisions = SpawnCollisionHandler.GetLimbCollisions(newLimb, limbs);
            if (limbCollisions.Any(l => l != parentLimb))
                return; 

            // Add the new limb to the lists and maps.
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
            foreach (Connection c in connectionMap[node.Gid])
            {
                if (c.TerminalOnly && !nodeRecursionLimitReached)
                    continue;

                Node childNode = nodeMap[c.ChildNodeGid];
                int newRecursionDepth = childNode.Gid == node.Gid ? nodeRecursionDepth + 1 : 0;

                List<Action> limbCreationActions = new()
                {
                    // The non-mirrored child.
                    () => RecursivelyBuildLimbTree(
                        nodeMap, connectionMap, neuronDefinitionMap, childNode, c, newLimb, parentScale,
                        limbs, limbToNodeGidMap, parentToChildLimbsMap, childToParentLimbMap, receiverToInputDefinitionSetGidMap,
                        newRecursionDepth
                    )
                };

                // Loop over all non-zero combinations of reflection flags.
                for (int i = 1; i < 8; i++)
                {
                    bool isXMirrored = (i & 1) != 0 && c.ReflectionX;
                    bool isYMirrored = (i & 2) != 0 && c.ReflectionY;
                    bool isZMirrored = (i & 4) != 0 && c.ReflectionZ;
                    if (isXMirrored || isYMirrored || isZMirrored)
                    {
                        limbCreationActions.Add(() => RecursivelyBuildLimbTree(
                            nodeMap, connectionMap, neuronDefinitionMap, childNode, c, newLimb, parentScale,
                            limbs, limbToNodeGidMap, parentToChildLimbsMap, childToParentLimbMap, receiverToInputDefinitionSetGidMap,
                            newRecursionDepth, isXMirrored, isYMirrored, isZMirrored
                        ));
                    }
                }

                // If adding these limbs would exceed the max limb count, skip the entire connection.
                // This way, we avoid partially created connections that might lead to inconsistent states.
                if (limbCreationActions.Count + limbs.Count > SimsPhenotype.MaxLimbCount)
                    continue;

                limbCreationActions.ForEach(action => action.Invoke());
            }
        }
    }
}
