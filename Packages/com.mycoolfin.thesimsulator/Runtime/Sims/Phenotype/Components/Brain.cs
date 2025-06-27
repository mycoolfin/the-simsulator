using System;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public class Brain : INeuronContainer
    {
        public const int MAX_NEURONS = 10;

        public IEnumerable<Neuron> Neurons { get; private set; }

        public Brain(List<Neuron> neurons)
        {
            Neurons = neurons ?? throw new ArgumentNullException(nameof(neurons), "Neurons cannot be null.");
        }
    }
}
