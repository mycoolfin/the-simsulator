using UnityEngine;

public class InterpolateNeuron : NeuronBase
{
    public InterpolateNeuron() : base(3) {}
    
    protected override float Evaluate()
    {
        float[] inputValues = GetWeightedInputValues();
        return Mathf.Lerp(inputValues[0], inputValues[1], inputValues[2]);
    }
}
