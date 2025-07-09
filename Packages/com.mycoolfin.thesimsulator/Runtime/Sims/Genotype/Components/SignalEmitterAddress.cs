using System;
using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims.Genotype
{
    public enum RelativeSignalPort : byte
    {
        Bias, ThisLimb, ParentLimb, ChildLimb, AnyLimb, Brain
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct SignalEmitterAddress
    {
        public readonly RelativeSignalPort Port;
        public readonly byte Slot;
        public readonly byte LimbInstance;

        public SignalEmitterAddress(RelativeSignalPort port, byte slot, byte limbInstance = default)
        {
            Port = port;
            Slot = slot;
            LimbInstance = limbInstance;
        }

        public static SignalEmitterAddress CreateRandom(
            ulong containerGid,
            ReadOnlySpan<Node> nodes,
            ReadOnlySpan<Connection> connections,
            ReadOnlySpan<NeuronDefinition> neuronDefinitions
        )
        {
            XorShift32 rng = new((uint)containerGid);

            bool inBrain = containerGid == SimsGenotype.BRAIN_GID;
            ulong thisLimbNodeGid = containerGid;
            byte thisLimbSlots = GetEmitterCount(thisLimbNodeGid, nodes, neuronDefinitions);
            ulong parentLimbNodeGid = 0;
            byte parentLimbSlots = 0;
            bool anyValidChildren = false;
            byte brainSlots = GetEmitterCount(SimsGenotype.BRAIN_GID, nodes, neuronDefinitions);
            for (int i = 0; i < connections.Length; i++)
            {
                Connection c = connections[i];
                if (c.ChildNodeGid == containerGid) // Container is child.
                {
                    parentLimbNodeGid = c.ParentNodeGid;
                    parentLimbSlots = GetEmitterCount(parentLimbNodeGid, nodes, neuronDefinitions);
                }
                else if (c.ParentNodeGid == containerGid) // Container is parent.
                {
                    int childSlots = GetEmitterCount(c.ChildNodeGid, nodes, neuronDefinitions);
                    if (childSlots > 0)
                        anyValidChildren = true;
                }
            }

            Span<RelativeSignalPort> validPorts = stackalloc RelativeSignalPort[5];
            int p = 0; validPorts[p++] = RelativeSignalPort.Bias;
            if (inBrain)
            {
                validPorts[p++] = RelativeSignalPort.AnyLimb;
            }
            else
            {
                if (thisLimbSlots > 0) validPorts[p++] = RelativeSignalPort.ThisLimb;
                if (parentLimbSlots > 0) validPorts[p++] = RelativeSignalPort.ParentLimb;
                if (anyValidChildren) validPorts[p++] = RelativeSignalPort.ChildLimb;
            }
            if (brainSlots > 0) validPorts[p++] = RelativeSignalPort.Brain;

            RelativeSignalPort port = validPorts[rng.NextInt(p)];
            return port switch
            {
                RelativeSignalPort.ThisLimb => new(port, (byte)rng.NextInt(thisLimbSlots)),
                RelativeSignalPort.ParentLimb => new(port, (byte)rng.NextInt(parentLimbSlots)),
                RelativeSignalPort.ChildLimb => new(port, (byte)rng.NextInt(256), (byte)rng.NextInt(256)),
                RelativeSignalPort.AnyLimb => new(port, (byte)rng.NextInt(256), (byte)rng.NextInt(256)),
                RelativeSignalPort.Brain => new(port, (byte)rng.NextInt(brainSlots)),
                _ => new(port, 0), // Bias.
            };
        }

        static byte GetEmitterCount(ulong containerGid, ReadOnlySpan<Node> nodes, ReadOnlySpan<NeuronDefinition> neuronDefinitions)
        {
            byte count = 0;
            if (containerGid != SimsGenotype.BRAIN_GID)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    if (nodes[i].Gid == containerGid)
                    {
                        count += (byte)nodes[i].SensorCount;
                        break;
                    }
                }
            }
            for (int i = 0; i < neuronDefinitions.Length; i++)
                if (neuronDefinitions[i].ContainerGid == containerGid)
                    count++;
            return count;
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
