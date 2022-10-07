using UnityEngine;

public class ExptNeuron : NeuronBase
{
    public ExptNeuron() : base(1) {}

    public override float Evaluate()
    {
        return Mathf.Exp(GetInputValues()[0]);
    }
}
