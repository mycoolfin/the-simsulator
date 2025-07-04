using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct InputSetDefinition
    {
        public readonly SignalInputDefinition A;
        public readonly SignalInputDefinition B;
        public readonly SignalInputDefinition C;

        public InputSetDefinition(SignalInputDefinition a, SignalInputDefinition b, SignalInputDefinition c)
        {
            A = a;
            B = b;
            C = c;
        }

        public static InputSetDefinition CreateRandom(ulong containerId, IReadOnlyList<Node> nodes, IReadOnlyList<Connection> connections, IReadOnlyList<NeuronDefinition> neuronDefinitions)
        {
            Span<ulong> childGidsScratch = stackalloc ulong[32];
            Span<Node> nodeSpan = nodes.ToArray().AsSpan();
            Span<Connection> connectionSpan = connections.ToArray().AsSpan();
            Span<NeuronDefinition> neuronSpan = neuronDefinitions.ToArray().AsSpan();
            return new InputSetDefinition(
                SignalInputDefinition.CreateRandom(
                    SignalEmitterAddress.CreateRandom(containerId, nodeSpan, connectionSpan, neuronSpan, childGidsScratch)),
                SignalInputDefinition.CreateRandom(
                    SignalEmitterAddress.CreateRandom(containerId, nodeSpan, connectionSpan, neuronSpan, childGidsScratch)),
                SignalInputDefinition.CreateRandom(
                    SignalEmitterAddress.CreateRandom(containerId, nodeSpan, connectionSpan, neuronSpan, childGidsScratch))
            );
        }
    }
}
