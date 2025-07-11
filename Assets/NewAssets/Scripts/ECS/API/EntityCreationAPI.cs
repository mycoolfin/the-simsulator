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
    public static void CreateEntitiesFromPhenotypes(World world, List<PhenotypeEntityCreationInfo> creationInfoList)
    {
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

        // Convert to NativeArrays for better performance and reduced allocations.
        using NativeArray<LimbEntityCreationRequest> limbRequestsArray = new(limbRequests.ToArray(), Allocator.Temp);
        using NativeArray<JointEntityCreationRequest> jointRequestsArray = new(jointRequests.ToArray(), Allocator.Temp);
        using NativeArray<RootPhenotypeEntityCreationRequest> neuralRequestsArray = new(neuralNetworkRequests.ToArray(), Allocator.Temp);
        
        // Batch create all entities at once.
        using NativeArray<Entity> limbEntities = new(limbRequests.Count, Allocator.Temp);
        using NativeArray<Entity> jointEntities = new(jointRequests.Count, Allocator.Temp);
        using NativeArray<Entity> neuralNetworkEntities = new(neuralNetworkRequests.Count, Allocator.Temp);
        entityManager.CreateEntity(limbRequestArchetype, limbEntities);
        entityManager.CreateEntity(jointRequestArchetype, jointEntities);
        entityManager.CreateEntity(neuralNetworkRequestArchetype, neuralNetworkEntities);
        
        // Set component data.
        for (int i = 0; i < limbRequestsArray.Length; i++)
            entityManager.SetComponentData(limbEntities[i], limbRequestsArray[i]);
        for (int i = 0; i < jointRequestsArray.Length; i++)
            entityManager.SetComponentData(jointEntities[i], jointRequestsArray[i]);
        for (int i = 0; i < neuralRequestsArray.Length; i++)
            entityManager.SetComponentData(neuralNetworkEntities[i], neuralRequestsArray[i]);
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
                LimbIndex = (byte)i,
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
