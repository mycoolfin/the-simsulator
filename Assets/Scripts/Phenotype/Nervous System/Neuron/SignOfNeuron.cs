using UnityEngine;

public class SignOfNeuron : NeuronBase
{
    public SignOfNeuron() : base(1) {}

    protected override float Evaluate()
    {
        return Mathf.Sign(WeightedInputValues[0]);
    }
}
