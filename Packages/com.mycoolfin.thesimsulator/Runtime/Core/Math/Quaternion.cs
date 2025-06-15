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
    }
}
