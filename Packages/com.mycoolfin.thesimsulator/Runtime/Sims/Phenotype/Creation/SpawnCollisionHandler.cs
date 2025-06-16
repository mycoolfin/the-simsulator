using System;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public static class SpawnCollisionHandler
    {
        // Returns a list of limbs that collide with the new limb being considered.
        public static List<Limb> GetLimbCollisions(Limb newLimb, List<Limb> existingLimbs)
        {
            List<Limb> collisions = new();
            foreach (var limb in existingLimbs)
            {
                if (ObbOverlap(newLimb, limb))
                    collisions.Add(limb);
            }
            return collisions;
        }

        // Returns true if the OBBs of two limbs overlap in world space (using SAT).
        private static bool ObbOverlap(Limb a, Limb b)
        {
            // Get OBB data for both limbs.
            var (centerA, axesA, halfA) = GetObbData(a);
            var (centerB, axesB, halfB) = GetObbData(b);

            // 15 axes to test: 3 from A, 3 from B, 9 cross products.
            Vector3[] testAxes = new Vector3[15];
            for (int i = 0; i < 3; i++) testAxes[i] = axesA[i];
            for (int i = 0; i < 3; i++) testAxes[3 + i] = axesB[i];
            int idx = 6;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    testAxes[idx] = Vector3.Cross(axesA[i], axesB[j]);
                    idx++;
                }
            // SAT: for each axis, project both boxes and check for overlap.
            for (int i = 0; i < 15; i++)
            {
                Vector3 axis = testAxes[i];
                float lenSq = axis.X * axis.X + axis.Y * axis.Y + axis.Z * axis.Z;
                if (lenSq < 1e-8f) continue; // Skip degenerate axes.
                axis /= (float)Math.Sqrt(lenSq);
                ProjectObb(centerA, axesA, halfA, axis, out float minA, out float maxA);
                ProjectObb(centerB, axesB, halfB, axis, out float minB, out float maxB);
                if (maxA < minB || maxB < minA)
                    return false; // Separating axis found.
            }
            return true; // No separating axis, boxes overlap.
        }

        // Gets OBB data: center, axes (X,Y,Z), and half-extents in world space.
        private static (Vector3 center, Vector3[] axes, Vector3 halfExtents) GetObbData(Limb limb)
        {
            Vector3 center = limb.Position;
            Vector3[] axes = new Vector3[3];
            axes[0] = limb.Rotation * new Vector3(1, 0, 0); // X axis.
            axes[1] = limb.Rotation * new Vector3(0, 1, 0); // Y axis.
            axes[2] = limb.Rotation * new Vector3(0, 0, 1); // Z axis.
            Vector3 half = limb.Dimensions * 0.5f;
            return (center, axes, half);
        }

        // Projects an OBB onto an axis, returns min/max scalar values.
        private static void ProjectObb(Vector3 center, Vector3[] axes, Vector3 half, Vector3 axis, out float min, out float max)
        {
            // Project center
            float c = Vector3.Dot(center, axis);
            // Project half-extents
            float r = Math.Abs(Vector3.Dot(axes[0], axis)) * half.X
                    + Math.Abs(Vector3.Dot(axes[1], axis)) * half.Y
                    + Math.Abs(Vector3.Dot(axes[2], axis)) * half.Z;
            min = c - r;
            max = c + r;
        }
    }
}