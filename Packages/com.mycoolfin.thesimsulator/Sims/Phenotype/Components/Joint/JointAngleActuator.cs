namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    public class JointAngleActuator : Actuator
    {
        public readonly JointAxis Axis;

        public JointAngleActuator(JointAxis axis) : base(ActuatorType.JointAngle)
        {
            Axis = axis;
        }
    }
}
