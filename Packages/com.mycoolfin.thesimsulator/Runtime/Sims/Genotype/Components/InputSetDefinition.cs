using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims
{
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public readonly struct InputSetDefinition
    {
        [FieldOffset(0)] public readonly SignalInputDefinition A;
        [FieldOffset(8)] public readonly SignalInputDefinition B;
        [FieldOffset(16)] public readonly SignalInputDefinition C;

        public InputSetDefinition(SignalInputDefinition a, SignalInputDefinition b, SignalInputDefinition c)
        {
            A = a;
            B = b;
            C = c;
        }

        public static InputSetDefinition CreateRandom(ulong containerId, IReadOnlyList<Node> nodes, IReadOnlyList<Connection> connections, IReadOnlyList<NeuronDefinition> neuronDefinitions)
        {
            SignalInputDefinition randomA = SignalInputDefinition.CreateRandom(SignalEmitterAddress.CreateRandom(containerId, nodes, connections, neuronDefinitions));
            SignalInputDefinition randomB = SignalInputDefinition.CreateRandom(SignalEmitterAddress.CreateRandom(containerId, nodes, connections, neuronDefinitions));
            SignalInputDefinition randomC = SignalInputDefinition.CreateRandom(SignalEmitterAddress.CreateRandom(containerId, nodes, connections, neuronDefinitions));

            return new InputSetDefinition(randomA, randomB, randomC);
        }
    }
}
