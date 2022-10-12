using UnityEngine;

public class AtanNeuron : NeuronBase
{
    public AtanNeuron() : base(1) {}

    protected override float Evaluate()
    {
        return Mathf.Atan(GetWeightedInputValues()[0]);
    }
}
