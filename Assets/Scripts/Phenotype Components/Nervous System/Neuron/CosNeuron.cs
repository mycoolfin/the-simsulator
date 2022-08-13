using UnityEngine;
using System.Collections;

public class CosNeuron : NeuronBase
{
    public override float Evaluate()
    {
        return Mathf.Cos(GetWeightedSumOfInputValues());
    }
}
