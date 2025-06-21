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

        public static Matrix4x4 TRS(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            // Create rotation matrix from quaternion
            float x = rotation.X, y = rotation.Y, z = rotation.Z, w = rotation.W;
            float x2 = x * 2f, y2 = y * 2f, z2 = z * 2f;
            float xx = x * x2, yy = y * y2, zz = z * z2;
            float xy = x * y2, xz = x * z2, yz = y * z2;
            float wx = w * x2, wy = w * y2, wz = w * z2;

            // Combine rotation and scale, then add translation
            return new Matrix4x4(
                (1f - (yy + zz)) * scale.X, (xy - wz) * scale.Y, (xz + wy) * scale.Z, translation.X,
                (xy + wz) * scale.X, (1f - (xx + zz)) * scale.Y, (yz - wx) * scale.Z, translation.Y,
                (xz - wy) * scale.X, (yz + wx) * scale.Y, (1f - (xx + yy)) * scale.Z, translation.Z,
                0f, 0f, 0f, 1f
            );
        }

        public static Matrix4x4 CreateTranslation(Vector3 translation)
        {
            return new Matrix4x4(
                1f, 0f, 0f, translation.X,
                0f, 1f, 0f, translation.Y,
                0f, 0f, 1f, translation.Z,
                0f, 0f, 0f, 1f
            );
        }

        public static Matrix4x4 CreateRotation(float xDegrees, float yDegrees, float zDegrees)
        {
            // Create rotation matrices for each axis.
            Matrix4x4 rotX = CreateRotationX(xDegrees);
            Matrix4x4 rotY = CreateRotationY(yDegrees);
            Matrix4x4 rotZ = CreateRotationZ(zDegrees);

            // Combine rotations (left-hand coordinate system: ZYX order)
            return rotX * rotY * rotZ;
        }

        public static Matrix4x4 CreateRotationX(float angleDegrees)
        {
            float rad = angleDegrees * (float)System.Math.PI / 180f;
            float cos = (float)System.Math.Cos(rad);
            float sin = (float)System.Math.Sin(rad);
            return new Matrix4x4(
                1f, 0f, 0f, 0f,
                0f, cos, sin, 0f,
                0f, -sin, cos, 0f,
                0f, 0f, 0f, 1f
            );
        }

        public static Matrix4x4 CreateRotationY(float angleDegrees)
        {
            float rad = angleDegrees * (float)System.Math.PI / 180f;
            float cos = (float)System.Math.Cos(rad);
            float sin = (float)System.Math.Sin(rad);
            return new Matrix4x4(
                cos, 0f, -sin, 0f,
                0f, 1f, 0f, 0f,
                sin, 0f, cos, 0f,
                0f, 0f, 0f, 1f
            );
        }

        public static Matrix4x4 CreateRotationZ(float angleDegrees)
        {
            float rad = angleDegrees * (float)System.Math.PI / 180f;
            float cos = (float)System.Math.Cos(rad);
            float sin = (float)System.Math.Sin(rad);
            return new Matrix4x4(
                cos, sin, 0f, 0f,
                -sin, cos, 0f, 0f,
                0f, 0f, 1f, 0f,
                0f, 0f, 0f, 1f
            );
        }

        public static Matrix4x4 CreateLookAt(Vector3 eye, Vector3 target, Vector3 up)
        {
            Vector3 forward = (target - eye).Normalized;
            Vector3 right = Vector3.Cross(forward, up).Normalized;
            up = Vector3.Cross(right, forward);
            
            return new Matrix4x4(
                right.X, up.X, forward.X, -Vector3.Dot(right, eye),
                right.Y, up.Y, forward.Y, -Vector3.Dot(up, eye),
                right.Z, up.Z, forward.Z, -Vector3.Dot(forward, eye),
                0f, 0f, 0f, 1f
            );
        }

        public static Matrix4x4 CreateScale(Vector3 scale)
        {
            return new Matrix4x4(
                scale.X, 0f, 0f, 0f,
                0f, scale.Y, 0f, 0f,
                0f, 0f, scale.Z, 0f,
                0f, 0f, 0f, 1f
            );
        }

        public Vector3 GetTranslation()
        {
            return new Vector3(M14, M24, M34);
        }

        public Quaternion GetRotation()
        {
            // Extract scale to remove it from rotation calculation
            var scale = GetScale();
            var m11 = M11 / scale.X;
            var m12 = M12 / scale.Y;
            var m13 = M13 / scale.Z;
            var m21 = M21 / scale.X;
            var m22 = M22 / scale.Y;
            var m23 = M23 / scale.Z;
            var m31 = M31 / scale.X;
            var m32 = M32 / scale.Y;
            var m33 = M33 / scale.Z;

            // Create normalized rotation matrix and extract quaternion
            var rotMatrix = new Matrix4x4(
                m11, m12, m13, 0f,
                m21, m22, m23, 0f,
                m31, m32, m33, 0f,
                0f, 0f, 0f, 1f
            );
            return Quaternion.FromMatrix(rotMatrix);
        }

        public Vector3 GetScale()
        {
            var scaleX = new Vector3(M11, M21, M31).Magnitude;
            var scaleY = new Vector3(M12, M22, M32).Magnitude;
            var scaleZ = new Vector3(M13, M23, M33).Magnitude;

            // Check for negative scaling by determinant
            var det = M11 * (M22 * M33 - M23 * M32) - M12 * (M21 * M33 - M23 * M31) + M13 * (M21 * M32 - M22 * M31);
            if (det < 0f)
                scaleX = -scaleX;

            return new Vector3(scaleX, scaleY, scaleZ);
        }

        public Matrix4x4 Translate(Vector3 translation)
        {
            return this * CreateTranslation(translation);
        }

        public Matrix4x4 Rotate(Quaternion rotation)
        {
            var rotMatrix = TRS(Vector3.Zero, rotation, Vector3.One);
            return this * rotMatrix;
        }

        public Matrix4x4 Scale(Vector3 scale)
        {
            return this * CreateScale(scale);
        }

        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
        {
            return new Matrix4x4(
                a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41,
                a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42,
                a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43,
                a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44,

                a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41,
                a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42,
                a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43,
                a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44,

                a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41,
                a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42,
                a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43,
                a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44,

                a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41,
                a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42,
                a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43,
                a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44
            );
        }

        public Vector3 GetForward() => new(M31, M32, M33);
        public Vector3 GetUp() => new(M21, M22, M23);
        public Vector3 GetRight() => new(M11, M12, M13);

        public override string ToString() =>
            $"[{M11}, {M12}, {M13}, {M14}; {M21}, {M22}, {M23}, {M24}; {M31}, {M32}, {M33}, {M34}; {M41}, {M42}, {M43}, {M44}]";
    }
}
