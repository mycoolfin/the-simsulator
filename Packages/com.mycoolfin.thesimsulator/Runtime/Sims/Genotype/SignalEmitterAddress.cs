using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims
{
    public enum SignalPort : byte
    {
        ThisLimb,
        ParentLimb,
        ChildLimb,
        Brain
    }

    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public readonly struct SignalEmitterAddress
    {
        [FieldOffset(0)] public readonly SignalPort Port;
        [FieldOffset(1)] public readonly byte Slot;
        [FieldOffset(2)] public readonly byte ChildIndex; // Ignored unless Port == ChildLimb.
        [FieldOffset(3)] public readonly byte _pad;

        public SignalEmitterAddress(SignalPort port, byte slot, byte childIndex)
        {
            Port = port;
            Slot = slot;
            ChildIndex = childIndex;
            _pad = 0;
        }

        public static SignalEmitterAddress CreateRandom(ulong thisContainerId, IReadOnlyList<Node> nodes, IReadOnlyList<Connection> connections, IReadOnlyList<NeuronDefinition> neuronDefinitions)
        {
            SignalPort randomPort = (SignalPort)SharedRandom.Next(0, System.Enum.GetValues(typeof(SignalPort)).Length);

            int existingSlotsCount = 0;
            byte slot = 0b0;
            byte childIndex = 0b0;
            switch (randomPort)
            {
                case SignalPort.ThisLimb:
                    existingSlotsCount = GetExistingSlotsCount(thisContainerId, nodes, neuronDefinitions);
                    slot = existingSlotsCount > 0 ? (byte)SharedRandom.Next(0, existingSlotsCount) : (byte)SharedRandom.Next(0, 256);
                    childIndex = (byte)SharedRandom.Next(0, 256);
                    break;
                case SignalPort.ParentLimb:
                    ulong parentContainerId = connections
                        .Where(c => c.ChildNodeGid == thisContainerId)
                        .Select(c => c.ParentNodeGid)
                        .FirstOrDefault();
                    existingSlotsCount = GetExistingSlotsCount(parentContainerId, nodes, neuronDefinitions);
                    slot = existingSlotsCount > 0 ? (byte)SharedRandom.Next(0, existingSlotsCount) : (byte)SharedRandom.Next(0, 256);
                    childIndex = (byte)SharedRandom.Next(0, 256);
                    break;
                case SignalPort.ChildLimb:
                    List<ulong> childLimbs = connections
                        .Where(c => c.ParentNodeGid == thisContainerId)
                        .Select(c => c.ChildNodeGid)
                        .ToList();
                    if (childLimbs.Count > 0)
                    {
                        childIndex = (byte)SharedRandom.Next(0, childLimbs.Count);
                        existingSlotsCount = GetExistingSlotsCount(childLimbs[childIndex], nodes, neuronDefinitions);
                        slot = existingSlotsCount > 0 ? (byte)SharedRandom.Next(0, existingSlotsCount) : (byte)SharedRandom.Next(0, 256);
                        break;
                    }
                    slot = (byte)SharedRandom.Next(0, 256);
                    childIndex = (byte)SharedRandom.Next(0, 256);
                    break;
                case SignalPort.Brain:
                    existingSlotsCount = GetExistingSlotsCount(SimsGenotype.BRAIN_GID, nodes, neuronDefinitions);
                    slot = existingSlotsCount > 0 ? (byte)SharedRandom.Next(0, existingSlotsCount) : (byte)SharedRandom.Next(0, 256);
                    childIndex = (byte)SharedRandom.Next(0, 256);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(randomPort), "Invalid SignalPort value.");
            }

            return new SignalEmitterAddress(randomPort, slot, childIndex);
        }

        private static ulong GetRandomContainerId(IReadOnlyList<Node> nodes, IReadOnlyList<NeuronDefinition> neuronDefinitions)
        {
            Dictionary<ulong, int> containerCounts = neuronDefinitions
                .GroupBy(n => n.ContainerGid)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (ulong id in nodes.Select(n => n.Gid).Append(SimsGenotype.BRAIN_GID))
                containerCounts.TryAdd(id, 0);

            ulong[] eligible = containerCounts
                .Where(kvp => kvp.Value < SimsGenotype.MaxNeuronDefinitionCount)
                .Select(kvp => kvp.Key)
                .ToArray();

            return eligible[SharedRandom.Next(eligible.Length)];
        }

        private static int GetExistingSlotsCount(ulong sourceContainerId, IReadOnlyList<Node> nodes, IReadOnlyList<NeuronDefinition> neuronDefinitions)
        {
            if (sourceContainerId == SimsGenotype.BRAIN_GID)
                return neuronDefinitions.Count(n => n.ContainerGid == SimsGenotype.BRAIN_GID);

            Node? hit = nodes
                .Where(n => n.Gid == sourceContainerId)
                .Select(n => (Node?)n)
                .FirstOrDefault();

            if (hit is not null)
            {
                int neuronCount = neuronDefinitions.Count(n => n.ContainerGid == sourceContainerId);
                return Node.SENSOR_COUNT + neuronCount; // TODO: BIAS handling.
            }

            throw new System.ArgumentOutOfRangeException(nameof(sourceContainerId), "Source container ID did not match any existing container.");

        }
    }
}
