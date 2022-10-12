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
    public float Excitation
    {
        get { Debug.Log(inputs[0].OutputValue + " : " + Weights[0]);return inputs[0].OutputValue * Weights[0]; }
        set { inputs[0].OutputValue = Weights[0] == 0 ? 0 : value / Weights[0]; } // Debugging override.
    }

    protected EffectorBase()
    {
        Inputs = new ISignalEmitter[numberOfInputs];
        Weights = new float[numberOfInputs];
    }
}
