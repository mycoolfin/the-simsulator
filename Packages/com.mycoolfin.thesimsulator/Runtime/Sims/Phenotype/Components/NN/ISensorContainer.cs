using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public interface ISensorContainer
    {
        public IEnumerable<SensorBase> Sensors { get; }
    }
}
