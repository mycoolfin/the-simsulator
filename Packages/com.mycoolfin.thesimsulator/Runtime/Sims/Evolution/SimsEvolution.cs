using mycoolfin.TheSimsulator.Sims.Genotype;
using mycoolfin.TheSimsulator.Sims.Phenotype;

namespace mycoolfin.TheSimsulator.Sims.Evolution
{
    public class SimsEvolutionConfig : EvolutionConfigBase
    {
        public float AsexualProbability = 0.4f;
        public float CrossoverProbability = 0.3f;
        public float GraftingProbability = 0.3f;
        public int CrossoverInterval = 2;
    }

    public class SimsEvolution : EvolutionBase<SimsEvolutionConfig, SimsGenotype, SimsPhenotype>
    {
        public SimsEvolution(
            AssessPhenotypesDelegate assessPhenotypesDelegate,
            SimsEvolutionConfig config
        )
            : base(
                new SimsGenotypeFactory(
                    config.AsexualProbability,
                    config.CrossoverProbability,
                    config.GraftingProbability,
                    config.CrossoverInterval
                ),
                new SimsPhenotypeFactory(),
                assessPhenotypesDelegate,
                config
            )
        {
        }
    }
}
