namespace mycoolfin.TheSimsulator.Sims.Evolution
{
    using Core.Evolution;
    using Genotype;
    using Phenotype;

    public class SimsEvolutionConfig : EvolutionConfigBase
    {
    }

    public class SimsEvolution<TIndividual> : EvolutionBase<SimsEvolutionConfig, SimsGenotype, SimsPhenotype, TIndividual>
        where TIndividual : IIndividual<SimsGenotype, SimsPhenotype>, new()
    {
        public SimsEvolution(SimsEvolutionConfig config, AssessIndividualsDelegate assessPhenotypesDelegate)
        : base(config, assessPhenotypesDelegate, new SimsGenotypeFactory(), new SimsPhenotypeFactory())
        { }
    }
}
