using UnityEngine;
using System.Collections;

public class AtanNeuron : NeuronBase
{
    public override float Evaluate()
    {
        return Mathf.Atan(GetWeightedSumOfInputValues());
    }
}
