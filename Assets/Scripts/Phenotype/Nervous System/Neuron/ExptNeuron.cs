using UnityEngine;

public class ExptNeuron : NeuronBase
{
    public ExptNeuron() : base(1) {}

    protected override float Evaluate()
    {
        return Mathf.Exp(WeightedInputValues[0]);
    }
}
