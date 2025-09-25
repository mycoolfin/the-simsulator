using System.Collections.Generic;
using System.Numerics;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public class LightSensorArray : ISensorContainer
    {
        public readonly LightSensor XAxisSensor = new(Vector3.UnitX);
        public readonly LightSensor YAxisSensor = new(Vector3.UnitY);
        public readonly LightSensor ZAxisSensor = new(Vector3.UnitZ);

        private readonly List<Sensor> sensors;
        public IEnumerable<Sensor> Sensors => sensors;

        public LightSensorArray()
        {
            sensors = new List<Sensor>
            {
                XAxisSensor,
                YAxisSensor,
                ZAxisSensor
            };
        }
    }
}
