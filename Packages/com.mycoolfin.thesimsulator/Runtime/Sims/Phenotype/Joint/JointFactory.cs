namespace mycoolfin.TheSimsulator.Sims
{
    public static class JointFactory
    {
        public static JointBase CreateJoint(JointType jointType, Limb parentLimb, Limb childLimb, Vector3 localParentAnchor, Vector3 localChildAnchor, Vector3 angleLimits)
        {
            return jointType switch
            {
                JointType.Revolute => new RevoluteJoint(
                    parentLimb,
                    childLimb,
                    localParentAnchor,
                    localChildAnchor,
                    angleLimits
                ),
                JointType.Rigid => new RigidJoint(
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
