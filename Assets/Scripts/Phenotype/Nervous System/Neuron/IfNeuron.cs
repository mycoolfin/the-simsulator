﻿
public class IfNeuron : NeuronBase
{
    public IfNeuron() : base(3) {}

    protected override float Evaluate()
    {
        float[] inputValues = GetWeightedInputValues();
        return (inputValues[0] > 0) ? inputValues[1] : inputValues[2];
    }
}
