using UnityEngine;
using System.Collections;

public class SigmoidNeuron : NeuronBase
{
    public SigmoidNeuron() : base(1) {}

    public override float Evaluate()
    {
        return 1f / (1f + Mathf.Exp(GetInputValues()[0]));
    }
}
