namespace mycoolfin.TheSimsulator.Sims
{
    public static class JointFactory
    {
        public static JointBase CreateJoint(JointType jointType, Limb parentLimb, Limb childLimb, Vector3 localParentAnchor, Vector3 localChildAnchor, Vector3 angleLimits)
        {
            return jointType switch
            {
                JointType.Rigid => new RigidJoint(
                    parentLimb,
                    childLimb,
                    localParentAnchor,
                    localChildAnchor,
                    angleLimits
                ),
                JointType.Revolute => new RevoluteJoint(
                    parentLimb,
                    childLimb,
                    localParentAnchor,
                    localChildAnchor,
                    angleLimits
                ),
                JointType.Twist => new TwistJoint(
                    parentLimb,
                    childLimb,
                    localParentAnchor,
                    localChildAnchor,
                    angleLimits
                ),
                JointType.Universal => new UniversalJoint(
                    parentLimb,
                    childLimb,
                    localParentAnchor,
                    localChildAnchor,
                    angleLimits
                ),
                JointType.BendTwist => new BendTwistJoint(
                    parentLimb,
                    childLimb,
                    localParentAnchor,
                    localChildAnchor,
                    angleLimits
                ),
                JointType.TwistBend => new TwistBendJoint(
                    parentLimb,
                    childLimb,
                    localParentAnchor,
                    localChildAnchor,
                    angleLimits
                ),
                JointType.Spherical => new SphericalJoint(
                    parentLimb,
                    childLimb,
                    localParentAnchor,
                    localChildAnchor,
                    angleLimits
                ),
                _ => throw new System.ArgumentException($"Unsupported joint type: {jointType}"),
            };
        }
    }
}
