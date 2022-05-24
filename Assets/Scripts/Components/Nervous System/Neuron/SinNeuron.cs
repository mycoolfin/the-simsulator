using UnityEngine;
using System.Collections;

public class SinNeuron : NeuronBase
{
    public override float Evaluate()
    {
        return Mathf.Sin(GetWeightedSumOfInputValues());
    }
}
