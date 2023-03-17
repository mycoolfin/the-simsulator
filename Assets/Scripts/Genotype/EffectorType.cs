using System.Collections.Generic;

public enum EffectorType
{
    JointAngle
}

public static class EffectorTypeExtensions
{
    public static int NumberOfInputs(this EffectorType self)
    {
        return new Dictionary<EffectorType, int>
        {
            { EffectorType.JointAngle, 1 }
        }[self];
    }
}
