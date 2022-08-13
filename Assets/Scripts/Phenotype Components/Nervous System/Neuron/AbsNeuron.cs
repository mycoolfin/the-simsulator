using UnityEngine;
using System.Collections;

public class AbsNeuron : NeuronBase
{
    public override float Evaluate()
    {
        return Mathf.Abs(GetWeightedSumOfInputValues());
    }
}
