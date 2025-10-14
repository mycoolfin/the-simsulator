namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.Math
{
    public static class IntExtensions
    {
        public static int NextPowerOfTwo(this int value)
        {
            if (value <= 0) return 1;
            if (value == 1) return 2;

            // Find the next power of 2.
            int power = 1;
            while (power < value)
                power <<= 1;

            return power;
        }
    }
}
