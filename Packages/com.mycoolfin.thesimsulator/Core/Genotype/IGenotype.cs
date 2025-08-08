namespace mycoolfin.TheSimsulator.Core.Genotype
{
    public interface IGenotype<T> where T : IGenotype<T>
    {
        string Name { get; set; }
    }
}
