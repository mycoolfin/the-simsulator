using System.Numerics;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public class JointAngleSensor : Sensor
    {
        public readonly Vector3 Axis;

        public JointAngleSensor(Vector3 axis) : base(SensorType.JointAngle)
        {
            Axis = axis;
        }
    }
}
