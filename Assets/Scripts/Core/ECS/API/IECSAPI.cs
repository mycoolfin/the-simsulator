namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
    using TheSimsulator.Core.Phenotype;

    public interface IECSAPI<TPhenotype>
        where TPhenotype : IPhenotype<TPhenotype>
    {
        IWorldManagement World { get; }
        ISimulationSettings Simulation { get; }
        IPresentationSettings Presentation { get; }
        IPhenotypeEntityManagement<TPhenotype> Phenotype { get; }
        IEvolutionManagement Evolution { get; }
    }
}
