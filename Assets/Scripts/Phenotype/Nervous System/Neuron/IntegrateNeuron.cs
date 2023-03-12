using System.Collections.Generic;
using UnityEngine;

public class IntegrateNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Integrate;

    private float storedValue = 0f;

    protected override float Evaluate()
    {
        List<float> inputValues = GetWeightedInputValues();
        float newValue = storedValue + inputValues[0] - (Mathf.Abs(inputValues[1]) * storedValue);
        if (!float.IsNaN(newValue))
            storedValue = newValue;
        return storedValue;
    }
}
