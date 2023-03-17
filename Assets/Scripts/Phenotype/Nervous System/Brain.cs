using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public class Brain
{
    public List<NeuronBase> neurons;

    public Brain(ReadOnlyCollection<NeuronDefinition> neuronDefinitions)
    {
        neurons = neuronDefinitions.Select(n => NeuronBase.CreateNeuron(n)).ToList();
    }
}
