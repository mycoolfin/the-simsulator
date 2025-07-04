using System;
using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims
{
    public enum SignalPort : byte
    {
        Bias, ThisLimb, ParentLimb, ChildLimb, Brain
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct SignalEmitterAddress
    {
        public readonly SignalPort Port;
        public readonly byte Slot;
        public readonly byte ChildIndex; // Ignored unless Port == ChildLimb

        public SignalEmitterAddress(SignalPort port, byte slot, byte childIndex = 0)
        {
            Port = port;
            Slot = slot;
            ChildIndex = childIndex;
        }

        public static SignalEmitterAddress CreateRandom(
            ulong containerGid,
            ReadOnlySpan<Node> nodes,
            ReadOnlySpan<Connection> connections,
            ReadOnlySpan<NeuronDefinition> neuronDefinitions,
            Span<ulong> childGidsScratch // Caller passes, e.g. stackalloc ulong[32].
        )
        {
            XorShift32 rng = new((uint)containerGid);

            byte thisLimbSlots = CountEmitters(containerGid, nodes, neuronDefinitions);

            ulong parentGid = 0;
            byte parentLimbSlots = 0;
            for (int i = 0; i < connections.Length; i++)
                if (connections[i].ChildNodeGid == containerGid)
                {
                    parentGid = connections[i].ParentNodeGid;
                    parentLimbSlots = CountEmitters(parentGid, nodes, neuronDefinitions);
                    break;
                }

            int childCount = 0;
            for (int i = 0; i < connections.Length; i++)
                if (connections[i].ParentNodeGid == containerGid)
                    childGidsScratch[childCount++] = connections[i].ChildNodeGid;
            bool anyChildLimbSlots = false;
            for (int i = 0; i < childCount && !anyChildLimbSlots; i++)
                anyChildLimbSlots = CountEmitters(childGidsScratch[i], nodes, neuronDefinitions) > 0;

            byte brainSlots = CountEmitters(SimsGenotype.BRAIN_GID, nodes, neuronDefinitions);

            Span<SignalPort> validPorts = stackalloc SignalPort[5];
            int p = 0; validPorts[p++] = SignalPort.Bias;
            if (thisLimbSlots > 0) validPorts[p++] = SignalPort.ThisLimb;
            if (parentLimbSlots > 0) validPorts[p++] = SignalPort.ParentLimb;
            if (anyChildLimbSlots) validPorts[p++] = SignalPort.ChildLimb;
            if (brainSlots > 0) validPorts[p++] = SignalPort.Brain;

            SignalPort port = validPorts[rng.NextInt(p)];
            switch (port)
            {
                case SignalPort.ThisLimb:
                    return new(port, (byte)rng.NextInt(thisLimbSlots));
                case SignalPort.ParentLimb:
                    return new(port, (byte)rng.NextInt(parentLimbSlots));
                case SignalPort.ChildLimb:
                    int childIndex = rng.NextInt(childCount);
                    byte childSlots = CountEmitters(childGidsScratch[childIndex], nodes, neuronDefinitions);
                    return new(port, (byte)rng.NextInt(childSlots), (byte)childIndex);
                case SignalPort.Brain:
                    return new(port, (byte)rng.NextInt(brainSlots));
                default: // Bias.
                    return new(port, 0);
            }
        }

        static byte CountEmitters(ulong container, ReadOnlySpan<Node> nodes, ReadOnlySpan<NeuronDefinition> neuronDefinitions)
        {
            int count = 0;
            if (container == SimsGenotype.BRAIN_GID)
            {
                for (int i = 0; i < neuronDefinitions.Length; i++)
                    if (neuronDefinitions[i].ContainerGid == container) ++count;
            }
            else
            {
                for (int i = 0; i < nodes.Length; i++)
                    if (nodes[i].Gid == container) { count += Node.SENSOR_COUNT; break; }

                for (int i = 0; i < neuronDefinitions.Length; i++)
                    if (neuronDefinitions[i].ContainerGid == container) ++count;
            }
            return (byte)(count > byte.MaxValue ? byte.MaxValue : count);
        }
    }

    public struct XorShift32
    {
        uint state;
        public XorShift32(uint seed) { state = seed == 0 ? 0xdeadbeefu : seed; }

        uint NextUInt()
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return state = x;
        }
        public int NextInt(int maxExclusive) => (int)(NextUInt() % (uint)maxExclusive);
    }
}
