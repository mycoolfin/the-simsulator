using System.Numerics;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using mycoolfin.TheSimsulator.Sims.Phenotype;

public struct PhenotypeEntityCreationInfo
{
    public SimsPhenotype Phenotype;
    public Vector3 VisualOffset;
    public bool AllowInterPhenotypeCollisions;
}

public static class EntityCreationAPI
{
    public static void CreateEntitiesFromPhenotypes(List<PhenotypeEntityCreationInfo> creationInfoList)
    {
        World world = World.DefaultGameObjectInjectionWorld;

        EntityManager entityManager = world.EntityManager;
        EntityArchetype limbRequestArchetype = entityManager.CreateArchetype(
            typeof(LimbEntityCreationRequest)
        );
        EntityArchetype jointRequestArchetype = entityManager.CreateArchetype(
            typeof(JointEntityCreationRequest)
        );
        EntityArchetype neuralNetworkRequestArchetype = entityManager.CreateArchetype(
            typeof(RootPhenotypeEntityCreationRequest)
        );

        List<LimbEntityCreationRequest> limbRequests = new();
        List<JointEntityCreationRequest> jointRequests = new();
        List<RootPhenotypeEntityCreationRequest> neuralNetworkRequests = new();
        foreach (PhenotypeEntityCreationInfo c in creationInfoList)
        {
            limbRequests.AddRange(ConvertToLimbCreationRequests(c));
            jointRequests.AddRange(ConvertToJointCreationRequests(c));
            using BlobBuilder builder = new(Allocator.Temp);
            neuralNetworkRequests.Add(ConvertToNeuralNetworkCreationRequest(c, builder));
        }

        // Add limb entity creation requests to ECS.
        using NativeArray<Entity> entities = new(limbRequests.Count, Allocator.Temp);
        entityManager.CreateEntity(limbRequestArchetype, entities);
        for (int i = 0; i < limbRequests.Count; i++)
            entityManager.SetComponentData(entities[i], limbRequests[i]);

        // Add joint entity creation requests to ECS.
        using NativeArray<Entity> jointEntities = new(jointRequests.Count, Allocator.Temp);
        entityManager.CreateEntity(jointRequestArchetype, jointEntities);
        for (int i = 0; i < jointRequests.Count; i++)
            entityManager.SetComponentData(jointEntities[i], jointRequests[i]);

        // Add neural network entity creation requests to ECS.
        using NativeArray<Entity> neuralNetworkEntities = new(neuralNetworkRequests.Count, Allocator.Temp);
        entityManager.CreateEntity(neuralNetworkRequestArchetype, neuralNetworkEntities);
        for (int i = 0; i < neuralNetworkRequests.Count; i++)
            entityManager.SetComponentData(neuralNetworkEntities[i], neuralNetworkRequests[i]);
    }

    private static List<LimbEntityCreationRequest> ConvertToLimbCreationRequests(PhenotypeEntityCreationInfo info)
    {
        List<LimbEntityCreationRequest> requests = new();

        for (int i = 0; i < info.Phenotype.Limbs.Count; i++)
        {
            mycoolfin.TheSimsulator.Sims.Phenotype.Limb limb = info.Phenotype.Limbs[i];
            LimbEntityCreationRequest request = new()
            {
                PhenotypeGid = info.Phenotype.Gid,
                LimbIndex = i,
                Position = limb.Position.ToFloat3(),
                Rotation = limb.Rotation.ToQuaternion(),
                Dimensions = limb.Dimensions.ToFloat3(),
                Mass = limb.Mass,
                Color = limb.Color.ToFloat4(),
                VisualOffset = info.VisualOffset.ToFloat3(),
                AllowInterPhenotypeCollisions = info.AllowInterPhenotypeCollisions
            };
            requests.Add(request);
        }

        return requests;
    }

    private static List<JointEntityCreationRequest> ConvertToJointCreationRequests(PhenotypeEntityCreationInfo info)
    {
        List<JointEntityCreationRequest> requests = new();
        List<mycoolfin.TheSimsulator.Sims.Phenotype.Limb> limbs = info.Phenotype.Limbs;
        for (int i = 0; i < limbs.Count; i++)
        {
            mycoolfin.TheSimsulator.Sims.Phenotype.Limb attachedLimb = limbs[i];
            if (attachedLimb.Joint == null)
                continue;

            int referenceLimbIndex = limbs.FindIndex(l => l == attachedLimb.Joint.ParentLimb);
            if (referenceLimbIndex < 0)
                throw new System.Exception($"Invalid reference limb index: {referenceLimbIndex} for attached limb index: {i}");

            mycoolfin.TheSimsulator.Sims.Phenotype.Limb referenceLimb = limbs[referenceLimbIndex];

            JointEntityCreationRequest request = new()
            {
                PhenotypeGid = info.Phenotype.Gid,
                JointType = attachedLimb.Joint.Type,
                ReferenceLimbIndex = referenceLimbIndex,
                AttachedLimbIndex = i,
                ReferenceLimbSpaceAnchor = attachedLimb.Joint.ParentSpaceAnchor.ToFloat3(),
                ReferenceLimbSpaceXAxis = attachedLimb.Joint.ParentSpaceXAxis.ToFloat3(),
                ReferenceLimbSpaceYAxis = attachedLimb.Joint.ParentSpaceYAxis.ToFloat3(),
                ReferenceLimbSpaceZAxis = attachedLimb.Joint.ParentSpaceZAxis.ToFloat3(),
                AngleLimits = attachedLimb.Joint.AngleLimits.ToFloat3(),
                MaxMotorImpulseScaleFactor = attachedLimb.Joint.MinCrossSectionalArea
            };
            requests.Add(request);
        }

        return requests;
    }

    private static RootPhenotypeEntityCreationRequest ConvertToNeuralNetworkCreationRequest(PhenotypeEntityCreationInfo info, BlobBuilder builder)
    {
        BlobAssetReference<CompiledNeuralGraph> compiledNeuralGraph = NeuralCompiler.Compile(info.Phenotype, builder);

        return new RootPhenotypeEntityCreationRequest
        {
            PhenotypeGid = info.Phenotype.Gid,
            Graph = compiledNeuralGraph,
            LimbCount = (byte)info.Phenotype.Limbs.Count
        };
    }
}
