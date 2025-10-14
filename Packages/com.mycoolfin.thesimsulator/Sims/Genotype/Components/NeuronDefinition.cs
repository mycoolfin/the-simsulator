using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims.Genotype
{
    public enum ActivationFunction : byte
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NeuronDefinition
    {
        public ulong Gid { get; set; }
        public ulong ContainerGid { get; set; }
        public ActivationFunction ActivationFunction { get; set; }
        public InputSetDefinition Inputs { get; set; }

        private const int ActivationFunctionCount = (int)ActivationFunction.SumThreshold + 1;

        public NeuronDefinition(ulong containerId, ActivationFunction activationFunction, InputSetDefinition inputs)
        {
            Gid = SharedRandom.NextUInt64();
            ContainerGid = containerId;
            ActivationFunction = activationFunction;
            Inputs = inputs;
        }

        public static NeuronDefinition CreateRandom(ulong containerId, IReadOnlyList<Node> nodes, IReadOnlyList<Connection> connections, IReadOnlyList<NeuronDefinition> neuronDefinitions)
        {
            ActivationFunction randomActivationFunction = (ActivationFunction)SharedRandom.Next(0, ActivationFunctionCount);

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
