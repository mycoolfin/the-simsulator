namespace mycoolfin.TheSimsulator.UnityIntegration.Evolution
{
    using Sims.Genotype;
    using Sims.Phenotype;
    using Sims.Evolution;

    public class SimsEvolutionSimulator : EvolutionSimulatorBase<
        SimsGenotype,
        SimsPhenotype,
        SimsEvolution<AssessableCreature<SimsGenotype, SimsPhenotype>>,
        SimsEvolutionConfig,
        ECS.API.SimsPhenotypeEntityManagement
    >
    {
        public SimsEvolutionSimulator()
            : base((config, assessPhenotypesDelegate) => new SimsEvolution<AssessableCreature<SimsGenotype, SimsPhenotype>>(config, assessPhenotypesDelegate))
        { }
    }
}
