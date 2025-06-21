namespace mycoolfin.TheSimsulator.Sims
{
    public class RigidJoint : JointBase
    {
        public RigidJoint(Limb parentLimb, Limb childLimb, Vector3 parentSpaceAnchor, Vector3 parentSpaceXAxis, Vector3 parentSpaceYAxis, Vector3 parentSpaceZAxis, Vector3 angleLimits)
            : base(
                parentLimb,
                childLimb,
                parentSpaceAnchor,
parentSpaceXAxis,
parentSpaceYAxis,
parentSpaceZAxis,
                angleLimits,
                null,
                null,
                null
            )
        {
        }
    }
}
