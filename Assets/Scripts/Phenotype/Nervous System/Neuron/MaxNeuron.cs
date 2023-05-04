using System.Collections.Generic;
using UnityEngine;

public class MaxNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Max;

    protected override float Evaluate()
    {
        List<float> inputValues = WeightedInputValues;
        return Mathf.Max(inputValues[0], inputValues[1], inputValues[2]);
    }
}
