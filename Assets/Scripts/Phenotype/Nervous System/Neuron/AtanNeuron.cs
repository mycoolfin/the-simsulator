using UnityEngine;

public class AtanNeuron : NeuronBase
{
    public AtanNeuron() : base(1) {}

    public override float Evaluate()
    {
        return Mathf.Atan(GetInputValues()[0]);
    }
}
