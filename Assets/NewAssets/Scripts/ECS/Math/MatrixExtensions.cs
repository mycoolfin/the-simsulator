using Unity.Mathematics;
using mycoolfin.TheSimsulator;

public static class MatrixExtensions
{
    public static float4x4 ToFloat4x4(this Matrix4x4 matrix)
    {
        return new float4x4(
            new float4(matrix.M11, matrix.M12, matrix.M13, matrix.M14),
            new float4(matrix.M21, matrix.M22, matrix.M23, matrix.M24),
            new float4(matrix.M31, matrix.M32, matrix.M33, matrix.M34),
            new float4(matrix.M41, matrix.M42, matrix.M43, matrix.M44)
        );
    }
}
