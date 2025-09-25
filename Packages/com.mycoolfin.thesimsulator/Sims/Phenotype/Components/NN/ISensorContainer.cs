using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public interface ISensorContainer
    {
        IEnumerable<Sensor> Sensors { get; }
    }
}
