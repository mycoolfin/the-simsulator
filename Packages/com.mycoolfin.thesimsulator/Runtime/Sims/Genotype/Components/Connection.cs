using System;
using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims
{
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public readonly struct Connection
    {
        public static readonly int MinParentFace = 0;
        public static readonly int MaxParentFace = 5;
        public static readonly Vector3 MinPosition = new(-1.0f, -1.0f, -1.0f);
        public static readonly Vector3 MaxPosition = new(1.0f, 1.0f, 1.0f);
        public static readonly Vector3 MinOrientation = new(-90.0f, -90.0f, -90.0f);
        public static readonly Vector3 MaxOrientation = new(90.0f, 90.0f, 90.0f);
        public static readonly Vector3 MinScale = new(0.1f, 0.1f, 0.1f);
        public static readonly Vector3 MaxScale = new(2.0f, 2.0f, 2.0f);

        [FieldOffset(0)] public readonly ulong Gid;
        [FieldOffset(8)] public readonly ulong ParentNodeGid;
        [FieldOffset(16)] public readonly ulong ChildNodeGid;
        [FieldOffset(24)] public readonly int ParentFace;
        [FieldOffset(28)] public readonly Vector2 Position; // Position on the parent limb's face in [-1, 1]x[-1, 1].
        [FieldOffset(36)] public readonly Vector3 Orientation; // Euler angles in degrees.
        [FieldOffset(48)] public readonly Vector3 Scale;
        [FieldOffset(60)] public readonly bool ReflectionX;
        [FieldOffset(61)] public readonly bool ReflectionY;
        [FieldOffset(62)] public readonly bool ReflectionZ;
        [FieldOffset(63)] public readonly bool TerminalOnly;

        public Connection(ulong parentNodeGid, ulong childNodeGid, int parentFace, Vector2 position, Vector3 orientation, Vector3 scale, bool reflectionX, bool reflectionY, bool reflectionZ, bool terminalOnly)
        {
            byte[] buffer = new byte[sizeof(ulong)];
            SharedRandom.NextBytes(buffer);
            Gid = BitConverter.ToUInt64(buffer, 0);
            ParentNodeGid = parentNodeGid;
            ChildNodeGid = childNodeGid;
            ParentFace = parentFace;
            Position = position;
            Orientation = orientation;
            Scale = scale;
            ReflectionX = reflectionX;
            ReflectionY = reflectionY;
            ReflectionZ = reflectionZ;
            TerminalOnly = terminalOnly;
        }

        public static Connection CreateRandom(ulong parentNodeId, ulong childNodeId)
        {
            int parentFace = SharedRandom.Next(MinParentFace, MaxParentFace + 1);
            Vector2 position = new(
                (float)SharedRandom.NextDouble() * (MaxPosition.X - MinPosition.X) + MinPosition.X,
                (float)SharedRandom.NextDouble() * (MaxPosition.Y - MinPosition.Y) + MinPosition.Y
            );
            Vector3 orientation = new(
                (float)SharedRandom.NextDouble() * (MaxOrientation.X - MinOrientation.X) + MinOrientation.X,
                (float)SharedRandom.NextDouble() * (MaxOrientation.Y - MinOrientation.Y) + MinOrientation.Y,
                (float)SharedRandom.NextDouble() * (MaxOrientation.Z - MinOrientation.Z) + MinOrientation.Z
            );
            Vector3 scale = new(
                (float)SharedRandom.NextDouble() * (MaxScale.X - MinScale.X) + MinScale.X,
                (float)SharedRandom.NextDouble() * (MaxScale.Y - MinScale.Y) + MinScale.Y,
                (float)SharedRandom.NextDouble() * (MaxScale.Z - MinScale.Z) + MinScale.Z
            );
            bool reflectionX = SharedRandom.Next(0, 2) == 0;
            bool reflectionY = SharedRandom.Next(0, 2) == 0;
            bool reflectionZ = SharedRandom.Next(0, 2) == 0;
            bool terminalOnly = SharedRandom.Next(0, 2) == 0;

            return new Connection(parentNodeId, childNodeId, parentFace, position, orientation, scale, reflectionX, reflectionY, reflectionZ, terminalOnly);
        }
    }
}
