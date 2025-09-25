using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public interface IActuatorContainer
    {
        IEnumerable<Actuator> Actuators { get; }
    }
}
