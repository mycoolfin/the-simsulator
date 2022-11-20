public class MemoryNeuron : NeuronBase
{
    protected override NeuronType TypeOfNeuron => NeuronType.Memory;

    private float storedValue = 0f;
    
    protected override float Evaluate()
    {
        float[] inputValues = WeightedInputValues;
        if (inputValues[0] > 0)
            storedValue = inputValues[1];
        return storedValue;
    }
}
