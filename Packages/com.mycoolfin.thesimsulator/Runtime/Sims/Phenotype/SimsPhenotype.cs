using System;
using System.Collections.Generic;
using mycoolfin.TheSimsulator.Sims.Genotype;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public class SimsPhenotype : IPhenotype<SimsPhenotype>
    {
        public const int MAX_LIMBS = 20;
        public const int MAX_NEURONS = SimsGenotype.MAX_NEURON_DEFINITIONS;
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
