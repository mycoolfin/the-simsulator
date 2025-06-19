// Homemade structs for predictable memory layout across runtimes.

using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public readonly struct Quaternion
    {
        [FieldOffset(0)] public readonly float X;
        [FieldOffset(4)] public readonly float Y;
        [FieldOffset(8)] public readonly float Z;
        [FieldOffset(12)] public readonly float W;

        public static readonly Quaternion Identity = new(0f, 0f, 0f, 1f);

        public Quaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public float Magnitude => (float)System.Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
        public float SqrMagnitude => X * X + Y * Y + Z * Z + W * W;
        public Quaternion Normalized => this / Magnitude;

        public static float Dot(Quaternion a, Quaternion b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
        public static Quaternion Lerp(Quaternion a, Quaternion b, float t)
        {
            // Simple lerp, not normalized
            return new Quaternion(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t,
                a.W + (b.W - a.W) * t
            ).Normalized;
        }
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
        {
            // Unity-like Slerp implementation
            float dot = Dot(a, b);
            if (dot < 0.0f)
            {
                b = new Quaternion(-b.X, -b.Y, -b.Z, -b.W);
                dot = -dot;
            }
            const float DOT_THRESHOLD = 0.9995f;
            if (dot > DOT_THRESHOLD)
            {
                // If the inputs are too close for comfort, linearly interpolate and normalize the result.
                return Lerp(a, b, t);
            }
            float theta_0 = (float)System.Math.Acos(dot);
            float theta = theta_0 * t;
            float sin_theta = (float)System.Math.Sin(theta);
            float sin_theta_0 = (float)System.Math.Sin(theta_0);
            float s0 = (float)System.Math.Cos(theta) - dot * sin_theta / sin_theta_0;
            float s1 = sin_theta / sin_theta_0;
            return new Quaternion(
                (a.X * s0) + (b.X * s1),
                (a.Y * s0) + (b.Y * s1),
                (a.Z * s0) + (b.Z * s1),
                (a.W * s0) + (b.W * s1)
            );
        }
        public static Quaternion operator *(Quaternion a, Quaternion b)
        {
            return new Quaternion(
                a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
                a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
                a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
                a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z
            );
        }
        public static Quaternion operator *(Quaternion q, float d) => new(q.X * d, q.Y * d, q.Z * d, q.W * d);
        public static Quaternion operator /(Quaternion q, float d) => new(q.X / d, q.Y / d, q.Z / d, q.W / d);
        public static Quaternion operator -(Quaternion q) => new(-q.X, -q.Y, -q.Z, -q.W);

        public static Quaternion LookRotation(Vector3 forward, Vector3 up)
        {
            var rotMatrix = Matrix4x4.CreateRotation(forward, up);
            return FromMatrix(rotMatrix);
        }

        public static Quaternion FromMatrix(Matrix4x4 m)
        {
            float trace = m.M11 + m.M22 + m.M33;
            float qw, qx, qy, qz;
            if (trace > 0f)
            {
                float s = (float)System.Math.Sqrt(trace + 1f) * 2f;
                qw = 0.25f * s;
                qx = (m.M32 - m.M23) / s;
                qy = (m.M13 - m.M31) / s;
                qz = (m.M21 - m.M12) / s;
            }
            else if ((m.M11 > m.M22) && (m.M11 > m.M33))
            {
                float s = (float)System.Math.Sqrt(1f + m.M11 - m.M22 - m.M33) * 2f;
                qw = (m.M32 - m.M23) / s;
                qx = 0.25f * s;
                qy = (m.M12 + m.M21) / s;
                qz = (m.M13 + m.M31) / s;
            }
            else if (m.M22 > m.M33)
            {
                float s = (float)System.Math.Sqrt(1f + m.M22 - m.M11 - m.M33) * 2f;
                qw = (m.M13 - m.M31) / s;
                qx = (m.M12 + m.M21) / s;
                qy = 0.25f * s;
                qz = (m.M23 + m.M32) / s;
            }
            else
            {
                float s = (float)System.Math.Sqrt(1f + m.M33 - m.M11 - m.M22) * 2f;
                qw = (m.M21 - m.M12) / s;
                qx = (m.M13 + m.M31) / s;
                qy = (m.M23 + m.M32) / s;
                qz = 0.25f * s;
            }
            return new Quaternion(qx, qy, qz, qw);
        }

        public static Quaternion Euler(float xDegrees, float yDegrees, float zDegrees)
        {
            // Convert degrees to radians
            float x = xDegrees * (float)System.Math.PI / 180f;
            float y = yDegrees * (float)System.Math.PI / 180f;
            float z = zDegrees * (float)System.Math.PI / 180f;
            float cx = (float)System.Math.Cos(x * 0.5f);
            float sx = (float)System.Math.Sin(x * 0.5f);
            float cy = (float)System.Math.Cos(y * 0.5f);
            float sy = (float)System.Math.Sin(y * 0.5f);
            float cz = (float)System.Math.Cos(z * 0.5f);
            float sz = (float)System.Math.Sin(z * 0.5f);
            return new Quaternion(
                sx * cy * cz - cx * sy * sz,
                cx * sy * cz + sx * cy * sz,
                cx * cy * sz - sx * sy * cz,
                cx * cy * cz + sx * sy * sz
            );
        }

        public static Quaternion FromToRotation(Vector3 from, Vector3 to)
        {
            Vector3 f = from.Normalized;
            Vector3 t = to.Normalized;
            float dot = Vector3.Dot(f, t);
            if (dot > 0.999999f)
                return Identity;
            if (dot < -0.999999f)
            {
                // 180 degree rotation around any orthogonal axis.
                Vector3 ortho = System.Math.Abs(f.X) > System.Math.Abs(f.Z)
                    ? new Vector3(-f.Y, f.X, 0f)
                    : new Vector3(0f, -f.Z, f.Y);
                ortho = ortho.Normalized;
                return Euler(ortho.X * 180f, ortho.Y * 180f, ortho.Z * 180f);
            }
            Vector3 axis = Vector3.Cross(f, t);
            float s = (float)System.Math.Sqrt((1f + dot) * 2f);
            float invs = 1f / s;
            return new Quaternion(axis.X * invs, axis.Y * invs, axis.Z * invs, s * 0.5f).Normalized;
        }

        public static Vector3 operator *(Quaternion rotation, Vector3 point)
        {
            // Unity-style: rotate a vector by a quaternion
            // result = q * v * q^-1
            float x = rotation.X, y = rotation.Y, z = rotation.Z, w = rotation.W;
            float num = x * 2f;
            float num2 = y * 2f;
            float num3 = z * 2f;
            float num4 = x * num;
            float num5 = y * num2;
            float num6 = z * num3;
            float num7 = x * num2;
            float num8 = x * num3;
            float num9 = y * num3;
            float num10 = w * num;
            float num11 = w * num2;
            float num12 = w * num3;
            return new Vector3(
                (1f - (num5 + num6)) * point.X + (num7 - num12) * point.Y + (num8 + num11) * point.Z,
                (num7 + num12) * point.X + (1f - (num4 + num6)) * point.Y + (num9 - num10) * point.Z,
                (num8 - num11) * point.X + (num9 + num10) * point.Y + (1f - (num4 + num5)) * point.Z
            );
        }

        public static Quaternion Inverse(Quaternion q)
        {
            // For unit quaternions, inverse is just the conjugate
            return new Quaternion(-q.X, -q.Y, -q.Z, q.W);
        }

        public override string ToString() => $"({X}, {Y}, {Z}, {W})";
    }
}
