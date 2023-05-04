using UnityEngine;

public class LogNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Log;
    
    protected override float Evaluate()
    {
        return Mathf.Log(Mathf.Abs(WeightedInputValues[0]));
    }
}
