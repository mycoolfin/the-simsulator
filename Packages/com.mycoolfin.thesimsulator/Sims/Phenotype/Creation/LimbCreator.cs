using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    using Genotype;

    // NOTE: This has been AI-optimised, hence the absurd length.
    // A refactor may be necessary in the future to improve readability and maintainability.
    public static class LimbCreator
    {
        // Pre-computed face axis lookup tables for better cache performance.
        private static readonly Vector3[] FaceRightVectors = new Vector3[]
        {
            new(0f, 0f, -1f), // -X
            new(-1f, 0f, 0f), // -Y  
            new(1f, 0f, 0f),  // -Z
            new(0f, 0f, 1f),  // +X
            new(1f, 0f, 0f),  // +Y
            new(-1f, 0f, 0f)  // +Z
        };

        private static readonly Vector3[] FaceUpVectors = new Vector3[]
        {
            new(0f, 1f, 0f),  // -X
            new(0f, 0f, 1f),  // -Y
            new(0f, 1f, 0f),  // -Z
            new(0f, 1f, 0f),  // +X
            new(0f, 0f, 1f),  // +Y
            new(0f, 1f, 0f)   // +Z
        };

        private static readonly Vector3[] FaceNormalVectors = new Vector3[]
        {
            new(-1f, 0f, 0f), // -X
            new(0f, -1f, 0f), // -Y
            new(0f, 0f, -1f), // -Z
            new(1f, 0f, 0f),  // +X
            new(0f, 1f, 0f),  // +Y
            new(0f, 0f, 1f)   // +Z
        };

        // Memory pool for frequent allocations (static to persist across calls).
        private static readonly ConcurrentStack<List<Neuron>> NeuronListPool = new();
        private static readonly ConcurrentStack<List<(bool, bool, bool)>> ReflectionVariantsPool = new();
        private static readonly ConcurrentStack<Dictionary<ulong, Node>> NodeMapPool = new();
        private static readonly ConcurrentStack<Dictionary<ulong, List<Connection>>> ConnectionMapPool = new();
        private static readonly ConcurrentStack<Dictionary<ulong, List<NeuronDefinition>>> NeuronDefinitionMapPool = new();
        private static readonly ConcurrentStack<List<Connection>> ConnectionListPool = new();
        private static readonly ConcurrentStack<List<NeuronDefinition>> NeuronDefinitionListPool = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<Neuron> GetPooledNeuronList()
        {
            if (NeuronListPool.TryPop(out List<Neuron> list))
            {
                list.Clear();
                return list;
            }
            return new List<Neuron>(Node.MAX_NEURON_DEFINITIONS);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnPooledNeuronList(List<Neuron> list)
        {
            if (list != null && NeuronListPool.Count < SimsPhenotype.MAX_LIMBS)
                NeuronListPool.Push(list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<(bool, bool, bool)> GetPooledReflectionList()
        {
            if (ReflectionVariantsPool.TryPop(out List<(bool, bool, bool)> list))
            {
                list.Clear();
                return list;
            }
            return new List<(bool, bool, bool)>(8); // Maximum reflection variants per connection (2³ for X,Y,Z reflections).
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnPooledReflectionList(List<(bool, bool, bool)> list)
        {
            if (list != null && ReflectionVariantsPool.Count < SimsPhenotype.MAX_LIMBS)
                ReflectionVariantsPool.Push(list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<ulong, Node> GetPooledNodeMap()
        {
            if (NodeMapPool.TryPop(out Dictionary<ulong, Node> map))
            {
                map.Clear();
                return map;
            }
            return new Dictionary<ulong, Node>(SimsPhenotype.MAX_LIMBS);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnPooledNodeMap(Dictionary<ulong, Node> map)
        {
            if (map != null && NodeMapPool.Count < SimsPhenotype.MAX_LIMBS)
                NodeMapPool.Push(map);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<ulong, List<Connection>> GetPooledConnectionMap()
        {
            if (ConnectionMapPool.TryPop(out Dictionary<ulong, List<Connection>> map))
            {
                map.Clear();
                return map;
            }
            return new Dictionary<ulong, List<Connection>>(SimsPhenotype.MAX_LIMBS);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnPooledConnectionMap(Dictionary<ulong, List<Connection>> map)
        {
            if (map != null && ConnectionMapPool.Count < SimsPhenotype.MAX_LIMBS)
                ConnectionMapPool.Push(map);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Dictionary<ulong, List<NeuronDefinition>> GetPooledNeuronDefinitionMap()
        {
            if (NeuronDefinitionMapPool.TryPop(out Dictionary<ulong, List<NeuronDefinition>> map))
            {
                map.Clear();
                return map;
            }
            return new Dictionary<ulong, List<NeuronDefinition>>(SimsPhenotype.MAX_LIMBS);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnPooledNeuronDefinitionMap(Dictionary<ulong, List<NeuronDefinition>> map)
        {
            if (map != null && NeuronDefinitionMapPool.Count < SimsPhenotype.MAX_LIMBS)
                NeuronDefinitionMapPool.Push(map);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<Connection> GetPooledConnectionList()
        {
            if (ConnectionListPool.TryPop(out List<Connection> list))
            {
                list.Clear();
                return list;
            }
            return new List<Connection>(Node.MAX_CONNECTIONS);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnPooledConnectionList(List<Connection> list)
        {
            if (list != null && ConnectionListPool.Count < SimsPhenotype.MAX_LIMBS * 4)
                ConnectionListPool.Push(list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<NeuronDefinition> GetPooledNeuronDefinitionList()
        {
            if (NeuronDefinitionListPool.TryPop(out List<NeuronDefinition> list))
            {
                list.Clear();
                return list;
            }
            return new List<NeuronDefinition>(Node.MAX_NEURON_DEFINITIONS);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnPooledNeuronDefinitionList(List<NeuronDefinition> list)
        {
            if (list != null && NeuronDefinitionListPool.Count < SimsPhenotype.MAX_LIMBS * 4)
                NeuronDefinitionListPool.Push(list);
        }
        private readonly struct CreatedLimbData
        {
            public readonly Limb Limb;
            public readonly Vector3 CurrentScale;
            public readonly bool SwapX;

            public CreatedLimbData(Limb limb, Vector3 currentScale, bool swapX)
            {
                Limb = limb;
                CurrentScale = currentScale;
                SwapX = swapX;
            }
        }

        // Preallocated work structures to avoid allocations
        private readonly struct LimbCreationWorkspace
        {
            public readonly Dictionary<ulong, Node> NodeMap;
            public readonly Dictionary<ulong, List<Connection>> ConnectionMap;
            public readonly Dictionary<ulong, List<NeuronDefinition>> NeuronDefinitionMap;
            public readonly Dictionary<Limb, ulong> LimbToNodeGidMap;
            public readonly List<Limb> AllCreatedLimbs;
            public readonly Dictionary<Limb, List<Limb>> ParentToChildLimbsMap;
            public readonly Dictionary<Limb, Limb> ChildToParentLimbMap;
            public readonly Dictionary<ISignalReceiver, InputSetDefinition> ReceiverToInputDefinitionSetMap;

            // Pre-allocated work arrays to avoid repeated allocations.
            public readonly List<(bool reflectX, bool reflectY, bool reflectZ)> ReflectionVariantsWorkList;
            public readonly List<Limb> CollisionCheckWorkList;
            public readonly List<Neuron> NeuronCreationWorkList;

            public LimbCreationWorkspace(
                Dictionary<ulong, Node> nodeMap,
                Dictionary<ulong, List<Connection>> connectionMap,
                Dictionary<ulong, List<NeuronDefinition>> neuronDefinitionMap,
                Dictionary<Limb, List<Limb>> parentToChildLimbsMap,
                Dictionary<Limb, Limb> childToParentLimbMap,
                Dictionary<ISignalReceiver, InputSetDefinition> receiverToInputDefinitionSetMap,
                List<(bool reflectX, bool reflectY, bool reflectZ)> reflectionVariantsWorkList,
                List<Neuron> neuronCreationWorkList)
            {
                NodeMap = nodeMap;
                ConnectionMap = connectionMap;
                NeuronDefinitionMap = neuronDefinitionMap;
                LimbToNodeGidMap = new Dictionary<Limb, ulong>();
                AllCreatedLimbs = new List<Limb>();
                ParentToChildLimbsMap = parentToChildLimbsMap;
                ChildToParentLimbMap = childToParentLimbMap;
                ReceiverToInputDefinitionSetMap = receiverToInputDefinitionSetMap;

                // Use provided pooled work structures to avoid allocations.
                ReflectionVariantsWorkList = reflectionVariantsWorkList;
                CollisionCheckWorkList = new List<Limb>(SimsPhenotype.MAX_LIMBS);
                NeuronCreationWorkList = neuronCreationWorkList;
            }
        }

        // Optimized stack-based processing structure with cached frequently accessed values.
        private readonly struct LimbBuildTask
        {
            public readonly Node TargetNode;
            public readonly Connection? ConnectionToParent;
            public readonly CreatedLimbData ParentLimbData;
            public readonly int NodeRecursionDepth;
            public readonly bool IsParentLimbFaceRightMirrored;
            public readonly bool IsParentLimbFaceUpMirrored;
            public readonly bool IsParentLimbFaceForwardMirrored;
            public readonly bool IsFaceRightMirrored;
            public readonly bool IsFaceUpMirrored;
            public readonly bool IsFaceForwardMirrored;

            // Pre-computed mirroring flags to avoid XOR operations in hot path.
            public readonly bool ComputedMirrorRight;
            public readonly bool ComputedMirrorUp;
            public readonly bool ComputedMirrorForward;

            public LimbBuildTask(
                Node targetNode, Connection? connectionToParent, CreatedLimbData parentLimbData,
                int nodeRecursionDepth, bool isParentLimbFaceRightMirrored, bool isParentLimbFaceUpMirrored,
                bool isParentLimbFaceForwardMirrored, bool isFaceRightMirrored, bool isFaceUpMirrored, bool isFaceForwardMirrored)
            {
                TargetNode = targetNode;
                ConnectionToParent = connectionToParent;
                ParentLimbData = parentLimbData;
                NodeRecursionDepth = nodeRecursionDepth;
                IsParentLimbFaceRightMirrored = isParentLimbFaceRightMirrored;
                IsParentLimbFaceUpMirrored = isParentLimbFaceUpMirrored;
                IsParentLimbFaceForwardMirrored = isParentLimbFaceForwardMirrored;
                IsFaceRightMirrored = isFaceRightMirrored;
                IsFaceUpMirrored = isFaceUpMirrored;
                IsFaceForwardMirrored = isFaceForwardMirrored;

                // Pre-compute mirroring flags to avoid repeated XOR operations.
                ComputedMirrorRight = isParentLimbFaceRightMirrored ^ isFaceRightMirrored;
                ComputedMirrorUp = isParentLimbFaceUpMirrored ^ isFaceUpMirrored;
                ComputedMirrorForward = isParentLimbFaceForwardMirrored ^ isFaceForwardMirrored;
            }
        }

        public static List<Limb> CreateLimbs(
            SimsGenotype genotype, Dictionary<Limb, List<Limb>> parentToChildLimbsMap,
            Dictionary<Limb, Limb> childToParentLimbMap,
            Dictionary<ISignalReceiver, InputSetDefinition> receiverToInputDefinitionSetMap
        )
        {
            // Get pooled lookup structures to avoid allocations.
            Dictionary<ulong, Node> nodeMap = GetPooledNodeMap();
            Dictionary<ulong, List<Connection>> connectionMap = GetPooledConnectionMap();
            Dictionary<ulong, List<NeuronDefinition>> neuronDefinitionMap = GetPooledNeuronDefinitionMap();
            List<(bool, bool, bool)> reflectionVariantsWorkList = GetPooledReflectionList();
            List<Neuron> neuronCreationWorkList = GetPooledNeuronList();

            try
            {
                // Populate lookup structures
                PopulateNodeMap(genotype.Nodes, nodeMap);
                PopulateConnectionMap(genotype.Connections, connectionMap);
                PopulateNeuronDefinitionMap(genotype.NeuronDefinitions, neuronDefinitionMap);

                LimbCreationWorkspace workspace = new(
                    nodeMap, connectionMap, neuronDefinitionMap,
                    parentToChildLimbsMap, childToParentLimbMap, receiverToInputDefinitionSetMap,
                    reflectionVariantsWorkList, neuronCreationWorkList);

                // Use queue-based processing to maintain breadth-first order (same as original).
                Queue<LimbBuildTask> processingQueue = new(SimsPhenotype.MAX_LIMBS);

                // Start with root node.
                LimbBuildTask rootTask = new(
                    genotype.Nodes[0], null, new CreatedLimbData(null, Vector3.One, false),
                    0, false, false, false, false, false, false);
                processingQueue.Enqueue(rootTask);

                ProcessLimbBuildingTasks(processingQueue, workspace);

                return workspace.AllCreatedLimbs;
            }
            finally
            {
                // Return pooled resources - clean up any Lists inside the maps first.
                ReturnConnectionMapToPool(connectionMap);
                ReturnNeuronDefinitionMapToPool(neuronDefinitionMap);
                ReturnPooledNodeMap(nodeMap);

                // Also return workspace pooled resources.
                ReturnPooledReflectionList(reflectionVariantsWorkList);
                ReturnPooledNeuronList(neuronCreationWorkList);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PopulateNodeMap(IReadOnlyList<Node> nodes, Dictionary<ulong, Node> nodeMap)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                nodeMap[nodes[i].Gid] = nodes[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PopulateConnectionMap(IReadOnlyList<Connection> connections, Dictionary<ulong, List<Connection>> connectionMap)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                Connection connection = connections[i];
                if (!connectionMap.TryGetValue(connection.ParentNodeGid, out List<Connection> connectionList))
                {
                    connectionList = GetPooledConnectionList();
                    connectionMap[connection.ParentNodeGid] = connectionList;
                }
                connectionList.Add(connection);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PopulateNeuronDefinitionMap(IReadOnlyList<NeuronDefinition> neuronDefinitions, Dictionary<ulong, List<NeuronDefinition>> neuronDefinitionMap)
        {
            for (int i = 0; i < neuronDefinitions.Count; i++)
            {
                NeuronDefinition neuronDefinition = neuronDefinitions[i];
                if (!neuronDefinitionMap.TryGetValue(neuronDefinition.ContainerGid, out List<NeuronDefinition> neuronDefinitionList))
                {
                    neuronDefinitionList = GetPooledNeuronDefinitionList();
                    neuronDefinitionMap[neuronDefinition.ContainerGid] = neuronDefinitionList;
                }
                neuronDefinitionList.Add(neuronDefinition);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnConnectionMapToPool(Dictionary<ulong, List<Connection>> connectionMap)
        {
            foreach (List<Connection> list in connectionMap.Values)
            {
                ReturnPooledConnectionList(list);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnNeuronDefinitionMapToPool(Dictionary<ulong, List<NeuronDefinition>> neuronDefinitionMap)
        {
            foreach (List<NeuronDefinition> list in neuronDefinitionMap.Values)
            {
                ReturnPooledNeuronDefinitionList(list);
            }
        }

        private static void ProcessLimbBuildingTasks(Queue<LimbBuildTask> processingQueue, LimbCreationWorkspace workspace)
        {
            while (processingQueue.Count > 0)
            {
                LimbBuildTask currentTask = processingQueue.Dequeue();
                ProcessSingleLimbBuildTask(currentTask, processingQueue, workspace);
            }
        }

        private static void ProcessSingleLimbBuildTask(LimbBuildTask task, Queue<LimbBuildTask> processingQueue, LimbCreationWorkspace workspace)
        {
            workspace.NeuronDefinitionMap.TryGetValue(task.TargetNode.Gid, out List<NeuronDefinition> neuronDefinitions);

            CreatedLimbData newLimbData = CreateChildLimb(
                task.ParentLimbData, task.TargetNode, task.ConnectionToParent, neuronDefinitions,
                task.ComputedMirrorRight, task.ComputedMirrorUp, task.ComputedMirrorForward, task.NodeRecursionDepth,
                workspace.ReceiverToInputDefinitionSetMap, workspace.NeuronCreationWorkList);

            // Early exit if limb dimensions are out of bounds.
            if (IsLimbDimensionsOutOfBounds(newLimbData.Limb.Dimensions))
                return;

            // Early exit if limb would collide with existing limbs (excluding parent).
            if (DoesLimbCollideWithExisting(newLimbData.Limb, workspace.AllCreatedLimbs, task.ParentLimbData.Limb, workspace.CollisionCheckWorkList))
                return;

            // Add the new limb to tracking structures.
            AddLimbToWorkspace(newLimbData.Limb, task.TargetNode.Gid, task.ParentLimbData.Limb, workspace);

            // Process child connections if recursion limit not reached.
            ProcessChildConnections(task, newLimbData, processingQueue, workspace);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLimbDimensionsOutOfBounds(Vector3 dimensions)
        {
            return dimensions.X < SimsPhenotype.MIN_LIMB_DIMENSION ||
                   dimensions.Y < SimsPhenotype.MIN_LIMB_DIMENSION ||
                   dimensions.Z < SimsPhenotype.MIN_LIMB_DIMENSION ||
                   dimensions.X > SimsPhenotype.MAX_LIMB_DIMENSION ||
                   dimensions.Y > SimsPhenotype.MAX_LIMB_DIMENSION ||
                   dimensions.Z > SimsPhenotype.MAX_LIMB_DIMENSION;
        }

        private static bool DoesLimbCollideWithExisting(Limb newLimb, List<Limb> existingLimbs, Limb parentLimb, List<Limb> workCollisionList)
        {
            // Optimized collision detection - check directly without allocating intermediate list.
            for (int i = 0; i < existingLimbs.Count; i++)
            {
                Limb existingLimb = existingLimbs[i];
                if (existingLimb != parentLimb && CollisionDetector.ObbOverlap(newLimb, existingLimb))
                    return true;
            }
            return false;
        }

        private static void AddLimbToWorkspace(Limb newLimb, ulong nodeGid, Limb parentLimb, LimbCreationWorkspace workspace)
        {
            workspace.AllCreatedLimbs.Add(newLimb);
            workspace.LimbToNodeGidMap[newLimb] = nodeGid;

            if (parentLimb != null)
            {
                if (!workspace.ParentToChildLimbsMap.TryGetValue(parentLimb, out List<Limb> childLimbs))
                {
                    childLimbs = new List<Limb>();
                    workspace.ParentToChildLimbsMap[parentLimb] = childLimbs;
                }
                childLimbs.Add(newLimb);
                workspace.ChildToParentLimbMap[newLimb] = parentLimb;
            }
        }

        private static void ProcessChildConnections(LimbBuildTask parentTask, CreatedLimbData newLimbData, Queue<LimbBuildTask> processingQueue, LimbCreationWorkspace workspace)
        {
            bool nodeRecursionLimitReached = parentTask.NodeRecursionDepth == parentTask.TargetNode.RecursiveLimit;

            if (!workspace.ConnectionMap.TryGetValue(parentTask.TargetNode.Gid, out List<Connection> connections))
                return;

            // Pre-compute mirroring state for child tasks.
            bool inheritedMirrorRight = parentTask.ComputedMirrorRight;
            bool inheritedMirrorUp = parentTask.ComputedMirrorUp;
            bool inheritedMirrorForward = parentTask.ComputedMirrorForward;

            for (int connectionIndex = 0; connectionIndex < connections.Count; connectionIndex++)
            {
                Connection connection = connections[connectionIndex];

                if (connection.TerminalOnly && !nodeRecursionLimitReached)
                    continue;

                Node childNode = workspace.NodeMap[connection.ChildNodeGid];
                int newRecursionDepth = childNode.Gid == parentTask.TargetNode.Gid ? parentTask.NodeRecursionDepth + 1 : 0;

                // Skip if recursion limit exceeded.
                if (newRecursionDepth > childNode.RecursiveLimit)
                    continue;

                // Calculate reflection variants efficiently using pre-allocated list.
                workspace.ReflectionVariantsWorkList.Clear();
                CalculateReflectionVariants(connection, workspace.ReflectionVariantsWorkList);

                // Skip if adding these limbs would exceed max limb count.
                if (workspace.ReflectionVariantsWorkList.Count + workspace.AllCreatedLimbs.Count > SimsPhenotype.MAX_LIMBS)
                    continue;

                // Add tasks to queue for each variant (maintain breadth-first order).
                for (int variantIndex = 0; variantIndex < workspace.ReflectionVariantsWorkList.Count; variantIndex++)
                {
                    (bool reflectX, bool reflectY, bool reflectZ) = workspace.ReflectionVariantsWorkList[variantIndex];

                    LimbBuildTask childTask = new(
                        childNode, connection, newLimbData, newRecursionDepth,
                        inheritedMirrorRight, inheritedMirrorUp, inheritedMirrorForward,
                        reflectX, reflectY, reflectZ);

                    processingQueue.Enqueue(childTask);
                }
            }
        }

        private static void CalculateReflectionVariants(Connection connection, List<(bool reflectX, bool reflectY, bool reflectZ)> variantsList)
        {
            // Start with base variant (no reflections).
            variantsList.Add((false, false, false));

            // Apply each reflection type, doubling the variants each time.
            if (connection.ReflectionX)
            {
                int currentCount = variantsList.Count;
                for (int i = 0; i < currentCount; i++)
                {
                    (bool reflectX, bool reflectY, bool reflectZ) variant = variantsList[i];
                    variantsList.Add((!variant.reflectX, variant.reflectY, variant.reflectZ));
                }
            }

            if (connection.ReflectionY)
            {
                int currentCount = variantsList.Count;
                for (int i = 0; i < currentCount; i++)
                {
                    (bool reflectX, bool reflectY, bool reflectZ) variant = variantsList[i];
                    variantsList.Add((variant.reflectX, !variant.reflectY, variant.reflectZ));
                }
            }

            if (connection.ReflectionZ)
            {
                int currentCount = variantsList.Count;
                for (int i = 0; i < currentCount; i++)
                {
                    (bool reflectX, bool reflectY, bool reflectZ) variant = variantsList[i];
                    variantsList.Add((variant.reflectX, variant.reflectY, !variant.reflectZ));
                }
            }
        }

        private static CreatedLimbData CreateChildLimb(
            CreatedLimbData parentData, Node node, Connection? connection, List<NeuronDefinition> neuronDefinitions,
            bool mirrorRight, bool mirrorUp, bool mirrorForward, int nodeRecursionDepth,
            Dictionary<ISignalReceiver, InputSetDefinition> receiverToInputDefinitionSetGidMap,
            List<Neuron> neuronCreationWorkList)
        {
            // Compute new scale and dimensions.
            Vector3 currentScale = connection.HasValue
                ? Vector3.Multiply(parentData.CurrentScale, connection.Value.Scale)
                : parentData.CurrentScale;

            Vector3 dimensions = new(
                node.Dimensions.X * currentScale.X,
                node.Dimensions.Y * currentScale.Y,
                node.Dimensions.Z * currentScale.Z
            );

            // Create new limb with absolute dimensions and node GID.
            Limb newLimb = new(dimensions, node.Gid);
            newLimb.SetColor(node.Color.ToRGBA(nodeRecursionDepth));

            bool swapX = false;
            if (connection.HasValue)
            {
                // Compute joint placement and rotation.
                swapX = ProcessJointCreation(parentData, newLimb, connection.Value, node,
                    mirrorRight, mirrorUp, mirrorForward, dimensions, receiverToInputDefinitionSetGidMap);
            }

            // Create and assign neurons efficiently.
            ProcessNeuronCreation(newLimb, neuronDefinitions, receiverToInputDefinitionSetGidMap, neuronCreationWorkList);

            return new CreatedLimbData(newLimb, currentScale, swapX);
        }

        private static bool ProcessJointCreation(
            CreatedLimbData parentData, Limb newLimb, Connection connection, Node node,
            bool mirrorRight, bool mirrorUp, bool mirrorForward, Vector3 dimensions,
            Dictionary<ISignalReceiver, InputSetDefinition> receiverToInputDefinitionSetGidMap)
        {
            // Compute face axes and normal in parent's coordinate system.
            GetParentSpaceFaceAxes(connection.ParentFace, out Vector3 faceRight, out Vector3 faceUp, out Vector3 faceNormal);

            // Apply parent handedness swap if needed (use conditional moves to avoid branching).
            if (parentData.SwapX)
            {
                faceRight.X = -faceRight.X;
                faceUp.X = -faceUp.X;
                faceNormal.X = -faceNormal.X;
            }

            // Apply mirroring (use conditional negation).
            if (mirrorRight) faceRight = -faceRight;
            if (mirrorUp) faceUp = -faceUp;
            if (mirrorForward) faceNormal = -faceNormal;

            // Cache parent dimensions and half extents.
            Vector3 parentDimensions = parentData.Limb.Dimensions;
            float parentHalfX = parentDimensions.X * 0.5f;
            float parentHalfY = parentDimensions.Y * 0.5f;
            float parentHalfZ = parentDimensions.Z * 0.5f;

            // Compute anchor position in parent space (optimized vector math).
            Vector3 parentSpaceAnchor = new(
                (faceNormal.X * parentHalfX) + (faceRight.X * parentHalfX * connection.Position.X) + (faceUp.X * parentHalfX * connection.Position.Y),
                (faceNormal.Y * parentHalfY) + (faceRight.Y * parentHalfY * connection.Position.X) + (faceUp.Y * parentHalfY * connection.Position.Y),
                (faceNormal.Z * parentHalfZ) + (faceRight.Z * parentHalfZ * connection.Position.X) + (faceUp.Z * parentHalfZ * connection.Position.Y)
            );

            // Calculate joint orientation with simplified parity calculation.
            float parity = (mirrorRight ^ mirrorUp ^ mirrorForward ^ parentData.SwapX) ? -1f : 1f;
            Quaternion jointRotation = QuaternionHelper.AngleAxis(connection.Orientation.Z * parity, faceNormal);
            jointRotation *= QuaternionHelper.AngleAxis(connection.Orientation.Y * parity, faceUp);
            jointRotation *= QuaternionHelper.AngleAxis(connection.Orientation.X * parity, faceRight);

            Vector3 parentSpaceXAxis = Vector3.Transform(faceRight, jointRotation);
            Vector3 parentSpaceYAxis = Vector3.Transform(faceUp, jointRotation);
            Vector3 parentSpaceZAxis = Vector3.Transform(faceNormal, jointRotation);

            Quaternion parentSpaceRotation = Quaternion.Normalize(jointRotation * QuaternionHelper.LookRotation(faceNormal, faceUp));

            // Position child limb.
            float halfDepth = Math.Abs(dimensions.Z) * 0.5f;
            Vector3 parentSpaceChildPosition = parentSpaceAnchor + Vector3.Transform(new Vector3(0f, 0f, halfDepth), parentSpaceRotation);

            // Transform to world space.
            Vector3 worldChildPosition = parentData.Limb.Position + Vector3.Transform(parentSpaceChildPosition, parentData.Limb.Rotation);
            Quaternion worldChildRotation = parentData.Limb.Rotation * parentSpaceRotation;

            newLimb.SetPositionAndRotation(worldChildPosition, worldChildRotation);

            // Calculate cross-sectional area for joint strength (optimized calculations).
            float parentFaceWidth = 2f * Math.Abs(Vector3.Dot(faceRight, new Vector3(parentHalfX, parentHalfY, parentHalfZ)));
            float parentFaceHeight = 2f * Math.Abs(Vector3.Dot(faceUp, new Vector3(parentHalfX, parentHalfY, parentHalfZ)));
            float parentCrossSectionalArea = parentFaceWidth * parentFaceHeight;
            float childCrossSectionalArea = newLimb.Dimensions.X * newLimb.Dimensions.Y;
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

            // Wire up joint actuators.
            foreach (JointAngleActuator actuator in joint.Actuators)
            {
                InputSetDefinition inputs = GetInputsForActuatorAxis(actuator.Axis, node.JointDefinition);
                receiverToInputDefinitionSetGidMap[actuator] = inputs;
            }

            // Check for handedness swap.
            return Vector3.Dot(Vector3.Cross(faceRight, faceUp), faceNormal) > 0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static InputSetDefinition GetInputsForActuatorAxis(JointAxis axis, JointDefinition jointDefinition)
        {
            if (axis == JointAxis.Primary)
                return jointDefinition.PrimaryAxisInputs;
            else if (axis == JointAxis.Secondary)
                return jointDefinition.SecondaryAxisInputs;
            else if (axis == JointAxis.Tertiary)
                return jointDefinition.TertiaryAxisInputs;
            else
                throw new ArgumentException($"Invalid actuator axis: {axis}. Must be Primary, Secondary, or Tertiary.");
        }

        private static void ProcessNeuronCreation(
            Limb newLimb, List<NeuronDefinition> neuronDefinitions,
            Dictionary<ISignalReceiver, InputSetDefinition> receiverToInputDefinitionSetGidMap,
            List<Neuron> neuronCreationWorkList)
        {
            neuronCreationWorkList.Clear();

            if (neuronDefinitions != null)
            {
                for (int i = 0; i < neuronDefinitions.Count; i++)
                {
                    NeuronDefinition neuronDefinition = neuronDefinitions[i];
                    Neuron neuron = new(neuronDefinition.ActivationFunction);
                    neuronCreationWorkList.Add(neuron);
                    receiverToInputDefinitionSetGidMap[neuron] = neuronDefinition.Inputs;
                }
            }

            newLimb.SetNeurons(neuronCreationWorkList);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GetParentSpaceFaceAxes(int face, out Vector3 faceRight, out Vector3 faceUp, out Vector3 faceNormal)
        {
            // Use pre-computed lookup tables for better performance.
            if ((uint)face >= 6u)
                throw new ArgumentOutOfRangeException(nameof(face), $"Invalid face index {face}. Must be 0-5.");

            faceRight = FaceRightVectors[face];
            faceUp = FaceUpVectors[face];
            faceNormal = FaceNormalVectors[face];
        }
    }
}
