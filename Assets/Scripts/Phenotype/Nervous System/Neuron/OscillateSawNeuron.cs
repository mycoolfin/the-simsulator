using UnityEngine;

public class OscillateSawNeuron : NeuronBase
{
    public OscillateSawNeuron() : base(3) {}

    public override float Evaluate()
    {
        float[] inputValues = GetInputValues();
        return (Time.time % ((1f + inputValues[0]) * inputValues[1])) * inputValues[2];
    }
}
