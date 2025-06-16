using System;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public class SimsPhenotype : IPhenotype<SimsPhenotype>
    {
        public static int MaxLimbCount = 20;

        public Brain Brain { get; private set; }
        public List<Limb> Limbs { get; private set; }

        public event Action OnDispose;

        public SimsPhenotype(Brain brain, List<Limb> limbs)
        {
            Brain = brain ?? throw new ArgumentNullException(nameof(brain), "Brain cannot be null.");
            Limbs = limbs ?? throw new ArgumentNullException(nameof(limbs), "Limbs cannot be null.");
        }

        public void Dispose()
        {
            OnDispose?.Invoke();
        }
    }
}
