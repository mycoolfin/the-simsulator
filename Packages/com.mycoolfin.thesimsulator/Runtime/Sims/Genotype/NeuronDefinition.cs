using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims
{
    public enum ActivationFunction : int
    {
        Abs,
        Atan,
        Cos,
        Differentiate,
        Divide,
        Expt,
        GreaterThan,
        If,
        Integrate,
        Interpolate,
        Log,
        Max,
        Memory,
        Min,
        OscillateSaw,
        OscillateWave,
        Product,
        Sigmoid,
        SignOf,
        Sin,
        Smooth,
        Sum,
        SumThreshold
    }

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public readonly struct NeuronDefinition
    {
        [FieldOffset(0)] public readonly ulong Gid;
        [FieldOffset(8)] public readonly ulong ContainerGid;
        [FieldOffset(16)] public readonly ActivationFunction ActivationFunction;
        [FieldOffset(20)] public readonly SignalInputDefinition InputA;
        [FieldOffset(28)] public readonly SignalInputDefinition InputB;
        [FieldOffset(36)] public readonly SignalInputDefinition InputC;
        [FieldOffset(44)] public readonly double _pad0;
        [FieldOffset(52)] public readonly double _pad1;
        [FieldOffset(60)] public readonly int _pad2;

        public NeuronDefinition(ulong containerId, ActivationFunction activationFunction, SignalInputDefinition inputA, SignalInputDefinition inputB, SignalInputDefinition inputC)
        {
            byte[] buffer = new byte[sizeof(ulong)];
            SharedRandom.NextBytes(buffer);
            Gid = BitConverter.ToUInt64(buffer, 0);
            ContainerGid = containerId;
            ActivationFunction = activationFunction;
            InputA = inputA;
            InputB = inputB;
            InputC = inputC;
            _pad0 = 0.0;
            _pad1 = 0.0;
            _pad2 = 0;
        }

        public static NeuronDefinition CreateRandom(IReadOnlyList<Node> nodes, IReadOnlyList<Connection> connections, IReadOnlyList<NeuronDefinition> neuronDefinitions)
        {
            ulong randomContainerId = GetRandomContainerId(nodes, neuronDefinitions);

            ActivationFunction randomActivationFunction = (ActivationFunction)SharedRandom.Next(0, System.Enum.GetValues(typeof(ActivationFunction)).Length);

            SignalInputDefinition randomInputA = SignalInputDefinition.CreateRandom(SignalEmitterAddress.CreateRandom(randomContainerId, nodes, connections, neuronDefinitions));
            SignalInputDefinition randomInputB = SignalInputDefinition.CreateRandom(SignalEmitterAddress.CreateRandom(randomContainerId, nodes, connections, neuronDefinitions));
            SignalInputDefinition randomInputC = SignalInputDefinition.CreateRandom(SignalEmitterAddress.CreateRandom(randomContainerId, nodes, connections, neuronDefinitions));

            return new NeuronDefinition(randomContainerId, randomActivationFunction, randomInputA, randomInputB, randomInputC);
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
    }
}
