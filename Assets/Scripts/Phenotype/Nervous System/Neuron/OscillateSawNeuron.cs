using System.Collections.Generic;
using UnityEngine;

public class OscillateSawNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.OscillateSaw;

    protected override float Evaluate()
    {
        List<float> inputValues = WeightedInputValues;
        return (inputValues[0] == -1f || inputValues[1] == 0) ? 0 : (Time.time % ((1f + inputValues[0]) * inputValues[1])) * inputValues[2];
    }
}
