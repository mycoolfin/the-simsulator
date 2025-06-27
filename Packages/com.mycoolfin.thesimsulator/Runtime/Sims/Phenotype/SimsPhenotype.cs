using System;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public class SimsPhenotype : IPhenotype<SimsPhenotype>
    {
        public const int MAX_LIMBS = 20;
        public const int MAX_NEURONS = SimsGenotype.MAX_NEURON_DEFINITIONS;

        public int Gid { get; private set; }

        public Brain Brain { get; private set; }
        public List<Limb> Limbs { get; private set; }

        public event Action OnDispose;

        public SimsPhenotype(Brain brain, List<Limb> limbs)
        {
            byte[] buffer = new byte[sizeof(int)];
            SharedRandom.NextBytes(buffer);
            Gid = Math.Abs(BitConverter.ToInt32(buffer, 0));
            Brain = brain ?? throw new ArgumentNullException(nameof(brain), "Brain cannot be null.");
            Limbs = limbs ?? throw new ArgumentNullException(nameof(limbs), "Limbs cannot be null.");
        }

        public void Dispose()
        {
            OnDispose?.Invoke();
        }
    }
}
