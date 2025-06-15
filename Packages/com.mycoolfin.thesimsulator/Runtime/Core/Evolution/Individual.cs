namespace mycoolfin.TheSimsulator
{
    public class Individual<TGenotype, TPhenotype>
        where TGenotype : IGenotype<TGenotype>
        where TPhenotype : IPhenotype<TPhenotype>
    {
        public TGenotype genotype;
        public TPhenotype phenotype;
        public float fitness;
    }
}
