using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace mycoolfin.TheSimsulator.Sims.Genotype
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct Connection
    {
        public const int MIN_PARENT_FACE = 0;
        public const int MAX_PARENT_FACE = 5;
        public const float MIN_POSITION = -1.0f;
        public const float MAX_POSITION = 1.0f;
        public const float MIN_ORIENTATION = (float)-Math.PI / 2;
        public const float MAX_ORIENTATION = (float)Math.PI / 2;
        public const float MIN_SCALE = 0.1f;
        public const float MAX_SCALE = 2.0f;

        public readonly ulong Gid;
        public readonly ulong ParentNodeGid;
        public readonly ulong ChildNodeGid;
        public readonly int ParentFace;
        public readonly Vector2 Position; // Position on the parent limb's face in [-1, 1]x[-1, 1].
        public readonly Vector3 Orientation; // Euler angles in radians.
        public readonly Vector3 Scale;
        public readonly bool ReflectionX;
        public readonly bool ReflectionY;
        public readonly bool ReflectionZ;
        public readonly bool TerminalOnly;

        public Connection(ulong parentNodeGid, ulong childNodeGid, int parentFace, Vector2 position, Vector3 orientation, Vector3 scale, bool reflectionX, bool reflectionY, bool reflectionZ, bool terminalOnly)
        {
            Gid = SharedRandom.NextUInt64();
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
            int parentFace = RandomParentFace();
            Vector2 position = new(RandomPosition(), RandomPosition());
            Vector3 orientation = new(RandomOrientation(), RandomOrientation(), RandomOrientation());
            Vector3 scale = new(RandomScale(), RandomScale(), RandomScale());
            bool reflectionX = RandomBool();
            bool reflectionY = RandomBool();
            bool reflectionZ = RandomBool();
            bool terminalOnly = RandomBool();

            return new Connection(parentNodeId, childNodeId, parentFace, position, orientation, scale, reflectionX, reflectionY, reflectionZ, terminalOnly);
        }

        private static int RandomParentFace() => SharedRandom.Next(MIN_PARENT_FACE, MAX_PARENT_FACE + 1);
        private static float RandomPosition() => (float)SharedRandom.NextDouble() * (MAX_POSITION - MIN_POSITION) + MIN_POSITION;
        private static float RandomOrientation() => (float)SharedRandom.NextDouble() * (MAX_ORIENTATION - MIN_ORIENTATION) + MIN_ORIENTATION;
        private static float RandomScale() => (float)SharedRandom.NextDouble() * (MAX_SCALE - MIN_SCALE) + MIN_SCALE;
        private static bool RandomBool() => SharedRandom.Next(0, 2) == 0;
    }
}
