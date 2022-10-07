using UnityEngine;

public class InterpolateNeuron : NeuronBase
{
    public InterpolateNeuron() : base(3) {}
    
    public override float Evaluate()
    {
        float[] inputValues = GetInputValues();
        return Mathf.Lerp(inputValues[0], inputValues[1], inputValues[2]);
    }
}
