using System;
using System.Numerics;

namespace mycoolfin.TheSimsulator.Sims.Genotype
{
    public struct NodeColor
    {
        public const float MIN_COLOR_RECURSIVE_OFFSET = -0.1f;
        public const float MAX_COLOR_RECURSIVE_OFFSET = 0.1f;

        public float H;
        public float S;
        public float V;

        public float RecursiveOffsetH;
        public float RecursiveOffsetS;
        public float RecursiveOffsetV;

        public NodeColor(float h, float s, float v, float recursiveOffsetH, float recursiveOffsetS, float recursiveOffsetV)
        {
            H = h;
            S = s;
            V = v;
            RecursiveOffsetH = recursiveOffsetH;
            RecursiveOffsetS = recursiveOffsetS;
            RecursiveOffsetV = recursiveOffsetV;
        }

        public readonly Vector4 ToRGBA(int nodeRecursionDepth)
        {
            float hueValue = H + RecursiveOffsetH * nodeRecursionDepth;
            float adjustedH = hueValue - (float)Math.Floor(hueValue);
            float adjustedS = Math.Clamp(S + RecursiveOffsetS * nodeRecursionDepth, 0f, 1f);
            float adjustedV = Math.Clamp(V + RecursiveOffsetV * nodeRecursionDepth, 0f, 1f);

            // Convert HSV to RGB.
            float r, g, b;
            int i = (int)(adjustedH * 6);
            float f = adjustedH * 6 - i;
            float p = adjustedV * (1 - adjustedS);
            float q = adjustedV * (1 - f * adjustedS);
            float t = adjustedV * (1 - (1 - f) * adjustedS);
            i %= 6;
            switch (i)
            {
                case 0: r = adjustedV; g = t; b = p; break;
                case 1: r = q; g = adjustedV; b = p; break;
                case 2: r = p; g = adjustedV; b = t; break;
                case 3: r = p; g = q; b = adjustedV; break;
                case 4: r = t; g = p; b = adjustedV; break;
                case 5: r = adjustedV; g = p; b = q; break;
                default: r = g = b = 0; break;
            }
            return new Vector4(r, g, b, 1f);
        }

        public static NodeColor CreateRandom()
        {
            float h = (float)SharedRandom.NextDouble();
            float s = (float)SharedRandom.NextDouble();
            float v = (float)SharedRandom.NextDouble();

            float recursiveOffsetH = (float)SharedRandom.NextDouble() * (MAX_COLOR_RECURSIVE_OFFSET - MIN_COLOR_RECURSIVE_OFFSET) + MIN_COLOR_RECURSIVE_OFFSET;
            float recursiveOffsetS = (float)SharedRandom.NextDouble() * (MAX_COLOR_RECURSIVE_OFFSET - MIN_COLOR_RECURSIVE_OFFSET) + MIN_COLOR_RECURSIVE_OFFSET;
            float recursiveOffsetV = (float)SharedRandom.NextDouble() * (MAX_COLOR_RECURSIVE_OFFSET - MIN_COLOR_RECURSIVE_OFFSET) + MIN_COLOR_RECURSIVE_OFFSET;

            return new NodeColor(h, s, v, recursiveOffsetH, recursiveOffsetS, recursiveOffsetV);
        }
    }
}
