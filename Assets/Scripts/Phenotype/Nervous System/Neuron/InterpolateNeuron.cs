using UnityEngine;

public class InterpolateNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Interpolate;
    
    protected override float Evaluate()
    {
        float[] inputValues = WeightedInputValues;
        return Mathf.Lerp(inputValues[0], inputValues[1], inputValues[2]);
    }
}
