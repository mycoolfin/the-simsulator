using System;
using Unity.Entities;
using UnityEngine;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core
{
    using TheSimsulator.Core.Genotype;
    using TheSimsulator.Core.Phenotype;
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
        string Name { get; set; }

        delegate void OnCullDelegate();
        OnCullDelegate OnCull { get; set; }
        void Cull();

        void SaveGenotypeToFile(Action<FileOperationResult, string> OnComplete, string filePath = null);
        void SavePhenotypeModelToFile(World world, Func<Mesh> getBaseMesh, float scaleFactor, Action<FileOperationResult, string> OnComplete, string filePath = null);

        ICreature Breed(ICreature other);
    }

    public class Creature<TGenotype, TPhenotype> : ICreature
        where TGenotype : IGenotype<TGenotype>
        where TPhenotype : IPhenotype<TPhenotype>
    {
        public TGenotype Genotype { get; set; }
        public TPhenotype Phenotype { get; set; }

        public string Name { get => Genotype.Name; set { Genotype.Name = value; } }

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

        public void SaveGenotypeToFile(Action<FileOperationResult, string> OnComplete, string filePath = null)
        {
            GenotypeDiskOperations.SaveGenotypeToFilePath(Genotype, (result, fp) => OnComplete(result, fp), filePath);
        }

        public void SavePhenotypeModelToFile(World world, Func<Mesh> getBaseMesh, float scaleFactor, Action<FileOperationResult, string> OnComplete, string filePath = null)
        {
            PhenotypeMeshExporter.SavePhenotypeModelToFilePath(Genotype.Name, world, getBaseMesh, Phenotype, scaleFactor, (result, fp) => OnComplete(result, fp), filePath);
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

        public void Select()
        {
            // TODO
        }
    }
}
