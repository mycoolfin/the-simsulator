using UnityEngine;
using System.Collections;

public class SigmoidNeuron : NeuronBase
{
    public SigmoidNeuron() : base(1) {}

    protected override float Evaluate()
    {
        return 1f / (1f + Mathf.Exp(GetWeightedInputValues()[0]));
    }
}
