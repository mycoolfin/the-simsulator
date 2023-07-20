public enum SensorType
{
    JointAngle
}

public static class SensorTypeExtensions
{
    public static int NumberOfInputs(this SensorType self)
    {
        switch (self)
        {
            case SensorType.JointAngle: return 1;
            default: throw new System.Exception("Unknown number of inputs for type " + self.ToString());
        }
    }
}
