public class IfNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.If;

    protected override float Evaluate()
    {
        float[] inputValues = WeightedInputValues;
        return (inputValues[0] > 0) ? inputValues[1] : inputValues[2];
    }
}
