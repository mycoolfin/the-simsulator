using UnityEngine;
using System.Collections;

public class ExptNeuron : NeuronBase
{
    public override float Evaluate()
    {
        return Mathf.Exp(GetWeightedSumOfInputValues());
    }
}
