using System.Collections.Generic;

public enum SensorType
{
    JointAngle
}

public static class SensorTypeExtensions
{
    public static int NumberOfInputs(this SensorType self)
    {
        return new Dictionary<SensorType, int>
        {
            { SensorType.JointAngle, 1 }
        }[self];
    }
}
