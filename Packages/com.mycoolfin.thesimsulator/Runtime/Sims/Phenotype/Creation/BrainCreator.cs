using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public static class BrainCreator
    {
        public static Brain CreateBrain(List<NeuronDefinition> neuronDefinitions, Dictionary<ISignalReceiver, InputSetDefinition> receiverToInputDefinitionSetGidMap)
        {
            List<Neuron> neurons = new();
            if (neuronDefinitions != null)
            {
                foreach (NeuronDefinition nd in neuronDefinitions)
                {
                    Neuron neuron = new(nd.ActivationFunction);
                    neurons.Add(neuron);
                    receiverToInputDefinitionSetGidMap[neuron] = nd.Inputs;
                }
            }
            return new Brain(neurons);
        }
    }
}
