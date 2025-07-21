using System.Numerics;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.Math
{
    public static class QuaternionExtensions
    {
        public static quaternion ToQuaternion(this Quaternion quaternion)
        {
            return new quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        }
    }
}
