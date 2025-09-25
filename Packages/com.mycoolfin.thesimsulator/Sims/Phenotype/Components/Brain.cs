using System;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public class Brain : INeuronContainer
    {
        public const int MAX_NEURONS = 10;

        private readonly List<Neuron> neurons = new();
        public IEnumerable<Neuron> Neurons => neurons;

        public Brain(List<Neuron> neurons)
        {
            this.neurons = neurons ?? throw new ArgumentNullException(nameof(neurons), "Neurons cannot be null.");
        }
    }
}
