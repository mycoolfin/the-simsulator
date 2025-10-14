using System;

namespace mycoolfin.TheSimsulator.Sims.Genotype
{
    public static class NeuronDefinitionMutator
    {
        private static readonly WeightedChoiceList<Action<SimsGenotypeCreationContext, int>> NeuronDefinitionMutations = new()
        {
            { MutateActivationFunction, 1f },
            { MutateInputs, 1f }
        };

        public static void MutateNeuronDefinition(SimsGenotypeCreationContext context, int neuronDefinitionIndex)
        {
            NeuronDefinitionMutations.Choose().Invoke(context, neuronDefinitionIndex);
        }

        public static void MutateActivationFunction(SimsGenotypeCreationContext context, int neuronDefinitionIndex)
        {
            NeuronDefinition neuronDefinition = context.NeuronDefinitions[neuronDefinitionIndex];
            ActivationFunction newActivationFunction = (ActivationFunction)SharedRandom.Next(Enum.GetValues(typeof(ActivationFunction)).Length);
            NeuronDefinition newNeuronDefinition = new(neuronDefinition.ContainerGid, newActivationFunction, neuronDefinition.Inputs);
            context.NeuronDefinitions[neuronDefinitionIndex] = newNeuronDefinition;
        }

        public static void MutateInputs(SimsGenotypeCreationContext context, int neuronDefinitionIndex)
        {
            NeuronDefinition neuronDefinition = context.NeuronDefinitions[neuronDefinitionIndex];
            InputSetDefinition newInputs = new();
            InputSetDefinitionMutator.MutateInputSetDefinition(context, neuronDefinition.ContainerGid, neuronDefinition.Inputs, i => newInputs = i);
            NeuronDefinition newNeuronDefinition = new(neuronDefinition.ContainerGid, neuronDefinition.ActivationFunction, newInputs);
            context.NeuronDefinitions[neuronDefinitionIndex] = newNeuronDefinition;
        }
    }
}
