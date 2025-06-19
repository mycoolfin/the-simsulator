using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public interface INeuronContainer
    {
        public IEnumerable<Neuron> Neurons { get; }
    }
}
