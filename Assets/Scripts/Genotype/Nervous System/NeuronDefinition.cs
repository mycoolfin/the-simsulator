using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NeuronDefinition
{
    [SerializeField] private NeuronType type;
    [SerializeField] private List<InputDefinition> inputDefinitions;

    public NeuronType Type => type;
    public ReadOnlyCollection<InputDefinition> InputDefinitions => inputDefinitions.AsReadOnly();

    public NeuronDefinition(NeuronType type, IList<InputDefinition> inputDefinitions)
    {
        this.type = type;
        this.inputDefinitions = inputDefinitions.ToList();
    }

    public void Validate(EmitterAvailabilityMap emitterAvailabilityMap)
    {
        bool validNeuronType = Enum.IsDefined(typeof(NeuronType), type);
        if (!validNeuronType)
            throw new ArgumentException("Unknown neuron type. Specified: " + type);

        bool validInputDefinitionsCount = inputDefinitions.Count == 3;
        if (!validInputDefinitionsCount)
            throw new ArgumentException("Expected 3 input definitions. Specified: " + inputDefinitions.Count);

        foreach (InputDefinition inputDefinition in inputDefinitions)
            inputDefinition.Validate(emitterAvailabilityMap);
    }

    public static NeuronDefinition CreateRandom(EmitterAvailabilityMap emitterAvailabilityMap)
    {
        Array neuronTypes = Enum.GetValues(typeof(NeuronType));
        NeuronType type = (NeuronType)neuronTypes.GetValue(UnityEngine.Random.Range(0, neuronTypes.Length));

        return new(
            type,
            new List<InputDefinition> {
                InputDefinition.CreateRandom(emitterAvailabilityMap),
                InputDefinition.CreateRandom(emitterAvailabilityMap),
                InputDefinition.CreateRandom(emitterAvailabilityMap)
            }
        );
    }
}
