namespace mycoolfin.TheSimsulator.Sims
{
    public class RevoluteJoint : JointBase
    {
        public RevoluteJoint(Limb parentLimb, Limb childLimb, Vector3 localParentAnchor, Vector3 localChildAnchor, Vector3 angleLimits)
            : base(
                parentLimb,
                childLimb,
                localParentAnchor,
                localChildAnchor,
                angleLimits,
                new(new(), new()),
                null,
                null
            )
        {
        }
    }
}
