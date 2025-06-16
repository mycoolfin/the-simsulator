// Homemade structs for predictable memory layout across runtimes.

using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly struct Vector2
    {
        [FieldOffset(0)] public readonly float X;
        [FieldOffset(4)] public readonly float Y;

        public static readonly Vector2 Zero = new(0f, 0f);
        public static readonly Vector2 One = new(1f, 1f);

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float Magnitude => (float)System.Math.Sqrt(X * X + Y * Y);
        public float SqrMagnitude => X * X + Y * Y;
        public Vector2 Normalized => this / Magnitude;

        public static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;
        public static float Distance(Vector2 a, Vector2 b) => (a - b).Magnitude;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a + (b - a) * t;
        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator *(Vector2 a, float d) => new(a.X * d, a.Y * d);
        public static Vector2 operator *(float d, Vector2 a) => new(a.X * d, a.Y * d);
        public static Vector2 operator /(Vector2 a, float d) => new(a.X / d, a.Y / d);
        public static Vector2 operator -(Vector2 a) => new(-a.X, -a.Y);
        public static Vector2 Scale(Vector2 a, Vector2 b) => new(a.X * b.X, a.Y * b.Y);
    }

    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public readonly struct Vector3
    {
        [FieldOffset(0)] public readonly float X;
        [FieldOffset(4)] public readonly float Y;
        [FieldOffset(8)] public readonly float Z;

        public static readonly Vector3 Zero = new(0f, 0f, 0f);
        public static readonly Vector3 One = new(1f, 1f, 1f);

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float Magnitude => (float)System.Math.Sqrt(X * X + Y * Y + Z * Z);
        public float SqrMagnitude => X * X + Y * Y + Z * Z;
        public Vector3 Normalized => this / Magnitude;

        public static Vector3 Cross(Vector3 a, Vector3 b) => new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );
        public static float Dot(Vector3 a, Vector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static float Distance(Vector3 a, Vector3 b) => (a - b).Magnitude;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * t;
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator *(Vector3 a, float d) => new(a.X * d, a.Y * d, a.Z * d);
        public static Vector3 operator *(float d, Vector3 a) => new(a.X * d, a.Y * d, a.Z * d);
        public static Vector3 operator /(Vector3 a, float d) => new(a.X / d, a.Y / d, a.Z / d);
        public static Vector3 operator -(Vector3 a) => new(-a.X, -a.Y, -a.Z);
        public static Vector3 Scale(Vector3 a, Vector3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public readonly struct Vector4
    {
        [FieldOffset(0)] public readonly float X;
        [FieldOffset(4)] public readonly float Y;
        [FieldOffset(8)] public readonly float Z;
        [FieldOffset(12)] public readonly float W;

        public static readonly Vector4 Zero = new(0f, 0f, 0f, 0f);
        public static readonly Vector4 One = new(1f, 1f, 1f, 1f);

        public Vector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public float Magnitude => (float)System.Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
        public float SqrMagnitude => X * X + Y * Y + Z * Z + W * W;
        public Vector4 Normalized => this / Magnitude;

        public static float Dot(Vector4 a, Vector4 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
        public static float Distance(Vector4 a, Vector4 b) => (a - b).Magnitude;
        public static Vector4 Lerp(Vector4 a, Vector4 b, float t) => a + (b - a) * t;
        public static Vector4 operator +(Vector4 a, Vector4 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
        public static Vector4 operator -(Vector4 a, Vector4 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
        public static Vector4 operator *(Vector4 a, float d) => new(a.X * d, a.Y * d, a.Z * d, a.W * d);
        public static Vector4 operator *(float d, Vector4 a) => new(a.X * d, a.Y * d, a.Z * d, a.W * d);
        public static Vector4 operator /(Vector4 a, float d) => new(a.X / d, a.Y / d, a.Z / d, a.W / d);
        public static Vector4 operator -(Vector4 a) => new(-a.X, -a.Y, -a.Z, -a.W);
        public static Vector4 Scale(Vector4 a, Vector4 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);
    }
}
