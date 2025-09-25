using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public interface INeuronContainer
    {
        IEnumerable<Neuron> Neurons { get; }
    }
}
