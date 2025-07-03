using System;
using System.Linq;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public static class LimbCreator
    {
        private readonly struct CreatedLimbData
        {
            public Limb Limb { get; }
            public Vector3 CurrentScale { get; }
            public bool SwapX { get; }

            public CreatedLimbData(Limb limb, Vector3 currentScale, bool swapX)
            {
                Limb = limb;
                CurrentScale = currentScale;
                SwapX = swapX;
            }
        }

        public static List<Limb> CreateLimbs(
            SimsGenotype genotype, Dictionary<Limb, List<Limb>> parentToChildLimbsMap,
            Dictionary<Limb, Limb> childToParentLimbMap,
            Dictionary<ISignalReceiver, InputSetDefinition> receiverToInputDefinitionSetMap
        )
        {
            Dictionary<ulong, Node> nodeMap = genotype.Nodes.ToDictionary(n => n.Gid);
            Dictionary<ulong, List<Connection>> connectionMap = genotype.Connections
                .GroupBy(c => c.ParentNodeGid)
                .ToDictionary(g => g.Key, g => g.ToList());
            Dictionary<ulong, List<NeuronDefinition>> neuronDefinitionMap = genotype.NeuronDefinitions
                .GroupBy(nd => nd.ContainerGid)
                .ToDictionary(g => g.Key, g => g.ToList());
            Dictionary<Limb, ulong> limbToNodeGidMap = new();

            Queue<Action> buildQueue = new(); // For breadth-first limb creation.
            List<Limb> limbs = new();
            BuildLimbTree(
                nodeMap,
                connectionMap,
                neuronDefinitionMap,
                genotype.Nodes[0], // Start with the first node.
                null, // No parent limb for the root.
                new(null, Vector3.One, false),
                buildQueue,
                limbs,
                limbToNodeGidMap,
                parentToChildLimbsMap,
                childToParentLimbMap,
                receiverToInputDefinitionSetMap
            );
            while (buildQueue.Count > 0)
                buildQueue.Dequeue().Invoke();

            return limbs;
        }

        private static void BuildLimbTree(
            Dictionary<ulong, Node> nodeMap, // Node Gid -> Node.
            Dictionary<ulong, List<Connection>> connectionMap, // Parent Node Gid -> List<Connection>.
            Dictionary<ulong, List<NeuronDefinition>> neuronDefinitionMap, // Node Gid -> List<NeuronDefinition>.
            Node node,
            Connection? connectionToParent,
            CreatedLimbData parentData,
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
            bool mirrorRight = isParentLimbFaceRightMirrored ^ isFaceRightMirrored;
            bool mirrorUp = isParentLimbFaceUpMirrored ^ isFaceUpMirrored;
            bool mirrorForward = isParentLimbFaceForwardMirrored ^ isFaceForwardMirrored;
            CreatedLimbData newLimbData = CreateChildLimb(
                parentData, node, connectionToParent, neuronDefinitions,
                mirrorRight, mirrorUp, mirrorForward,
                receiverToInputDefinitionSetGidMap
            );

            // Skip this limb if any of its dimensions are out of bounds.
            if (newLimbData.Limb.Dimensions.X < SimsPhenotype.MIN_LIMB_DIMENSION ||
                newLimbData.Limb.Dimensions.Y < SimsPhenotype.MIN_LIMB_DIMENSION ||
                newLimbData.Limb.Dimensions.Z < SimsPhenotype.MIN_LIMB_DIMENSION ||
                newLimbData.Limb.Dimensions.X > SimsPhenotype.MAX_LIMB_DIMENSION ||
                newLimbData.Limb.Dimensions.Y > SimsPhenotype.MAX_LIMB_DIMENSION ||
                newLimbData.Limb.Dimensions.Z > SimsPhenotype.MAX_LIMB_DIMENSION)
                return;

            // Skip this limb if it would overlap with an existing, non-parent limb.
            List<Limb> limbCollisions = SpawnCollisionHandler.GetLimbCollisions(newLimbData.Limb, limbs);
            if (limbCollisions.Any(l => l != parentData.Limb))
                return;

            // Add the new limb to the lists and maps.
            limbs.Add(newLimbData.Limb);
            limbToNodeGidMap[newLimbData.Limb] = node.Gid;
            if (parentData.Limb != null)
            {
                parentToChildLimbsMap.TryGetValue(parentData.Limb, out List<Limb> childLimbs);
                if (childLimbs == null)
                {
                    childLimbs = new List<Limb>();
                    parentToChildLimbsMap[parentData.Limb] = childLimbs;
                }
                childLimbs.Add(newLimbData.Limb);
                childToParentLimbMap[newLimbData.Limb] = parentData.Limb;
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
                    List<(bool reflectX, bool reflectY, bool reflectZ)> childLimbVariants = new()
                    {
                        (false, false, false) // Start with no additional mirroring for this connection.
                    };
                    if (c.ReflectionX)
                    {
                        int count = childLimbVariants.Count;
                        for (int i = 0; i < count; i++)
                            childLimbVariants.Add((!childLimbVariants[i].reflectX, childLimbVariants[i].reflectY, childLimbVariants[i].reflectZ));
                    }
                    if (c.ReflectionY)
                    {
                        int count = childLimbVariants.Count;
                        for (int i = 0; i < count; i++)
                            childLimbVariants.Add((childLimbVariants[i].reflectX, !childLimbVariants[i].reflectY, childLimbVariants[i].reflectZ));
                    }
                    if (c.ReflectionZ)
                    {
                        int count = childLimbVariants.Count;
                        for (int i = 0; i < count; i++)
                            childLimbVariants.Add((childLimbVariants[i].reflectX, childLimbVariants[i].reflectY, !childLimbVariants[i].reflectZ));
                    }

                    // If adding these limbs would exceed the max limb count, skip the entire connection.
                    // This way, we avoid partially created connections that might lead to inconsistent states.
                    if (childLimbVariants.Count + limbs.Count > SimsPhenotype.MAX_LIMBS)
                        continue;

                    foreach (var (reflectX, reflectY, reflectZ) in childLimbVariants)
                    {
                        buildQueue.Enqueue(() => BuildLimbTree(
                            nodeMap, connectionMap, neuronDefinitionMap, childNode, c, newLimbData,
                            buildQueue, limbs, limbToNodeGidMap, parentToChildLimbsMap, childToParentLimbMap, receiverToInputDefinitionSetGidMap,
                            newRecursionDepth,
                            mirrorRight, mirrorUp, mirrorForward,
                            reflectX, reflectY, reflectZ
                        ));
                    }
                }
            }
        }

        private static CreatedLimbData CreateChildLimb(CreatedLimbData parentData, Node node, Connection? connection, List<NeuronDefinition> neuronDefinitions,
            bool mirrorRight, bool mirrorUp, bool mirrorForward,
            Dictionary<ISignalReceiver, InputSetDefinition> receiverToInputDefinitionSetGidMap
        )
        {
            // Compute new scale and dimensions.
            Vector3 currentScale = connection == null ? parentData.CurrentScale : Vector3.Scale(parentData.CurrentScale, connection.Value.Scale);
            Vector3 dimensions = new(
                node.Dimensions.X * currentScale.X,
                node.Dimensions.Y * currentScale.Y,
                node.Dimensions.Z * currentScale.Z
            );

            // Create new limb with absolute dimensions.
            Limb newLimb = new(new Vector3(dimensions.X, dimensions.Y, dimensions.Z))
            {
                debugMirroredX = mirrorRight,
                debugMirroredY = mirrorUp,
                debugMirroredZ = mirrorForward
            };

            bool swapX = false;
            if (connection != null)
            {
                // Compute face axes and normal in parent's coordinate system.
                GetParentSpaceFaceAxes(connection.Value.ParentFace, out Vector3 faceRight, out Vector3 faceUp, out Vector3 faceNormal);

                // If the parent has swapped handedness, swap the X axis.
                if (parentData.SwapX)
                {
                    faceRight = new Vector3(-faceRight.X, faceRight.Y, faceRight.Z);
                    faceUp = new Vector3(-faceUp.X, faceUp.Y, faceUp.Z);
                    faceNormal = new Vector3(-faceNormal.X, faceNormal.Y, faceNormal.Z);
                }

                // If mirroring, reflect the appropriate axes.
                if (mirrorRight) faceRight = -faceRight;
                if (mirrorUp) faceUp = -faceUp;
                if (mirrorForward) faceNormal = -faceNormal;

                // Compute anchor in parent space.
                Vector3 parentHalfExtents = parentData.Limb.Dimensions * 0.5f;
                Vector3 parentSpaceAnchor = Vector3.Scale(parentHalfExtents, faceNormal)
                                            + Vector3.Scale(parentHalfExtents, faceRight) * connection.Value.Position.X
                                            + Vector3.Scale(parentHalfExtents, faceUp) * connection.Value.Position.Y;

                // Orient joint axes in parent space.
                float parity = (mirrorRight ? -1 : 1) * (mirrorUp ? -1 : 1) * (mirrorForward ? -1 : 1) * (parentData.SwapX ? -1 : 1);
                Quaternion jointRotation = Quaternion.AngleAxis(connection.Value.Orientation.Z * parity, faceNormal);
                jointRotation *= Quaternion.AngleAxis(connection.Value.Orientation.Y * parity, faceUp);
                jointRotation *= Quaternion.AngleAxis(connection.Value.Orientation.X * parity, faceRight);

                Vector3 parentSpaceXAxis = jointRotation * faceRight;
                Vector3 parentSpaceYAxis = jointRotation * faceUp;
                Vector3 parentSpaceZAxis = jointRotation * faceNormal;

                Quaternion parentSpaceRotation = jointRotation * Quaternion.LookRotation(faceNormal, faceUp);

                // Place child -Z face at anchor in parent space.
                float halfDepth = Math.Abs(dimensions.Z) * 0.5f;
                Vector3 parentSpaceChildPosition = parentSpaceAnchor + parentSpaceRotation * new Vector3(0f, 0f, halfDepth);

                // Translate to world space.
                Vector3 worldChildPosition = parentData.Limb.Position + parentData.Limb.Rotation * parentSpaceChildPosition;
                Quaternion worldChildRotation = parentData.Limb.Rotation * parentSpaceRotation;

                // Set transform.
                newLimb.SetPositionAndRotation(worldChildPosition, worldChildRotation);

                // Calculate minimum cross-sectional area between the parent and child faces.
                float parentFaceWidth = 2 * Math.Abs(Vector3.Dot(faceRight, parentHalfExtents));
                float parentFaceHeight = 2 * Math.Abs(Vector3.Dot(faceUp, parentHalfExtents));
                float parentCrossSectionalArea = parentFaceWidth * parentFaceHeight;
                float childCrossSectionalArea = newLimb.Dimensions.X * newLimb.Dimensions.Y; // Child face is always the -Z face.
                float minCrossSectionalArea = Math.Min(parentCrossSectionalArea, childCrossSectionalArea);

                // Create and assign joint.
                Joint joint = new(
                    node.JointDefinition.JointType,
                    parentData.Limb,
                    newLimb,
                    parentSpaceAnchor,
                    parentSpaceXAxis,
                    parentSpaceYAxis,
                    parentSpaceZAxis,
                    node.JointDefinition.AngleLimits,
                    minCrossSectionalArea
                );
                newLimb.SetJoint(joint);
                if (joint.XAxisController != null)
                    receiverToInputDefinitionSetGidMap[joint.XAxisController.Actuator] = node.JointDefinition.XAxisInputs;
                if (joint.YAxisController != null)
                    receiverToInputDefinitionSetGidMap[joint.YAxisController.Actuator] = node.JointDefinition.YAxisInputs;
                if (joint.ZAxisController != null)
                    receiverToInputDefinitionSetGidMap[joint.ZAxisController.Actuator] = node.JointDefinition.ZAxisInputs;

                // Check if there was a handedness swap.
                swapX = Vector3.Dot(Vector3.Cross(faceNormal, faceUp), faceRight) > 0f;
                newLimb.debugSwappedX = swapX;
            }

            // Create and assign unwired neurons.
            List<Neuron> neurons = new();
            if (neuronDefinitions != null)
            {
                foreach (NeuronDefinition nd in neuronDefinitions)
                {
                    Neuron neuron = new(nd.ActivationFunction);
                    neurons.Add(neuron);
                    receiverToInputDefinitionSetGidMap[neuron] = nd.Inputs;
                }
            }
            newLimb.SetNeurons(neurons);

            return new CreatedLimbData(newLimb, currentScale, swapX);
        }

        private static void GetParentSpaceFaceAxes(int face, out Vector3 faceRight, out Vector3 faceUp, out Vector3 faceNormal)
        {
            switch (face)
            {
                case 0: // -X (left face)
                    faceRight = -Vector3.Forward;
                    faceUp = Vector3.Up;
                    faceNormal = -Vector3.Right;
                    break;
                case 1: // -Y (bottom face)
                    faceRight = -Vector3.Right;
                    faceUp = Vector3.Forward;
                    faceNormal = -Vector3.Up;
                    break;
                case 2: // -Z (back face)
                    faceRight = Vector3.Right;
                    faceUp = Vector3.Up;
                    faceNormal = -Vector3.Forward;
                    break;
                case 3: // +X (right face)
                    faceRight = Vector3.Forward;
                    faceUp = Vector3.Up;
                    faceNormal = Vector3.Right;
                    break;
                case 4: // +Y (top face)
                    faceRight = Vector3.Right;
                    faceUp = Vector3.Forward;
                    faceNormal = Vector3.Up;
                    break;
                case 5: // +Z (front face)
                    faceRight = -Vector3.Right;
                    faceUp = Vector3.Up;
                    faceNormal = Vector3.Forward;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(face), $"Invalid face index {face}. Must be 0-5.");
            }
        }
    }
}
