using UnityEngine;

public class AbsNeuron : NeuronBase
{
    public AbsNeuron() : base(1) {}

    public override float Evaluate()
    {
        return Mathf.Abs(GetInputValues()[0]);
    }
}
