using UnityEngine;

public class SumThresholdNeuron : NeuronBase
{
    public SumThresholdNeuron() : base(3) {}

    protected override float Evaluate()
    {
        float[] inputValues = GetWeightedInputValues();
        return Mathf.Min(inputValues[0] + inputValues[1], inputValues[2]);
    }
}
