using System;
using System.Numerics;

namespace mycoolfin.TheSimsulator
{
    public static class QuaternionHelper
    {
        /// <summary>
        /// Returns a quaternion that rotates <paramref name="radians"/> around
        /// <paramref name="axis"/> using the right-hand rule.
        /// </summary>
        public static Quaternion AngleAxis(float radians, Vector3 axis)
        {
            Vector3 n = Vector3.Normalize(axis);
            float halfRad = radians * 0.5f;

            float s = MathF.Sin(halfRad);
            float c = MathF.Cos(halfRad);

            return new Quaternion(n * s, c);   // (x,y,z,w)
        }

        /// <summary>
        /// Creates a rotation that points the <c>+Z</c> axis along
        /// <paramref name="forward"/> and the <c>+Y</c> axis as close as possible to
        /// <paramref name="up"/>.  All inputs are assumed to be in right-handed space.
        /// </summary>
        public static Quaternion LookRotation(Vector3 forward, Vector3 up)
        {
            Vector3 f = Vector3.Normalize(forward);
            Vector3 r = Vector3.Normalize(Vector3.Cross(up, f));   // U × F
            Vector3 u = Vector3.Cross(f, r);                       // re-orthogonalise

            var m = new Matrix4x4(
                r.X, r.Y, r.Z, 0f,
                u.X, u.Y, u.Z, 0f,
                f.X, f.Y, f.Z, 0f,
                0f, 0f, 0f, 1f);

            return Quaternion.CreateFromRotationMatrix(m);
        }
    }
}
