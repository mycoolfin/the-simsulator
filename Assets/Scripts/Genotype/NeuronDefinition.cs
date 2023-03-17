using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NeuronDefinition : IDefinition
{
    [SerializeField] private NeuronType type;
    [SerializeField] private List<InputDefinition> inputDefinitions;

    public NeuronType Type => type;
    public ReadOnlyCollection<InputDefinition> InputDefinitions => inputDefinitions.AsReadOnly();

    public NeuronDefinition(NeuronType type, IList<InputDefinition> inputDefinitions)
    {
        ValidateNeuronType(type);
        ValidateInputDefinitions(inputDefinitions);

        this.type = type;
        this.inputDefinitions = inputDefinitions.ToList();
    }

    private static void ValidateNeuronType(NeuronType type)
    {
        bool validNeuronType = Enum.IsDefined(typeof(NeuronType), type);
        if (!validNeuronType)
            throw new ArgumentException("Unknown neuron type. Specified: " + type);
    }

    private static void ValidateInputDefinitions(IList<InputDefinition> inputDefinitions)
    {
        bool validInputDefinitionsCount = inputDefinitions.Count == 3;
        if (!validInputDefinitionsCount)
            throw new ArgumentException("Expected 3 input definitions. Specified: " + inputDefinitions.Count);

        foreach (InputDefinition inputDefinition in inputDefinitions)
            inputDefinition.Validate();
    }

    public void Validate()
    {
        ValidateNeuronType(type);
        ValidateInputDefinitions(inputDefinitions);
    }

    public static NeuronDefinition CreateRandom()
    {
        Array neuronTypes = Enum.GetValues(typeof(NeuronType));
        NeuronType type = (NeuronType)neuronTypes.GetValue(UnityEngine.Random.Range(0, neuronTypes.Length));

        return new NeuronDefinition(
            type,
            new List<InputDefinition> {
                InputDefinition.CreateRandom(),
                InputDefinition.CreateRandom(),
                InputDefinition.CreateRandom()
            }
        );
    }
}
