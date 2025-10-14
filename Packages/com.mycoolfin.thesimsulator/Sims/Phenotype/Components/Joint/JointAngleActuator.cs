using System.Numerics;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public class JointAngleActuator : Actuator
    {
        public readonly Vector3 Axis;

        public JointAngleActuator(Vector3 axis) : base(ActuatorType.JointAngle)
        {
            Axis = axis;
        }
    }
}
