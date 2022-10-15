using UnityEngine;

public class OscillateSawNeuron : NeuronBase
{
    public OscillateSawNeuron() : base(3) {}

    protected override float Evaluate()
    {
        float[] inputValues = WeightedInputValues;
        return (inputValues[0] == -1f || inputValues[1] == 0) ? 0 : (Time.time % ((1f + inputValues[0]) * inputValues[1])) * inputValues[2];
    }
}
