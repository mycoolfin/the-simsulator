using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public class Brain
{
    public List<Neuron> neurons;

    public Brain(ReadOnlyCollection<NeuronDefinition> neuronDefinitions)
    {
        neurons = neuronDefinitions.Select(n => new Neuron(n)).ToList();
    }

    public List<SignalReceiver> GetSignalReceivers() => neurons.Select(n => n.Processor.Receiver).ToList();

    public List<SignalEmitter> GetSignalEmitters() => neurons.Select(n => n.Processor.Emitter).ToList();
}
