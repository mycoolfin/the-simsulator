namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.Evolution
{
    using TheSimsulator.Sims.Genotype;
    using TheSimsulator.Sims.Phenotype;
    using TheSimsulator.Sims.Evolution;
    using UnityIntegration.Core.Evolution;

    public class SimsEvolutionSimulator : EvolutionSimulatorBase<
        SimsGenotype,
        SimsPhenotype,
        SimsEvolution<AssessableCreature<SimsGenotype, SimsPhenotype>>,
        SimsEvolutionConfig,
        ECS.API.SimsECSAPI
    >
    {
        public SimsEvolutionSimulator()
            : base((config, assessPhenotypesDelegate) => new SimsEvolution<AssessableCreature<SimsGenotype, SimsPhenotype>>(config, assessPhenotypesDelegate))
        { }
    }
}
