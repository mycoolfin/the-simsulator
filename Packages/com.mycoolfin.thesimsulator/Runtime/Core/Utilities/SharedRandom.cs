using System;
using System.Threading;

namespace mycoolfin.TheSimsulator
{
    /// <summary>
    /// A lightweight replacement for <c>System.Random.Shared</c> that works on older C#
    /// versions.  Each thread gets its own <see cref="Random"/> instance, seeded in a
    /// deterministic (or non-deterministic) way, yet all threads share the same public
    /// static façade.
    /// 
    /// <para>
    /// Typical use:
    /// <code>
    /// SharedRandom.Seed(1234);              // Optional – makes results reproducible.
    /// int n = SharedRandom.Next(10);        // [0, 9].
    /// double d = SharedRandom.NextDouble(); // [0.0, 1.0).
    /// </code>
    /// </para>
    /// </summary>
    public static class SharedRandom
    {
        /// <summary>
        /// The seed supplied by the caller (or a time-based default).
        /// </summary>
        private static int _baseSeed = Environment.TickCount;

        /// <summary>
        /// Incremented for every thread that requests a <see cref="Random"/>.
        /// </summary>
        private static int _nextThreadId;

        // Lazy container so the underlying ThreadLocal<Random> is built only when first used.
        private static Lazy<ThreadLocal<Random>> _rng = new(
            () => new ThreadLocal<Random>(
                () => new Random(DeterministicThreadSeed())),
                LazyThreadSafetyMode.ExecutionAndPublication
            );

        /// <summary>
        /// Sets a deterministic base seed for the entire run.
        /// Must be called <b>before</b> the first use of <see cref="Instance"/>
        /// or any <c>Next*</c> method.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if seeding is attempted after the generator has been used.
        /// </exception>
        public static void Seed(int seed)
        {
            if (_rng.IsValueCreated)
                throw new InvalidOperationException(
                    "SharedRandom has already been used; reseeding is not allowed.");

            _baseSeed = seed;
        }

        /// <summary>
        /// Resets the generator.
        /// </summary>
        public static void Reset()
        {
            if (_rng.IsValueCreated)
                _rng.Value.Dispose();

            _rng = new Lazy<ThreadLocal<Random>>(
                () => new ThreadLocal<Random>(
                          () => new Random(DeterministicThreadSeed())),
                LazyThreadSafetyMode.ExecutionAndPublication);

            _baseSeed = Environment.TickCount;
            _nextThreadId = 0;
        }

        /// <summary>
        /// Gets the <see cref="Random"/> instance scoped to the current thread.
        /// </summary>
        public static Random Instance => _rng.Value.Value!; // Never null.

        /// <inheritdoc cref="Random.Next()"/>
        public static int Next() => Instance.Next();

        /// <inheritdoc cref="Random.Next(int)"/>
        public static int Next(int maxValue) => Instance.Next(maxValue);

        /// <inheritdoc cref="Random.Next(int,int)"/>
        public static int Next(int minValue, int maxValue) =>
            Instance.Next(minValue, maxValue);

        /// <inheritdoc cref="Random.NextDouble"/>
        public static double NextDouble() => Instance.NextDouble();

        /// <inheritdoc cref="Random.NextBytes(byte[])"/>
        public static void NextBytes(byte[] buffer) => Instance.NextBytes(buffer);

        /// <summary>
        /// Produces a decorrelated per-thread seed based on the base seed and an
        /// ever-increasing thread index, using one round of FNV-1a hashing.
        /// </summary>
        private static int DeterministicThreadSeed()
        {
            int threadIndex = Interlocked.Increment(ref _nextThreadId);

            unchecked
            {
                uint hash = 2166136261u;
                hash ^= (uint)_baseSeed;
                hash *= 16777619u;
                hash ^= (uint)threadIndex;
                hash *= 16777619u;
                return (int)hash; // 32-bit wrap-around intentional
            }
        }

        /// <summary>
        /// Draws a random number from a Poisson distribution with the given lambda.
        /// </summary>
        /// <param name="lambda">The rate parameter of the Poisson distribution.</param>
        /// <returns>A random integer drawn from the Poisson distribution.</returns>
        public static int DrawPoisson(double lambda)
        {
            var L = Math.Exp(-lambda);
            int k = 0;
            double p = 1.0;
            do { k++; p *= NextDouble(); } while (p > L);
            return k - 1;
        }


        /// <summary>
        /// Draws a random number from a Gaussian (normal) distribution with the given standard deviation.
        /// </summary>
        /// <param name="sigma">The standard deviation of the Gaussian distribution.</param>
        /// <returns>A random float drawn from the Gaussian distribution.</returns>
        public static float DrawGaussian(float sigma)
        {
            double u, v, s;
            do
            {
                u = 2.0 * NextDouble() - 1.0;
                v = 2.0 * NextDouble() - 1.0;
                s = u * u + v * v;
            }
            while (s >= 1.0 || s == 0.0);

            double mul = Math.Sqrt(-2.0 * Math.Log(s) / s);
            double z = u * mul;

            return (float)(sigma * z);
        }

        // Dispose the ThreadLocal when the process ends to silence analyzers.
        static SharedRandom()
        {
            AppDomain.CurrentDomain.ProcessExit += (_, __) => _rng.Value.Dispose();
        }
    }

}
