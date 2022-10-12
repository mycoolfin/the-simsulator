using UnityEngine;

public class SinNeuron : NeuronBase
{
    public SinNeuron() : base(1) {}
    
    protected override float Evaluate()
    {
        return Mathf.Sin(GetWeightedInputValues()[0]);
    }
}
