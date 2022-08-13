using UnityEngine;
using System.Collections;

public class SumNeuron : NeuronBase
{
    public override float Evaluate()
    {
        return GetWeightedSumOfInputValues();
    }
}
