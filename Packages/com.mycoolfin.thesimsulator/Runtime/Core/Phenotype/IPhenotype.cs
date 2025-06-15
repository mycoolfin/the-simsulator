using System;

namespace mycoolfin.TheSimsulator
{
    public interface IPhenotype<T> : IDisposable where T : IPhenotype<T>
    {
    }
}
