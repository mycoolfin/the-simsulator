using System.Numerics;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using mycoolfin.TheSimsulator.Sims;

public struct PhenotypeEntityCreationInfo
{
    public SimsPhenotype Phenotype;
    public Vector3 PhysicsPositionOffset;
    public uint PhysicsWorldIndex;
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

        List<LimbEntityCreationRequest> limbRequests = new();
        List<JointEntityCreationRequest> jointRequests = new();
        foreach (PhenotypeEntityCreationInfo c in creationInfoList)
        {
            limbRequests.AddRange(ConvertToLimbCreationRequests(c));
            jointRequests.AddRange(ConvertToJointCreationRequests(c));
        }

        using NativeArray<Entity> entities = new(limbRequests.Count, Allocator.Temp);
        entityManager.CreateEntity(limbRequestArchetype, entities);
        for (int i = 0; i < limbRequests.Count; i++)
            entityManager.SetComponentData(entities[i], limbRequests[i]);

        using NativeArray<Entity> jointEntities = new(jointRequests.Count, Allocator.Temp);
        entityManager.CreateEntity(jointRequestArchetype, jointEntities);
        for (int i = 0; i < jointRequests.Count; i++)
            entityManager.SetComponentData(jointEntities[i], jointRequests[i]);
    }

    private static List<LimbEntityCreationRequest> ConvertToLimbCreationRequests(PhenotypeEntityCreationInfo info)
    {
        List<LimbEntityCreationRequest> requests = new();
        for (int i = 0; i < info.Phenotype.Limbs.Count; i++)
        {
            mycoolfin.TheSimsulator.Sims.Limb limb = info.Phenotype.Limbs[i];
            LimbEntityCreationRequest request = new()
            {
                LimbIndex = i,
                PhenotypeGid = info.Phenotype.Gid,
                Position = limb.Position.ToFloat3(),
                Rotation = limb.Rotation.ToQuaternion(),
                Dimensions = limb.Dimensions.ToFloat3(),
                Mass = limb.Mass,
                Color = limb.Color.ToFloat4(),
                PhysicsPositionOffset = info.PhysicsPositionOffset.ToFloat3(),
                PhysicsWorldIndex = info.PhysicsWorldIndex

            };
            requests.Add(request);
        }

        return requests;
    }

    private static List<JointEntityCreationRequest> ConvertToJointCreationRequests(PhenotypeEntityCreationInfo info)
    {
        List<JointEntityCreationRequest> requests = new();
        List<mycoolfin.TheSimsulator.Sims.Limb> limbs = info.Phenotype.Limbs;
        for (int i = 0; i < limbs.Count; i++)
        {
            mycoolfin.TheSimsulator.Sims.Limb attachedLimb = limbs[i];
            if (attachedLimb.Joint == null)
                continue;

            int referenceLimbIndex = limbs.FindIndex(l => l == attachedLimb.Joint.ParentLimb);
            if (referenceLimbIndex < 0)
                throw new System.Exception($"Invalid reference limb index: {referenceLimbIndex} for attached limb index: {i}");

            mycoolfin.TheSimsulator.Sims.Limb referenceLimb = limbs[referenceLimbIndex];

            JointEntityCreationRequest request = new()
            {
                PhenotypeGid = info.Phenotype.Gid,
                JointType = attachedLimb.Joint.Type,
                ReferenceLimbIndex = referenceLimbIndex,
                AttachedLimbIndex = i,
                PhysicsWorldIndex = info.PhysicsWorldIndex,
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
}
