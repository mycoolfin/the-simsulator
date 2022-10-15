using System.Linq;
using UnityEngine;

public abstract class EffectorBase : ISignalReceiver
{
    private readonly int numberOfInputs = 1;
    private ISignalEmitter[] inputs;
    public ISignalEmitter[] Inputs
    {
        get { return inputs; }
        set
        {
            if (value.Length == numberOfInputs)
                inputs = value;
            else
                throw new System.ArgumentOutOfRangeException("Expected " + numberOfInputs.ToString() + " inputs.");

        }
    }
    public float[] Weights { get; set; }
    private float?[] inputOverrides;
    public float?[] InputOverrides
    {
        get { return inputOverrides; }
        set
        {
            if (value.Length == numberOfInputs)
                inputOverrides = value;
            else
                throw new System.ArgumentOutOfRangeException("Expected " + numberOfInputs.ToString() + " input overrides");
        }
    }
    public float[] WeightedInputValues
    {
        get { return Inputs.Select((input, i) => (inputOverrides[i] ?? input.OutputValue) * Weights[i]).ToArray(); }
    }
    public float Excitation
    {
        get { return Mathf.Clamp(WeightedInputValues[0], -1f, 1f); }
    }

    protected EffectorBase()
    {
        Inputs = new ISignalEmitter[numberOfInputs];
        InputOverrides = new float?[numberOfInputs];
        Weights = new float[numberOfInputs];
    }
}
