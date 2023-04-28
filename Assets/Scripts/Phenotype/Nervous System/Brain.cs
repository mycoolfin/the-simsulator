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

    public List<ISignalReceiver> GetSignalReceivers() => neurons.Cast<ISignalReceiver>().ToList();

    public List<ISignalEmitter> GetSignalEmitters() => neurons.Cast<ISignalEmitter>().ToList();
}
