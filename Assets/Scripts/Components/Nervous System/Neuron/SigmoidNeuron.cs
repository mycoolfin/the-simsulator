using UnityEngine;
using System.Collections;

public class SigmoidNeuron : NeuronBase
{
    public override float Evaluate()
    {
        return 1f / (1f + Mathf.Exp(GetWeightedSumOfInputValues()));
    }
}
