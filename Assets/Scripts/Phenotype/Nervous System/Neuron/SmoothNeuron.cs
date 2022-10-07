using UnityEngine;

public class SmoothNeuron : NeuronBase
{
    public SmoothNeuron() : base(2) {}

    float smoothedValue = 0;

    public override float Evaluate()
    {
        float[] inputValues = GetInputValues();
        float diff = inputValues[0] - smoothedValue;
        smoothedValue += Mathf.Sign(diff) * Mathf.Min(Mathf.Abs(diff), Mathf.Abs(inputValues[0]));
        return smoothedValue; 
    }
}
