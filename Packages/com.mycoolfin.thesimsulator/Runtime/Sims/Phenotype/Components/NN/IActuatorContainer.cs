using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public interface IActuatorContainer
    {
        public IEnumerable<ActuatorBase> Actuators { get; }
    }
}
