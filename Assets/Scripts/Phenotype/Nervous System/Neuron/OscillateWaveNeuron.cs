using UnityEngine;

public class OscillateWaveNeuron : NeuronBase
{
    public OscillateWaveNeuron() : base(3) {}

    protected override float Evaluate()
    {
        float[] inputValues = GetWeightedInputValues();
        return Mathf.Sin((Time.time * inputValues[0]) + inputValues[1]) * inputValues[2];
    }
}
