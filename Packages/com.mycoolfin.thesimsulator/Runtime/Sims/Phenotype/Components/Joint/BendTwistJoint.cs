namespace mycoolfin.TheSimsulator.Sims
{
    public class BendTwistJoint : JointBase
    {
        public BendTwistJoint(Limb parentLimb, Limb childLimb, Vector3 localParentAnchor, Vector3 localChildAnchor, Vector3 angleLimits)
            : base(
                parentLimb,
                childLimb,
                localParentAnchor,
                localChildAnchor,
                angleLimits,
                new(new(), new()),
                null,
                new(new(), new())
            )
        {
        }
    }
}
