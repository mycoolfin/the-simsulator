using UnityEngine;
using System.Collections;

public class SignOfNeuron : NeuronBase
{
    public override float Evaluate()
    {
        return Mathf.Sign(GetWeightedSumOfInputValues());
    }
}
