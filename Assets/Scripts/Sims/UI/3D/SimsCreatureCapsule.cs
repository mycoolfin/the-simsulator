namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.UI.ThreeD
{
    using TheSimsulator.Sims.Genotype;
    using TheSimsulator.Sims.Phenotype;
    using Core.UI.ThreeD;
    using Sims.ECS.API;

    public class SimsCreatureCapsule : CreatureCapsule<SimsGenotype, SimsPhenotype, SimsPhenotypeFactory, SimsECSAPI>, ICreatureCapsule
    {
        private SimsPhenotypeFactory phenotypeFactory;
        protected override SimsPhenotypeFactory PhenotypeFactory => phenotypeFactory;
        private SimsECSAPI ecsAPI;
        protected override SimsECSAPI ECSAPI => ecsAPI;

        private void Awake()
        {
            phenotypeFactory = new SimsPhenotypeFactory();
            ecsAPI = new SimsECSAPI();
        }
    }
}
