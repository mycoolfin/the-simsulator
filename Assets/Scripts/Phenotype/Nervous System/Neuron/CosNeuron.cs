using UnityEngine;

public class CosNeuron : NeuronBase
{
    public CosNeuron() : base(1) {}

    protected override float Evaluate()
    {
        return Mathf.Cos(GetWeightedInputValues()[0]);
    }
}
