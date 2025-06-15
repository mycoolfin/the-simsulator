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
    }
}
