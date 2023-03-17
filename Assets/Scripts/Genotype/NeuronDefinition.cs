using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

[Serializable]
public struct NeuronDefinition
{
    public readonly NeuronType type;
    public readonly ReadOnlyCollection<SignalReceiverInputDefinition> inputDefinitions;

    public NeuronDefinition(NeuronType type, ReadOnlyCollection<SignalReceiverInputDefinition> inputDefinitions)
    {
        bool validNeuronType = Enum.IsDefined(typeof(NeuronType), type);
        if (!validNeuronType)
            throw new ArgumentException("Unknown neuron type. Specified: " + type);

        bool validInputDefinitions = inputDefinitions.Count == 3;
        if (!validInputDefinitions)
            throw new ArgumentException("Expected 3 input definitions. Specified: " + inputDefinitions.Count);

        this.type = type;
        this.inputDefinitions = inputDefinitions;
    }

    public static NeuronDefinition CreateRandom()
    {
        Array neuronTypes = Enum.GetValues(typeof(NeuronType));
        NeuronType type = (NeuronType)neuronTypes.GetValue(UnityEngine.Random.Range(0, neuronTypes.Length));

        return new NeuronDefinition(
            type,
            new List<SignalReceiverInputDefinition> {
                SignalReceiverInputDefinition.CreateRandom(),
                SignalReceiverInputDefinition.CreateRandom(),
                SignalReceiverInputDefinition.CreateRandom()
            }.AsReadOnly()
        );
    }
}
