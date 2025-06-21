namespace mycoolfin.TheSimsulator.Sims
{
    public static class JointFactory
    {
        public static JointBase CreateJoint(JointType jointType, Limb parentLimb, Limb childLimb, Vector3 parentSpaceAnchor, Vector3 parentSpaceXAxis, Vector3 parentSpaceYAxis, Vector3 parentSpaceZAxis, Vector3 angleLimits)
        {
            return jointType switch
            {
                JointType.Rigid => new RigidJoint(
                    parentLimb,
                    childLimb,
                    parentSpaceAnchor,
parentSpaceXAxis,
parentSpaceYAxis,
parentSpaceZAxis,
angleLimits
                ),
                JointType.Revolute => new RevoluteJoint(
                    parentLimb,
                    childLimb,
                    parentSpaceAnchor,
parentSpaceXAxis,
parentSpaceYAxis,
parentSpaceZAxis,
angleLimits
                ),
                JointType.Twist => new TwistJoint(
                    parentLimb,
                    childLimb,
                    parentSpaceAnchor,
parentSpaceXAxis,
parentSpaceYAxis,
parentSpaceZAxis,
angleLimits
                ),
                JointType.Universal => new UniversalJoint(
                    parentLimb,
                    childLimb,
                    parentSpaceAnchor,
parentSpaceXAxis,
parentSpaceYAxis,
parentSpaceZAxis,
angleLimits
                ),
                JointType.BendTwist => new BendTwistJoint(
                    parentLimb,
                    childLimb,
                    parentSpaceAnchor,
parentSpaceXAxis,
parentSpaceYAxis,
parentSpaceZAxis,
angleLimits
                ),
                JointType.TwistBend => new TwistBendJoint(
                    parentLimb,
                    childLimb,
                    parentSpaceAnchor,
parentSpaceXAxis,
parentSpaceYAxis,
parentSpaceZAxis,
angleLimits
                ),
                JointType.Spherical => new SphericalJoint(
                    parentLimb,
                    childLimb,
                    parentSpaceAnchor,
parentSpaceXAxis,
parentSpaceYAxis,
parentSpaceZAxis,
angleLimits
                ),
                _ => throw new System.ArgumentException($"Unsupported joint type: {jointType}"),
            };
        }
    }
}
