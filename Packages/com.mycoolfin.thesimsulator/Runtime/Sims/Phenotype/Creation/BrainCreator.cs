using System.Collections.Generic;
using mycoolfin.TheSimsulator.Sims.Genotype;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public static class BrainCreator
    {
        public static Brain CreateBrain(IEnumerable<NeuronDefinition> neuronDefinitions, Dictionary<ISignalReceiver, InputSetDefinition> receiverToInputDefinitionSetGidMap)
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
