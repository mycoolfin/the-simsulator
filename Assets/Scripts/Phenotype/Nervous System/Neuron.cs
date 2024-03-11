using System;
using UnityEngine;

public class Neuron
{
    private readonly NeuronType type;
    public NeuronType Type => type;
    private readonly SignalProcessor processor;
    public SignalProcessor Processor => processor;
    private float storedValue;

    public Neuron(NeuronDefinition neuronDefinition)
    {
        type = neuronDefinition.Type;
        Func<float, float, float, float> evaluationFunction = type switch
        {
            NeuronType.Abs => Abs,
            NeuronType.Atan => Atan,
            NeuronType.Cos => Cos,
            NeuronType.Differentiate => Differentiate,
            NeuronType.Divide => Divide,
            NeuronType.Expt => Expt,
            NeuronType.GreaterThan => GreaterThan,
            NeuronType.If => If,
            NeuronType.Integrate => Integrate,
            NeuronType.Interpolate => Interpolate,
            NeuronType.Log => Log,
            NeuronType.Max => Max,
            NeuronType.Memory => Memory,
            NeuronType.Min => Min,
            NeuronType.OscillateSaw => OscillateSaw,
            NeuronType.OscillateWave => OscillateWave,
            NeuronType.Product => Product,
            NeuronType.Sigmoid => Sigmoid,
            NeuronType.SignOf => SignOf,
            NeuronType.Sin => Sin,
            NeuronType.Smooth => Smooth,
            NeuronType.Sum => Sum,
            NeuronType.SumThreshold => SumThreshold,
            _ => throw new System.ArgumentException("Unknown neuron type '" + type + "'")
        };
        processor = new(neuronDefinition.InputDefinitions, evaluationFunction);
    }

    private float Abs(float input1, float input2, float input3)
    {
        return MathF.Abs(input1);
    }

    private float Atan(float input1, float input2, float input3)
    {
        return MathF.Atan(input1);
    }

    private float Cos(float input1, float input2, float input3)
    {
        return MathF.Cos(input1);
    }

    private float Differentiate(float input1, float input2, float input3)
    {
        float diff = input1 - storedValue;
        float newValue = storedValue + diff * MathF.Abs(input2);
        if (!float.IsNaN(newValue))
            storedValue = newValue;
        return diff;
    }

    private float Divide(float input1, float input2, float input3)
    {
        return (input2 == 0) ? 0f : input1 / input2;
    }

    private float Expt(float input1, float input2, float input3)
    {
        return MathF.Exp(input1);
    }

    private float GreaterThan(float input1, float input2, float input3)
    {
        return input1 > input2 ? 1.0f : -1.0f;
    }

    private float If(float input1, float input2, float input3)
    {
        return (input1 > 0) ? input2 : input3;
    }

    private float Integrate(float input1, float input2, float input3)
    {
        float newValue = storedValue + input1 - (MathF.Abs(input2) * storedValue);
        if (!float.IsNaN(newValue))
            storedValue = newValue;
        return storedValue;
    }

    private float Interpolate(float input1, float input2, float input3)
    {
        return Mathf.Lerp(input1, input2, input3);
    }

    private float Log(float input1, float input2, float input3)
    {
        return MathF.Log(MathF.Abs(input1));
    }

    private float Max(float input1, float input2, float input3)
    {
        return MathF.Max(MathF.Max(input1, input2), input3);
    }

    private float Memory(float input1, float input2, float input3)
    {
        if (input1 > 0)
            storedValue = input2;
        return storedValue;
    }

    private float Min(float input1, float input2, float input3)
    {
        return MathF.Min(MathF.Min(input1, input2), input3);
    }

    private float OscillateSaw(float input1, float input2, float input3)
    {
        return (input1 == -1f || input2 == 0) ? 0 : (Time.fixedTime % ((1f + input1) * input2)) * input3;
    }

    private float OscillateWave(float input1, float input2, float input3)
    {
        return MathF.Sin((Time.fixedTime * input1) + input2) * input3;
    }

    private float Product(float input1, float input2, float input3)
    {
        return input1 * input2;
    }

    private float Sigmoid(float input1, float input2, float input3)
    {
        return 1f / (1f + MathF.Exp(input1));
    }

    private float SignOf(float input1, float input2, float input3)
    {
        return MathF.Sign(input1);
    }

    private float Sin(float input1, float input2, float input3)
    {
        return MathF.Sin(input1);
    }

    private float Smooth(float input1, float input2, float input3)
    {
        float diff = input1 - storedValue;
        storedValue += MathF.Sign(diff) * MathF.Min(MathF.Abs(diff), MathF.Abs(input2));
        return storedValue;
    }

    private float Sum(float input1, float input2, float input3)
    {
        return input1 + input2;
    }

    private float SumThreshold(float input1, float input2, float input3)
    {
        return MathF.Min(input1 + input2, input3);
    }
}
