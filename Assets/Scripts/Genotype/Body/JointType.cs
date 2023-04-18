using System.Collections.Generic;

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
        return new Dictionary<JointType, int>
        {
            { JointType.Rigid, 0 },
            { JointType.Revolute, 1 },
            { JointType.Twist, 1 },
            { JointType.Universal, 0 }, // TODO: fix
            { JointType.BendTwist, 2 },
            { JointType.TwistBend, 2 },
            { JointType.Spherical, 0 }, // TODO: fix
        }[self];
    }
}
