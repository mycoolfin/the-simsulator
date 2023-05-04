using System.Collections.Generic;
using UnityEngine;

public class DifferentiateNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Differentiate;

    private float storedValue = 0f;

    protected override float Evaluate()
    {
        List<float> inputValues = WeightedInputValues;
        float diff = inputValues[0] - storedValue;
        float newValue = storedValue + diff * Mathf.Abs(inputValues[1]);
        if (!float.IsNaN(newValue))
            storedValue = newValue;
        return diff;
    }
}
