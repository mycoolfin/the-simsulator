using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public class SimsPhenotype : IPhenotype<SimsPhenotype>
    {
        public List<Limb> Limbs { get; private set; }
        public List<JointBase> Joints { get; private set; }

        public SimsPhenotype(List<Limb> limbs, List<JointBase> joints)
        {
            Limbs = limbs ?? throw new System.ArgumentNullException(nameof(limbs), "Limbs cannot be null.");
            Joints = joints ?? throw new System.ArgumentNullException(nameof(joints), "Joints cannot be null.");
        }

        public void Dispose()
        {
            Limbs.Clear();
            Joints.Clear();
        }
    }
}
