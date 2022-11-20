using UnityEngine;

public class OscillateWaveNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.OscillateWave;

    protected override float Evaluate()
    {
        float[] inputValues = WeightedInputValues;
        return Mathf.Sin((Time.time * inputValues[0]) + inputValues[1]) * inputValues[2];
    }
}
