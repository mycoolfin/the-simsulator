using UnityEngine;

public class LogNeuron : NeuronBase
{
    public LogNeuron() : base(1) {}
    
    public override float Evaluate()
    {
        return Mathf.Log(GetInputValues()[0]);
    }
}
