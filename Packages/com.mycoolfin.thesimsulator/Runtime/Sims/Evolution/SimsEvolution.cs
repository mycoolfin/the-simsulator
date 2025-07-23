namespace mycoolfin.TheSimsulator.Sims.Evolution
{
    using Core.Evolution;
    using Genotype;
    using Phenotype;

    public class SimsEvolutionConfig : EvolutionConfigBase
    {
    }

    public class SimsEvolution : EvolutionBase<SimsEvolutionConfig, SimsGenotype, SimsPhenotype>
    {
        public SimsEvolution(
            SimsEvolutionConfig config,
            AssessPhenotypesDelegate assessPhenotypesDelegate
        ) : base(config, assessPhenotypesDelegate, new SimsGenotypeFactory(), new SimsPhenotypeFactory())
        {
        }
    }
}
