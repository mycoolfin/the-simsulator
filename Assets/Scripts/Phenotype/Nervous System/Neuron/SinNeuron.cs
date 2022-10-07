using UnityEngine;

public class SinNeuron : NeuronBase
{
    public SinNeuron() : base(1) {}
    
    public override float Evaluate()
    {
        return Mathf.Sin(GetInputValues()[0]);
    }
}
