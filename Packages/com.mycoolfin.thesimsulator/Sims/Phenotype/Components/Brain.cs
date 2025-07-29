using System;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public class Brain
    {
        public const int MAX_NEURONS = 10;

        public readonly List<Neuron> Neurons;

        public Brain(List<Neuron> neurons)
        {
            Neurons = neurons ?? throw new ArgumentNullException(nameof(neurons), "Neurons cannot be null.");
        }
    }
}
