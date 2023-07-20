public enum JointType
{
    Rigid,
    Revolute,
    Twist,
    Universal,
    BendTwist,
    TwistBend,
    Spherical
}

public static class JointTypeExtensions
{
    public static int DegreesOfFreedom(this JointType self)
    {
        switch (self)
        {
            case JointType.Rigid: return 0;
            case JointType.Revolute: return 1;
            case JointType.Twist: return 1;
            case JointType.Universal: return 2;
            case JointType.BendTwist: return 2;
            case JointType.TwistBend: return 2;
            case JointType.Spherical: return 3;
            default: throw new System.Exception("Unknown degrees of freedom for type " + self.ToString());
        }
    }
}
