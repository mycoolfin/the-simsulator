using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public enum NeuronType
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

public static class NeuronTypeExtensions
{
    public static int NumberOfInputs(this NeuronType self)
    {
        return new Dictionary<NeuronType, int>
        {
            { NeuronType.Abs, 1 },
            { NeuronType.Atan, 1 },
            { NeuronType.Cos, 1 },
            { NeuronType.Differentiate, 2 },
            { NeuronType.Divide, 2 },
            { NeuronType.Expt, 1 },
            { NeuronType.GreaterThan, 2 },
            { NeuronType.If, 3 },
            { NeuronType.Integrate, 2 },
            { NeuronType.Interpolate, 3 },
            { NeuronType.Log, 1 },
            { NeuronType.Max, 3 },
            { NeuronType.Memory, 2 },
            { NeuronType.Min, 3 },
            { NeuronType.OscillateSaw, 3 },
            { NeuronType.OscillateWave, 3 },
            { NeuronType.Product, 2 },
            { NeuronType.Sigmoid, 1 },
            { NeuronType.SignOf, 1 },
            { NeuronType.Sin, 1 },
            { NeuronType.Smooth, 2 },
            { NeuronType.Sum, 2 },
            { NeuronType.SumThreshold, 3 }
        }[self];
    }
}

[System.Serializable]
public struct NeuronDefinition
{
    public readonly NeuronType type;
    public readonly List<float> inputPreferences;
    public readonly List<float> inputWeights;

    public NeuronDefinition(NeuronType type, List<float> inputPreferences, List<float> inputWeights)
    {
        if (inputPreferences.Count != inputWeights.Count)
            throw new System.ArgumentException("Input parameters are improperly specified");

        this.type = type;
        this.inputPreferences = inputPreferences.Select(x => Mathf.Clamp(x, 0f, 1f)).ToList();
        this.inputWeights = inputWeights.Select(x => Mathf.Clamp(x, NeuronDefinitionParameters.MinWeight, NeuronDefinitionParameters.MaxWeight)).ToList();
    }

    public NeuronDefinition CreateCopy()
    {
        return new NeuronDefinition(
            type,
            inputPreferences,
            inputWeights
        );
    }

    public static NeuronDefinition CreateRandom()
    {
        Array neuronTypes = Enum.GetValues(typeof(NeuronType));
        NeuronType type = (NeuronType)neuronTypes.GetValue(UnityEngine.Random.Range(0, neuronTypes.Length));

        int numberOfInputs = type.NumberOfInputs();

        List<float> inputPreferences = new List<float>();
        List<float> inputWeights = new List<float>();
        for (int i = 0; i < numberOfInputs; i++)
        {
            inputPreferences.Add(UnityEngine.Random.Range(0f, 1f));
            inputWeights.Add(UnityEngine.Random.Range(NeuronDefinitionGenerationParameters.MinWeight, NeuronDefinitionGenerationParameters.MaxWeight));
        }
        return new NeuronDefinition(type, inputPreferences, inputWeights);
    }
}
