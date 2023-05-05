using System.Collections.Generic;

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
        switch (self)
        {
            case NeuronType.Abs: return 1;
            case NeuronType.Atan: return 1;
            case NeuronType.Cos: return 1;
            case NeuronType.Differentiate: return 2;
            case NeuronType.Divide: return 2;
            case NeuronType.Expt: return 1;
            case NeuronType.GreaterThan: return 2;
            case NeuronType.If: return 3;
            case NeuronType.Integrate: return 2;
            case NeuronType.Interpolate: return 3;
            case NeuronType.Log: return 1;
            case NeuronType.Max: return 3;
            case NeuronType.Memory: return 2;
            case NeuronType.Min: return 3;
            case NeuronType.OscillateSaw: return 3;
            case NeuronType.OscillateWave: return 3;
            case NeuronType.Product: return 2;
            case NeuronType.Sigmoid: return 1;
            case NeuronType.SignOf: return 1;
            case NeuronType.Sin: return 1;
            case NeuronType.Smooth: return 2;
            case NeuronType.Sum: return 2;
            case NeuronType.SumThreshold: return 3;
            default: throw new System.Exception("Unknown number of inputs for type " + self.ToString());
        }
    }
}
