using UnityEngine;

public class CosNeuron : NeuronBase
{
    public CosNeuron() : base(1) {}

    public override float Evaluate()
    {
        return Mathf.Cos(GetInputValues()[0]);
    }
}
