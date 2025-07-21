using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.Collections;
using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.API
{
    using Sims.Phenotype;
    using Math;
    using NeuralNetwork;
    using Systems.Initialisation;

    public struct PhenotypeEntityCreationInfo
    {
        public SimsPhenotype Phenotype;
        public Vector3 PhysicsPositionOffset;
        public Vector3 VisualOffset;
        public bool AllowInterPhenotypeCollisions;
    }

    public static class EntityManagement
    {
        public static IEnumerator CreateEntitiesFromPhenotypes(World world, List<PhenotypeEntityCreationInfo> creationInfoList)
        {
            EntityManager entityManager = world.EntityManager;

            EntityArchetype limbRequestArchetype = entityManager.CreateArchetype(
                typeof(LimbEntityCreationRequest)
            );
            EntityArchetype jointRequestArchetype = entityManager.CreateArchetype(
                typeof(JointEntityCreationRequest)
            );
            EntityArchetype rootPhenotypeRequestArchetype = entityManager.CreateArchetype(
                typeof(RootPhenotypeEntityCreationRequest)
            );

            List<LimbEntityCreationRequest> limbRequests = new();
            List<JointEntityCreationRequest> jointRequests = new();
            List<RootPhenotypeEntityCreationRequest> rootPhenotypeRequests = new();
            foreach (PhenotypeEntityCreationInfo c in creationInfoList)
            {
                limbRequests.AddRange(ConvertToLimbCreationRequests(c));
                jointRequests.AddRange(ConvertToJointCreationRequests(c));
                using BlobBuilder builder = new(Allocator.Temp);
                rootPhenotypeRequests.Add(ConvertToRootPhenotypeCreationRequest(c, builder));
            }

            // Convert to NativeArrays for better performance and reduced allocations.
            NativeArray<LimbEntityCreationRequest> limbRequestsArray = new(limbRequests.ToArray(), Allocator.Temp);
            NativeArray<JointEntityCreationRequest> jointRequestsArray = new(jointRequests.ToArray(), Allocator.Temp);
            NativeArray<RootPhenotypeEntityCreationRequest> rootPhenotypeRequestsArray = new(rootPhenotypeRequests.ToArray(), Allocator.Temp);

            // Batch create all entities at once.
            NativeArray<Entity> limbEntities = new(limbRequests.Count, Allocator.Temp);
            NativeArray<Entity> jointEntities = new(jointRequests.Count, Allocator.Temp);
            NativeArray<Entity> rootPhenotypeEntities = new(rootPhenotypeRequests.Count, Allocator.Temp);
            entityManager.CreateEntity(limbRequestArchetype, limbEntities);
            entityManager.CreateEntity(jointRequestArchetype, jointEntities);
            entityManager.CreateEntity(rootPhenotypeRequestArchetype, rootPhenotypeEntities);

            // Set component data.
            for (int i = 0; i < limbRequestsArray.Length; i++)
                entityManager.SetComponentData(limbEntities[i], limbRequestsArray[i]);
            for (int i = 0; i < jointRequestsArray.Length; i++)
                entityManager.SetComponentData(jointEntities[i], jointRequestsArray[i]);
            for (int i = 0; i < rootPhenotypeRequestsArray.Length; i++)
                entityManager.SetComponentData(rootPhenotypeEntities[i], rootPhenotypeRequestsArray[i]);

            // Dispose of NativeArrays to free memory.
            limbRequestsArray.Dispose();
            jointRequestsArray.Dispose();
            rootPhenotypeRequestsArray.Dispose();
            limbEntities.Dispose();
            jointEntities.Dispose();
            rootPhenotypeEntities.Dispose();

            // Query for the existence of any of the request components to ensure they are processed.
            EntityQuery limbQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<LimbEntityCreationRequest>());
            EntityQuery jointQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<JointEntityCreationRequest>());
            EntityQuery rootPhenotypeQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RootPhenotypeEntityCreationRequest>());
            while (!limbQuery.IsEmptyIgnoreFilter || !jointQuery.IsEmptyIgnoreFilter || !rootPhenotypeQuery.IsEmptyIgnoreFilter)
                yield return null;
        }

        public static IEnumerator DestroyAllPhenotypeEntities(World world)
        {
            EntityManager entityManager = world.EntityManager;
            Entity destroyRequestSingleton = entityManager.CreateSingleton<DestroyAllPhenotypeEntitiesRequest>();
            while (entityManager.HasComponent<DestroyAllPhenotypeEntitiesRequest>(destroyRequestSingleton))
                yield return null;
        }

        private static List<LimbEntityCreationRequest> ConvertToLimbCreationRequests(PhenotypeEntityCreationInfo info)
        {
            List<LimbEntityCreationRequest> requests = new();

            for (int i = 0; i < info.Phenotype.Limbs.Count; i++)
            {
                Limb limb = info.Phenotype.Limbs[i];
                LimbEntityCreationRequest request = new()
                {
                    PhenotypeGid = info.Phenotype.Gid,
                    LimbIndex = (byte)i,
                    Position = limb.Position.ToFloat3(),
                    Rotation = limb.Rotation.ToQuaternion(),
                    Dimensions = limb.Dimensions.ToFloat3(),
                    Mass = limb.Mass,
                    Color = limb.Color.ToFloat4(),
                    PhysicsPositionOffset = info.PhysicsPositionOffset.ToFloat3(),
                    AllowInterPhenotypeCollisions = info.AllowInterPhenotypeCollisions ? (byte)1 : (byte)0
                };
                requests.Add(request);
            }

            return requests;
        }

        private static List<JointEntityCreationRequest> ConvertToJointCreationRequests(PhenotypeEntityCreationInfo info)
        {
            List<JointEntityCreationRequest> requests = new();
            List<Limb> limbs = info.Phenotype.Limbs;
            for (int i = 0; i < limbs.Count; i++)
            {
                Limb attachedLimb = limbs[i];
                if (attachedLimb.Joint == null)
                    continue;

                int referenceLimbIndex = limbs.FindIndex(l => l == attachedLimb.Joint.ParentLimb);
                if (referenceLimbIndex < 0)
                    throw new System.Exception($"Invalid reference limb index: {referenceLimbIndex} for attached limb index: {i}");

                Limb referenceLimb = limbs[referenceLimbIndex];

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
                    MinCrossSectionalArea = attachedLimb.Joint.MinCrossSectionalArea
                };
                requests.Add(request);
            }

            return requests;
        }

        private static RootPhenotypeEntityCreationRequest ConvertToRootPhenotypeCreationRequest(PhenotypeEntityCreationInfo info, BlobBuilder builder)
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
}
