using System.Numerics;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public class ContactDirectionalLoadSensor : Sensor
    {
        public readonly Vector3 Axis;

        public ContactDirectionalLoadSensor(Vector3 axis) : base(SensorType.ContactDirectionalLoad)
        {
            Axis = axis;
        }
    }
}
