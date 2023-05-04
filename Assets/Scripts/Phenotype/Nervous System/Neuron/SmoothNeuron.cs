using System.Collections.Generic;
using UnityEngine;

public class SmoothNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Smooth;

    float smoothedValue = 0;

    protected override float Evaluate()
    {
        List<float> inputValues = WeightedInputValues;
        float diff = inputValues[0] - smoothedValue;
        smoothedValue += Mathf.Sign(diff) * Mathf.Min(Mathf.Abs(diff), Mathf.Abs(inputValues[1]));
        return smoothedValue; 
    }
}
