using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration
{
    using Core.Genotype;
    using Core.Phenotype;
    using UI;
    using UI.IO;

    public struct PhenotypeTransformData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Bounds Bounds;
    }

    public interface ICreature : ISelectable
    {
        string Name { get; }

        delegate void OnCullDelegate();
        OnCullDelegate OnCull { get; set; }
        void Cull();

        void SaveGenotypeToFile();

        ICreature Breed(ICreature other);
    }

    public class Creature<TGenotype, TPhenotype> : ICreature
        where TGenotype : IGenotype<TGenotype>
        where TPhenotype : IPhenotype<TPhenotype>
    {
        public TGenotype Genotype { get; set; }
        public TPhenotype Phenotype { get; set; }

        public string Name => Genotype.Name;

        public delegate PhenotypeTransformData GetPhenotypeTransformDataDelegate();
        public GetPhenotypeTransformDataDelegate GetPhenotypeTransformData { get; set; }
        public Vector3 WorldPosition => GetPhenotypeTransformData?.Invoke().Position ?? Vector3.zero;
        public Quaternion WorldRotation => GetPhenotypeTransformData?.Invoke().Rotation ?? Quaternion.identity;
        public Bounds Bounds => GetPhenotypeTransformData?.Invoke().Bounds ?? new Bounds(WorldPosition, Vector3.one);

        public ICreature.OnCullDelegate OnCull { get; set; }
        public void Cull()
        {
            OnCull?.Invoke();
        }

        public void SaveGenotypeToFile()
        {
            GenotypeDiskOperations.SaveGenotypeToFile(Genotype);
        }

        public Creature<TGenotype, TPhenotype> Breed(ICreature other)
        {
            // TODO
            return null;
        }
        ICreature ICreature.Breed(ICreature other)
        {
            return Breed(other);
        }

        public void Select(bool toggle, bool multiselect)
        {
            // TODO
        }
    }
}
