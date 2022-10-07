using UnityEngine;

public class SumThresholdNeuron : NeuronBase
{
    public SumThresholdNeuron() : base(3) {}

    public override float Evaluate()
    {
        float[] inputValues = GetInputValues();
        return Mathf.Min(inputValues[0] + inputValues[1], inputValues[2]);
    }
}
