using System;

namespace mycoolfin.TheSimsulator.Sims
{
    public static class InputSetDefinitionMutator
    {
        private static readonly WeightedChoiceList<Action<SimsGenotypeCreationContext, ulong, InputSetDefinition, Action<InputSetDefinition>>> InputSetDefinitionMutations = new()
        {
            { MutateInputA, 1f },
            { MutateInputB, 1f },
            { MutateInputC, 1f }
        };

        public static void MutateInputSetDefinition(SimsGenotypeCreationContext context, ulong containerId, InputSetDefinition oldInputSet, Action<InputSetDefinition> setNewInputSetCallback)
        {
            InputSetDefinitionMutations.Choose().Invoke(context, containerId, oldInputSet, setNewInputSetCallback);
        }

        public static void MutateInputA(SimsGenotypeCreationContext context, ulong containerId, InputSetDefinition oldInputSet, Action<InputSetDefinition> setNewInputSetCallback)
        {
            SignalInputDefinition newInputA = new();
            SignalInputDefinitionMutator.MutateSignalInputDefinition(context, containerId, oldInputSet.A, input => newInputA = input);
            InputSetDefinition newInputSet = new(newInputA, oldInputSet.B, oldInputSet.C);
            setNewInputSetCallback(newInputSet);
        }

        public static void MutateInputB(SimsGenotypeCreationContext context, ulong containerId, InputSetDefinition oldInputSet, Action<InputSetDefinition> setNewInputSetCallback)
        {
            SignalInputDefinition newInputB = new();
            SignalInputDefinitionMutator.MutateSignalInputDefinition(context, containerId, oldInputSet.B, input => newInputB = input);
            InputSetDefinition newInputSet = new(oldInputSet.A, newInputB, oldInputSet.C);
            setNewInputSetCallback(newInputSet);
        }

        public static void MutateInputC(SimsGenotypeCreationContext context, ulong containerId, InputSetDefinition oldInputSet, Action<InputSetDefinition> setNewInputSetCallback)
        {
            SignalInputDefinition newInputC = new();
            SignalInputDefinitionMutator.MutateSignalInputDefinition(context, containerId, oldInputSet.C, input => newInputC = input);
            InputSetDefinition newInputSet = new(oldInputSet.A, oldInputSet.B, newInputC);
            setNewInputSetCallback(newInputSet);
        }
    }
}
