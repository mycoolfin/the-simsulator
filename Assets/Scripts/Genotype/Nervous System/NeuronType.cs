using System;

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
        return self switch
        {
            NeuronType.Abs => 1,
            NeuronType.Atan => 1,
            NeuronType.Cos => 1,
            NeuronType.Differentiate => 2,
            NeuronType.Divide => 2,
            NeuronType.Expt => 1,
            NeuronType.GreaterThan => 2,
            NeuronType.If => 3,
            NeuronType.Integrate => 2,
            NeuronType.Interpolate => 3,
            NeuronType.Log => 1,
            NeuronType.Max => 3,
            NeuronType.Memory => 2,
            NeuronType.Min => 3,
            NeuronType.OscillateSaw => 3,
            NeuronType.OscillateWave => 3,
            NeuronType.Product => 2,
            NeuronType.Sigmoid => 1,
            NeuronType.SignOf => 1,
            NeuronType.Sin => 1,
            NeuronType.Smooth => 2,
            NeuronType.Sum => 2,
            NeuronType.SumThreshold => 3,
            _ => throw new System.Exception("Unknown number of inputs for type " + self.ToString()),
        };
    }
}
