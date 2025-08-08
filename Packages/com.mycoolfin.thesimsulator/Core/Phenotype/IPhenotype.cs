using System;

namespace mycoolfin.TheSimsulator.Core.Phenotype
{
    public interface IPhenotype<T> : IDisposable where T : IPhenotype<T>
    {
        public ulong Gid { get; }
    }
}
