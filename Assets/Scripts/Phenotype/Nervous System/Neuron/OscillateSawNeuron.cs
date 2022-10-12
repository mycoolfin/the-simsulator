using UnityEngine;

public class OscillateSawNeuron : NeuronBase
{
    public OscillateSawNeuron() : base(3) {}

    protected override float Evaluate()
    {
        float[] inputValues = GetWeightedInputValues();
        return (Time.time % ((1f + inputValues[0]) * inputValues[1])) * inputValues[2];
    }
}
