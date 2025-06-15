namespace mycoolfin.TheSimsulator.Sims
{
    public class HingeJoint : JointBase
    {
        public HingeJoint(Limb parentLimb, Limb childLimb, Vector3 parentAnchor, Vector3 childAnchor)
            : base(
                parentLimb,
                childLimb,
                parentAnchor,
                childAnchor,
                xAxisEnabled: true,
                yAxisEnabled: false,
                zAxisEnabled: false
            )
        {
        }
    }
}
