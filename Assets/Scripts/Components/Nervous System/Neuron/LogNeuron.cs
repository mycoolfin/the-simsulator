using UnityEngine;
using System.Collections;

public class LogNeuron : NeuronBase
{
    public override float Evaluate()
    {
        return Mathf.Log(GetWeightedSumOfInputValues());
    }
}
