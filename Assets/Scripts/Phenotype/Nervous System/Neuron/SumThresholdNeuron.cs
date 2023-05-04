using System.Collections.Generic;
using UnityEngine;

public class SumThresholdNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.SumThreshold;

    protected override float Evaluate()
    {
        List<float> inputValues = WeightedInputValues;
        return Mathf.Min(inputValues[0] + inputValues[1], inputValues[2]);
    }
}
