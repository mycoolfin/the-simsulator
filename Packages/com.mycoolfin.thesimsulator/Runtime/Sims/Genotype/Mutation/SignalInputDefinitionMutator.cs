using System;

namespace mycoolfin.TheSimsulator.Sims
{
    public static class SignalInputDefinitionMutator
    {
        private static readonly WeightedChoiceList<Action<SimsGenotypeCreationContext, ulong, SignalInputDefinition, Action<SignalInputDefinition>>> SignalInputDefinitionMutations = new()
        {
            { MutateSignalEmitterAddress, 1f },
            { MutateWeight, 1f }
        };

        public static void MutateSignalInputDefinition(SimsGenotypeCreationContext context, ulong containerId, SignalInputDefinition oldInput, Action<SignalInputDefinition> setNewInputCallback)
        {
            SignalInputDefinitionMutations.Choose().Invoke(context, containerId, oldInput, setNewInputCallback);
        }

        public static void MutateSignalEmitterAddress(SimsGenotypeCreationContext context, ulong containerId, SignalInputDefinition oldInput, Action<SignalInputDefinition> setNewInputCallback)
        {
            SignalEmitterAddress newSignalEmitterAddress = SignalEmitterAddress.CreateRandom(containerId, context.Nodes, context.Connections, context.NeuronDefinitions);
            SignalInputDefinition newInput = new(newSignalEmitterAddress, oldInput.Weight);
            setNewInputCallback(newInput);
        }

        public static void MutateWeight(SimsGenotypeCreationContext context, ulong containerId, SignalInputDefinition oldInput, Action<SignalInputDefinition> setNewInputCallback)
        {
            float sigma = 0.1f;
            float newWeight = Math.Clamp(oldInput.Weight + SharedRandom.DrawGaussian(sigma), SignalInputDefinition.MinWeight, SignalInputDefinition.MaxWeight);
            SignalInputDefinition newInput = new(oldInput.SignalEmitterAddress, newWeight);
            setNewInputCallback(newInput);
        }
    }
}
