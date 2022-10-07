using UnityEngine;

public class SignOfNeuron : NeuronBase
{
    public SignOfNeuron() : base(1) {}

    public override float Evaluate()
    {
        return Mathf.Sign(GetInputValues()[0]);
    }
}
