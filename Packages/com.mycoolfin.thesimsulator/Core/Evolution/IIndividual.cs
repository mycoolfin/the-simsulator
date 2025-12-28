namespace mycoolfin.TheSimsulator.Core.Evolution
{
    using Genotype;
    using Phenotype;

    public interface IIndividual<TGenotype, TPhenotype>
        where TGenotype : IGenotype<TGenotype>
        where TPhenotype : IPhenotype<TPhenotype>
    {
        public TGenotype Genotype { get; set; }
        public TPhenotype Phenotype { get; set; }
        public float Fitness { get; set; }
    }
}
