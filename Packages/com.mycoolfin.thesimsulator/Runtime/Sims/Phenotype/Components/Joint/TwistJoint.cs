namespace mycoolfin.TheSimsulator.Sims
{
    public class TwistJoint : JointBase
    {
        public TwistJoint(Limb parentLimb, Limb childLimb, Vector3 localParentAnchor, Vector3 localChildAnchor, Vector3 angleLimits)
            : base(
                parentLimb,
                childLimb,
                localParentAnchor,
                localChildAnchor,
                angleLimits,
                null,
                null,
                new(new(), new())
            )
        {
        }
    }
}
