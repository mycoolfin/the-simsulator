using UnityEngine;

public class SinNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Sin;
    
    protected override float Evaluate()
    {
        return Mathf.Sin(GetWeightedInputValues()[0]);
    }
}
