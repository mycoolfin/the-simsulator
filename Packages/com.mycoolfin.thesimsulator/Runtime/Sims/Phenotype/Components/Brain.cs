using System;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public class Brain : INeuronContainer
    {
        public IEnumerable<Neuron> Neurons { get; private set; }

        public Brain(List<Neuron> neurons)
        {
            Neurons = neurons ?? throw new ArgumentNullException(nameof(neurons), "Neurons cannot be null.");
        }
    }
}
