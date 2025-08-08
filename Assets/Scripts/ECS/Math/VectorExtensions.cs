using System.Numerics;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.Math
{
    public static class VectorExtensions
    {
        public static float3 ToFloat3(this Vector3 vector)
        {
            return new float3(vector.X, vector.Y, vector.Z);
        }

        public static float4 ToFloat4(this Vector4 vector)
        {
            return new float4(vector.X, vector.Y, vector.Z, vector.W);
        }
    }
}
