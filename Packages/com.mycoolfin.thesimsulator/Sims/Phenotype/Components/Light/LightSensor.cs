using System.Numerics;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public class LightSensor : Sensor
    {
        public readonly Vector3 Axis;

        public LightSensor(Vector3 axis) : base(SensorType.Light)
        {
            Axis = axis;
        }
    }
}
