using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class EffectorBase : ISignalReceiver
{
    private readonly int numberOfInputs = 1;
    private List<ISignalEmitter> inputs;
    public List<ISignalEmitter> Inputs
    {
        get { return inputs; }
        set
        {
            if (value.Count == numberOfInputs)
                inputs = value;
            else
                throw new System.ArgumentException("Expected " + numberOfInputs.ToString() + " inputs, got " + value.Count + ".");

        }
    }
    public List<float> Weights { get; set; }
    private List<float?> inputOverrides;
    public List<float?> InputOverrides
    {
        get { return inputOverrides; }
        set
        {
            if (value.Count == numberOfInputs)
                inputOverrides = value;
            else
                throw new System.ArgumentException("Expected " + numberOfInputs.ToString() + " input overrides, got " + value.Count + ".");
        }
    }
    public List<float> GetWeightedInputValues()
    {
        return Inputs.Select((input, i) => (inputOverrides[i] ?? input.OutputValue) * Weights[i]).ToList();
    }
    public float GetExcitation()
    {
        return Mathf.Clamp(GetWeightedInputValues()[0], -1f, 1f);
    }

    protected EffectorBase()
    {
        Inputs = new ISignalEmitter[numberOfInputs].ToList();
        InputOverrides = new float?[numberOfInputs].ToList();
        Weights = new float[numberOfInputs].ToList();
    }
}
