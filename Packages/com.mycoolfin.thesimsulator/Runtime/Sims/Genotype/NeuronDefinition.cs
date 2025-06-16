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
        [FieldOffset(20)] public readonly InputSetDefinition Inputs;
        [FieldOffset(44)] public readonly double _pad0;
        [FieldOffset(52)] public readonly double _pad1;
        [FieldOffset(60)] public readonly int _pad2;

        public NeuronDefinition(ulong containerId, ActivationFunction activationFunction, InputSetDefinition inputs)
        {
            byte[] buffer = new byte[sizeof(ulong)];
            SharedRandom.NextBytes(buffer);
            Gid = BitConverter.ToUInt64(buffer, 0);
            ContainerGid = containerId;
            ActivationFunction = activationFunction;
            Inputs = inputs;
            _pad0 = 0.0;
            _pad1 = 0.0;
            _pad2 = 0;
        }

        public static NeuronDefinition CreateRandom(ulong containerId, IReadOnlyList<Node> nodes, IReadOnlyList<Connection> connections, IReadOnlyList<NeuronDefinition> neuronDefinitions)
        {
            ActivationFunction randomActivationFunction = (ActivationFunction)SharedRandom.Next(0, Enum.GetValues(typeof(ActivationFunction)).Length);

            InputSetDefinition randomInputs = InputSetDefinition.CreateRandom(containerId, nodes, connections, neuronDefinitions);

            return new NeuronDefinition(containerId, randomActivationFunction, randomInputs);
        }

        public static NeuronDefinition RandomiseSignalInputs(ulong containerId, NeuronDefinition neuronDefinition, IReadOnlyList<Node> nodes, IReadOnlyList<Connection> connections, IReadOnlyList<NeuronDefinition> neuronDefinitions)
        {
            InputSetDefinition randomInputs = InputSetDefinition.CreateRandom(containerId, nodes, connections, neuronDefinitions);

            return new NeuronDefinition(
                neuronDefinition.ContainerGid,
                neuronDefinition.ActivationFunction,
                randomInputs
            );
        }
    }
}
