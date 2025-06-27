using Unity.Mathematics;
using mycoolfin.TheSimsulator;

public static class QuaternionExtensions
{
    public static quaternion ToQuaternion(this Quaternion quaternion)
    {
        return new quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
    }
}
