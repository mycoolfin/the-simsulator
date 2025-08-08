namespace mycoolfin.TheSimsulator.UnityIntegration.Evolution
{
    using Core.Genotype;
    using Core.Phenotype;
    using Core.Evolution;

    public interface IAssessableCreature : ICreature
    {
        float Fitness { get; }
        bool IsProtected { get; }

        void Protect(bool protect);
    }

    public class AssessableCreature<TGenotype, TPhenotype> : Creature<TGenotype, TPhenotype>, IAssessableCreature, IIndividual<TGenotype, TPhenotype>
        where TGenotype : IGenotype<TGenotype>
        where TPhenotype : IPhenotype<TPhenotype>
    {
        public float Fitness { get; set; } = 0f;

        public bool IsProtected { get; private set; } = false;

        public void Protect(bool protect)
        {
            IsProtected = protect;
        }
    }
}
