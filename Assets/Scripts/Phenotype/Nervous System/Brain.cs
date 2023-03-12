using System.Collections.Generic;
using System.Collections.ObjectModel;

public class Brain
{
    public List<NeuronBase> neurons;
    public List<List<float>> neuronInputPreferences;

    public Brain(ReadOnlyCollection<NeuronDefinition> neuronDefinitions)
    {
        neurons = new List<NeuronBase>();
        neuronInputPreferences = new List<List<float>>();
        for (int i = 0; i < neuronDefinitions.Count; i++)
        {
            neurons.Add(NeuronBase.CreateNeuron(neuronDefinitions[i].type, neuronDefinitions[i].inputWeights));
            neuronInputPreferences.Add(neuronDefinitions[i].inputPreferences);
        }
    }
}
