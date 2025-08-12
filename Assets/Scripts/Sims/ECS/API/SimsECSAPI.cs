namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.API
{
    using TheSimsulator.Sims.Phenotype;
    using UnityIntegration.Core.ECS.API;

    public class SimsECSAPI : IECSAPI<SimsPhenotype>
    {
        public IWorldManagement World { get; private set; }
        public ISimulationSettings Simulation { get; private set; }
        public IPresentationSettings Presentation { get; private set; }
        public IPhenotypeEntityManagement<SimsPhenotype> Phenotype { get; private set; }
        public IEvolutionManagement Evolution { get; private set; }

        public SimsECSAPI()
        {
            World = new WorldManagement();
            Phenotype = new SimsPhenotypeEntityManagement();
            SimsSimulationSettings simulation = new();
            Simulation = simulation;
            Evolution = new SimsEvolutionManagement(simulation);
            Presentation = new SimsPresentationSettings();
        }
    }
}
