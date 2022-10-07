using UnityEngine;

public class OscillateWaveNeuron : NeuronBase
{
    public OscillateWaveNeuron() : base(3) {}

    public override float Evaluate()
    {
        float[] inputValues = GetInputValues();
        return Mathf.Sin((Time.time * inputValues[0]) + inputValues[1]) * inputValues[2];
    }
}
