public class Brain
{
    public NeuronBase[] neurons;
    public float[][] neuronInputPreferences;

    public Brain(NeuronDefinition[] neuronDefinitions)
    {
        neurons = new NeuronBase[neuronDefinitions.Length];
        neuronInputPreferences = new float[neuronDefinitions.Length][];
        for (int i = 0; i < neuronDefinitions.Length; i++)
        {
            neurons[i] = NeuronBase.CreateNeuron(neuronDefinitions[i].type, neuronDefinitions[i].inputWeights);
            neuronInputPreferences[i] = neuronDefinitions[i].inputPreferences;
        }
    }
}
