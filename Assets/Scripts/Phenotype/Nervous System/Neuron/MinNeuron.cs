using UnityEngine;

public class MinNeuron : NeuronBase
{
    public MinNeuron() : base(3) {}

    protected override float Evaluate()
    {
        return Mathf.Max(GetWeightedInputValues());
    }
}
