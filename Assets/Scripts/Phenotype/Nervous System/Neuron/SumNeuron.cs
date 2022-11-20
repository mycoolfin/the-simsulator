public class SumNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Sum;

    protected override float Evaluate()
    {
        float[] inputValues = WeightedInputValues;
        return inputValues[0] + inputValues[1];
    }
}
