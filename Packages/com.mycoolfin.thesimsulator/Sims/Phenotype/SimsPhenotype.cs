using System;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    using Core.Phenotype;
    using Genotype;

    public class SimsPhenotype : IPhenotype<SimsPhenotype>
    {
        public const int MAX_LIMBS = 24;
        public const int MAX_NEURONS = MAX_LIMBS * Node.MAX_NEURON_DEFINITIONS + SimsGenotype.MAX_BRAIN_NEURON_DEFINITIONS;
        public const float MIN_LIMB_DIMENSION = 0.05f;
        public const float MAX_LIMB_DIMENSION = 10f;

        public ulong Gid { get; private set; }

        public Brain Brain { get; private set; }
        public List<Limb> Limbs { get; private set; }

        public event Action OnDispose;

        public SimsPhenotype(Brain brain, List<Limb> limbs)
        {
            Gid = SharedRandom.NextUInt64();
            Brain = brain ?? throw new ArgumentNullException(nameof(brain), "Brain cannot be null.");
            Limbs = limbs ?? throw new ArgumentNullException(nameof(limbs), "Limbs cannot be null.");
        }

        public void Dispose()
        {
            OnDispose?.Invoke();
        }
    }
}
