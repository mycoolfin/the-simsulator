using System;

namespace mycoolfin.TheSimsulator.Sims
{
    public static class SignalInputDefinitionMutator
    {
        private static readonly WeightedChoiceList<Action<SimsGenotypeCreationContext, int, int>> SignalInputDefinitionMutations = new()
        {
            { MutateSignalEmitterAddress, 1f },
            { MutateWeight, 1f }
        };

        public static void MutateSignalInputDefinition(SimsGenotypeCreationContext context, int neuronDefinitionIndex, int inputIndex)
        {
            SignalInputDefinitionMutations.Choose().Invoke(context, neuronDefinitionIndex, inputIndex);
        }

        public static void MutateSignalEmitterAddress(SimsGenotypeCreationContext context, int neuronDefinitionIndex, int inputIndex)
        {
            NeuronDefinition neuronDefinition = context.NeuronDefinitions[neuronDefinitionIndex];
            SignalInputDefinition input = GetInputByIndex(neuronDefinition, inputIndex);
            SignalEmitterAddress newSignalEmitterAddress = SignalEmitterAddress.CreateRandom(neuronDefinition.ContainerGid, context.Nodes, context.Connections, context.NeuronDefinitions);
            SignalInputDefinition newInput = new(newSignalEmitterAddress, input.Weight);
            NeuronDefinition newNeuronDefinition = SetInputByIndex(neuronDefinition, inputIndex, newInput);
            context.NeuronDefinitions[neuronDefinitionIndex] = newNeuronDefinition;
        }

        public static void MutateWeight(SimsGenotypeCreationContext context, int neuronDefinitionIndex, int inputIndex)
        {
            float sigma = 0.1f;
            NeuronDefinition neuronDefinition = context.NeuronDefinitions[neuronDefinitionIndex];
            SignalInputDefinition input = GetInputByIndex(neuronDefinition, inputIndex);
            float newWeight = Math.Clamp(input.Weight + SharedRandom.DrawGaussian(sigma), SignalInputDefinition.MinWeight, SignalInputDefinition.MaxWeight);
            SignalInputDefinition newInput = new(input.SignalEmitterAddress, newWeight);
            NeuronDefinition newNeuronDefinition = SetInputByIndex(neuronDefinition, inputIndex, newInput);
            context.NeuronDefinitions[neuronDefinitionIndex] = newNeuronDefinition;
        }

        private static SignalInputDefinition GetInputByIndex(NeuronDefinition neuronDefinition, int inputIndex)
        {
            return inputIndex switch
            {
                0 => neuronDefinition.InputA,
                1 => neuronDefinition.InputB,
                2 => neuronDefinition.InputC,
                _ => throw new ArgumentOutOfRangeException(nameof(inputIndex), "Input index must be 0, 1, or 2.")
            };
        }

        private static NeuronDefinition SetInputByIndex(NeuronDefinition neuronDefinition, int inputIndex, SignalInputDefinition newInput)
        {
            return inputIndex switch
            {
                0 => new NeuronDefinition(neuronDefinition.ContainerGid, neuronDefinition.ActivationFunction, newInput, neuronDefinition.InputB, neuronDefinition.InputC),
                1 => new NeuronDefinition(neuronDefinition.ContainerGid, neuronDefinition.ActivationFunction, neuronDefinition.InputA, newInput, neuronDefinition.InputC),
                2 => new NeuronDefinition(neuronDefinition.ContainerGid, neuronDefinition.ActivationFunction, neuronDefinition.InputA, neuronDefinition.InputB, newInput),
                _ => throw new ArgumentOutOfRangeException(nameof(inputIndex), "Input index must be 0, 1, or 2.")
            };
        }
    }
}
