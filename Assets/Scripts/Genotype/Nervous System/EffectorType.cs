using System.Collections.Generic;

public enum EffectorType
{
    JointAngle
}

public static class EffectorTypeExtensions
{
    public static int NumberOfInputs(this EffectorType self)
    {
        switch (self)
        {
            case EffectorType.JointAngle: return 1;
            default: throw new System.Exception("Unknown number of inputs for type " + self.ToString());
        }
    }
}
