// Homemade structs for predictable memory layout across runtimes.

using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator
{
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public readonly struct Matrix4x4
    {
        [FieldOffset(0)] public readonly float M11;
        [FieldOffset(4)] public readonly float M12;
        [FieldOffset(8)] public readonly float M13;
        [FieldOffset(12)] public readonly float M14;
        [FieldOffset(16)] public readonly float M21;
        [FieldOffset(20)] public readonly float M22;
        [FieldOffset(24)] public readonly float M23;
        [FieldOffset(28)] public readonly float M24;
        [FieldOffset(32)] public readonly float M31;
        [FieldOffset(36)] public readonly float M32;
        [FieldOffset(40)] public readonly float M33;
        [FieldOffset(44)] public readonly float M34;
        [FieldOffset(48)] public readonly float M41;
        [FieldOffset(52)] public readonly float M42;
        [FieldOffset(56)] public readonly float M43;
        [FieldOffset(60)] public readonly float M44;

        public static readonly Matrix4x4 Identity = new(
            1f, 0f, 0f, 0f,
            0f, 1f, 0f, 0f,
            0f, 0f, 1f, 0f,
            0f, 0f, 0f, 1f
        );

        public Matrix4x4(float m11, float m12, float m13, float m14,
                         float m21, float m22, float m23, float m24,
                         float m31, float m32, float m33, float m34,
                         float m41, float m42, float m43, float m44)
        {
            M11 = m11; M12 = m12; M13 = m13; M14 = m14;
            M21 = m21; M22 = m22; M23 = m23; M24 = m24;
            M31 = m31; M32 = m32; M33 = m33; M34 = m34;
            M41 = m41; M42 = m42; M43 = m43; M44 = m44;
        }

        public static Matrix4x4 CreateRotation(Vector3 forward, Vector3 up)
        {
            forward = forward.Normalized;
            up = up.Normalized;
            var right = Vector3.Cross(up, forward).Normalized;
            up = Vector3.Cross(forward, right);
            return new Matrix4x4(
                right.X, up.X, forward.X, 0f,
                right.Y, up.Y, forward.Y, 0f,
                right.Z, up.Z, forward.Z, 0f,
                0f, 0f, 0f, 1f
            );
        }

        public Vector3 GetForward() => new(M31, M32, M33);
        public Vector3 GetUp() => new(M21, M22, M23);
        public Vector3 GetRight() => new(M11, M12, M13);

        public override string ToString() =>
            $"[{M11}, {M12}, {M13}, {M14}; {M21}, {M22}, {M23}, {M24}; {M31}, {M32}, {M33}, {M34}; {M41}, {M42}, {M43}, {M44}]";
    }
}
