namespace mycoolfin.TheSimsulator.Sims
{
    public abstract class JointBase
    {
        public readonly Limb ParentLimb;
        public readonly Limb ChildLimb;

        public Vector3 ParentAnchor { get; private set; }
        public Vector3 ChildAnchor { get; private set; }

        public Vector3 DesiredAngles { get; private set; }
        public Vector3 ActualAngles { get; private set; }

        protected readonly bool XAxisEnabled;
        protected readonly bool YAxisEnabled;
        protected readonly bool ZAxisEnabled;

        public JointBase(Limb parentLimb, Limb childLimb, Vector3 parentAnchor, Vector3 childAnchor, bool xAxisEnabled, bool yAxisEnabled, bool zAxisEnabled)
        {
            ParentLimb = parentLimb ?? throw new System.ArgumentNullException(nameof(parentLimb), "Parent limb cannot be null.");
            ChildLimb = childLimb ?? throw new System.ArgumentNullException(nameof(childLimb), "Child limb cannot be null.");

            ParentAnchor = parentAnchor;
            ChildAnchor = childAnchor;

            DesiredAngles = Vector3.Zero;
            ActualAngles = Vector3.Zero;

            XAxisEnabled = xAxisEnabled;
            YAxisEnabled = yAxisEnabled;
            ZAxisEnabled = zAxisEnabled;
        }

        public void SetDesiredAngles(Vector3 desiredAngles)
        {
            if (!XAxisEnabled && desiredAngles.X != 0)
                throw new System.InvalidOperationException("X-axis is not enabled for this joint.");
            if (!YAxisEnabled && desiredAngles.Y != 0)
                throw new System.InvalidOperationException("Y-axis is not enabled for this joint.");
            if (!ZAxisEnabled && desiredAngles.Z != 0)
                throw new System.InvalidOperationException("Z-axis is not enabled for this joint.");

            DesiredAngles = desiredAngles;
        }

        public void SetActualAngles(Vector3 actualAngles)
        {
            ActualAngles = actualAngles;
        }
    }
}
