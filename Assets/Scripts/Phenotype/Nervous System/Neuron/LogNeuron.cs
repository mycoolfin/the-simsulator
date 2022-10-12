using UnityEngine;

public class LogNeuron : NeuronBase
{
    public LogNeuron() : base(1) {}
    
    protected override float Evaluate()
    {
        return Mathf.Log(GetWeightedInputValues()[0]);
    }
}
