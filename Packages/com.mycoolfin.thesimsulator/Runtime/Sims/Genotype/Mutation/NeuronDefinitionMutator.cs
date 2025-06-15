using System;

namespace mycoolfin.TheSimsulator.Sims
{
    public static class NeuronDefinitionMutator
    {
        private static readonly WeightedChoiceList<Action<SimsGenotypeCreationContext, int>> NeuronDefinitionMutations = new()
        {
            { MutateActivationFunction, 1f },
            { MutateInputA, 1f },
            { MutateInputB, 1f },
            { MutateInputC, 1f }
        };

        public static void MutateNeuronDefinition(SimsGenotypeCreationContext context, int neuronDefinitionIndex)
        {
            NeuronDefinitionMutations.Choose().Invoke(context, neuronDefinitionIndex);
        }

        public static void MutateActivationFunction(SimsGenotypeCreationContext context, int neuronDefinitionIndex)
        {
            NeuronDefinition neuronDefinition = context.NeuronDefinitions[neuronDefinitionIndex];
            ActivationFunction newActivationFunction = (ActivationFunction)SharedRandom.Next(Enum.GetValues(typeof(ActivationFunction)).Length);
            NeuronDefinition newNeuronDefinition = new(neuronDefinition.ContainerGid, newActivationFunction, neuronDefinition.InputA, neuronDefinition.InputB, neuronDefinition.InputC);
            context.NeuronDefinitions[neuronDefinitionIndex] = newNeuronDefinition;
        }

        public static void MutateInputA(SimsGenotypeCreationContext context, int neuronDefinitionIndex)
        {
            SignalInputDefinitionMutator.MutateSignalInputDefinition(context, neuronDefinitionIndex, 0);
        }

        public static void MutateInputB(SimsGenotypeCreationContext context, int neuronDefinitionIndex)
        {
            SignalInputDefinitionMutator.MutateSignalInputDefinition(context, neuronDefinitionIndex, 1);
        }

        public static void MutateInputC(SimsGenotypeCreationContext context, int neuronDefinitionIndex)
        {
            SignalInputDefinitionMutator.MutateSignalInputDefinition(context, neuronDefinitionIndex, 2);
        }
    }
}
