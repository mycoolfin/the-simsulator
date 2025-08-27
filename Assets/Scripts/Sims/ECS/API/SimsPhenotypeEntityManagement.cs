using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.API
{
    using TheSimsulator.Sims.Phenotype;
    using Components.Phenotype;
    using Core;
    using Core.ECS.API;
    using Core.ECS.Math;
    using Core.ECS.Rendering;
    using NeuralNetwork;
    using Builders;
    using Systems.Initialisation;

    public class SimsPhenotypeEntityManagement : IPhenotypeEntityManagement<SimsPhenotype>
    {
        public IEnumerator CreateEntitiesFromPhenotypes(World world, List<PhenotypeEntityCreationInfo<SimsPhenotype>> creationInfoList)
        {
            if (world == null || !world.IsCreated)
                yield break;

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
            foreach (PhenotypeEntityCreationInfo<SimsPhenotype> c in creationInfoList)
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
            while (world.IsCreated && (!limbQuery.IsEmptyIgnoreFilter || !jointQuery.IsEmptyIgnoreFilter || !rootPhenotypeQuery.IsEmptyIgnoreFilter))
                yield return null;
        }

        public IEnumerator DestroyAllPhenotypeEntities(World world)
        {
            if (world == null || !world.IsCreated)
                yield break;

            EntityManager entityManager = world.EntityManager;
            EntityArchetype destructionRequestArchetype = entityManager.CreateArchetype(
                typeof(DestroyPhenotypeEntitiesRequest)
            );
            DestroyPhenotypeEntitiesRequest request = new() { PhenotypeGid = 0 };
            Entity destructionRequestEntity = entityManager.CreateEntity(destructionRequestArchetype);
            entityManager.SetComponentData(destructionRequestEntity, request);

            while (world.IsCreated && entityManager.HasComponent<DestroyPhenotypeEntitiesRequest>(destructionRequestEntity))
                yield return null;
        }

        public void MarkPhenotypeEntitiesForDestruction(World world, SimsPhenotype phenotype)
        {
            if (world == null || !world.IsCreated || phenotype == null)
                return;

            EntityManager entityManager = world.EntityManager;
            EntityArchetype destructionRequestArchetype = entityManager.CreateArchetype(
                typeof(DestroyPhenotypeEntitiesRequest)
            );
            DestroyPhenotypeEntitiesRequest request = new() { PhenotypeGid = phenotype.Gid };
            Entity destructionRequestEntity = entityManager.CreateEntity(destructionRequestArchetype);
            entityManager.SetComponentData(destructionRequestEntity, request);
        }

        public PhenotypeTransformData GetPhenotypeTransformData(World world, SimsPhenotype phenotype)
        {
            if (world == null || !world.IsCreated)
                return new PhenotypeTransformData();

            EntityManager entityManager = world.EntityManager;
            ulong phenotypeGid = phenotype.Gid;

            // Find the root phenotype entity with matching PhenotypeGid component.
            using EntityQuery rootQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<PhenotypeGid>(),
                ComponentType.ReadOnly<PhenotypeBoundingBox>()
            );

            Entity rootEntity = Entity.Null;
            PhenotypeBoundingBox boundingBox = default;

            using NativeArray<ArchetypeChunk> rootChunks = rootQuery.ToArchetypeChunkArray(Allocator.Temp);
            ComponentTypeHandle<PhenotypeGid> phenotypeGidType = entityManager.GetComponentTypeHandle<PhenotypeGid>(true);
            ComponentTypeHandle<PhenotypeBoundingBox> boundingBoxType = entityManager.GetComponentTypeHandle<PhenotypeBoundingBox>(true);
            EntityTypeHandle entityType = entityManager.GetEntityTypeHandle();

            foreach (ArchetypeChunk chunk in rootChunks)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                NativeArray<PhenotypeGid> gids = chunk.GetNativeArray(ref phenotypeGidType);
                NativeArray<PhenotypeBoundingBox> boundingBoxes = chunk.GetNativeArray(ref boundingBoxType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    if (gids[i].Value == phenotypeGid)
                    {
                        rootEntity = entities[i];
                        boundingBox = boundingBoxes[i];
                        goto FoundRootEntity; // Break out of nested loops.
                    }
                }
            }

        FoundRootEntity:
            if (rootEntity == Entity.Null)
                return new PhenotypeTransformData();

            // Find the limb entity with LimbIndex.Value == 0 and RootPhenotypeEntity.Value == rootEntity.
            using EntityQuery limbQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<LimbIndex>(),
                ComponentType.ReadOnly<RootPhenotypeEntity>(),
                ComponentType.ReadOnly<LocalTransform>()
            );

            LocalTransform localTransform = default;
            bool foundLimb = false;

            using NativeArray<ArchetypeChunk> limbChunks = limbQuery.ToArchetypeChunkArray(Allocator.Temp);
            ComponentTypeHandle<LimbIndex> limbIndexType = entityManager.GetComponentTypeHandle<LimbIndex>(true);
            ComponentTypeHandle<RootPhenotypeEntity> rootPhenotypeEntityType = entityManager.GetComponentTypeHandle<RootPhenotypeEntity>(true);
            ComponentTypeHandle<LocalTransform> localTransformType = entityManager.GetComponentTypeHandle<LocalTransform>(true);

            foreach (ArchetypeChunk chunk in limbChunks)
            {
                NativeArray<LimbIndex> limbIndices = chunk.GetNativeArray(ref limbIndexType);
                NativeArray<RootPhenotypeEntity> rootPhenotypeEntities = chunk.GetNativeArray(ref rootPhenotypeEntityType);
                NativeArray<LocalTransform> localTransforms = chunk.GetNativeArray(ref localTransformType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    if (limbIndices[i].Value == 0 && rootPhenotypeEntities[i].Value == rootEntity)
                    {
                        localTransform = localTransforms[i];
                        foundLimb = true;
                        goto FoundLimb; // Break out of nested loops.
                    }
                }
            }

        FoundLimb:
            if (!foundLimb)
                return new PhenotypeTransformData();


            return new PhenotypeTransformData
            {
                Position = localTransform.Position,
                Rotation = localTransform.Rotation,
                Bounds = new()
                {
                    center = boundingBox.Center,
                    extents = boundingBox.CurrentExtents
                }
            };
        }

        public void SetPhenotypeCompanionObject(World world, SimsPhenotype phenotype, PhenotypeCompanionObject companionObject)
        {
            if (!world.IsCreated)
                return;

            EntityManager entityManager = world.EntityManager;
            Entity rootPhenotypeEntity = GetPhenotypeEntity(world, phenotype);
            if (rootPhenotypeEntity == Entity.Null) return;

            if (entityManager.HasComponent<PhenotypeCompanionObject>(rootPhenotypeEntity))
                entityManager.RemoveComponent<PhenotypeCompanionObject>(rootPhenotypeEntity);

            if (companionObject != null)
                entityManager.AddComponentObject(rootPhenotypeEntity, companionObject);
        }

        private Entity GetPhenotypeEntity(World world, SimsPhenotype phenotype)
        {
            if (!world.IsCreated)
                return Entity.Null;

            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PhenotypeGid>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            using NativeArray<PhenotypeGid> phenotypeGids = query.ToComponentDataArray<PhenotypeGid>(Allocator.Temp);
            for (int i = 0; i < phenotypeGids.Length; i++)
            {
                if (phenotypeGids[i].Value == phenotype.Gid)
                {
                    return entities[i];
                }
            }
            return Entity.Null;
        }

        private static CollisionFilter RayToPhenotypeFilter = new()
        {
            BelongsTo = LimbEntityBuilder.ALL_LAYERS,
            CollidesWith = LimbEntityBuilder.PHENOTYPE_LAYER,
            GroupIndex = 0
        };

        public bool TryRaycastToPhenotype(World world, UnityEngine.Ray ray, float rayLength, out ulong phenotypeGid)
        {
            phenotypeGid = default;

            EntityManager entityManager = world.EntityManager;
            EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PhysicsWorldSingleton>());
            if (query.IsEmptyIgnoreFilter) return false;

            PhysicsWorldSingleton pws = query.GetSingleton<PhysicsWorldSingleton>();
            RaycastInput input = new()
            {
                Start = ray.origin,
                End = ray.origin + ray.direction * rayLength,
                Filter = RayToPhenotypeFilter
            };

            if (pws.CollisionWorld.CastRay(input, out RaycastHit hit))
            {
                Entity hitEntity = pws.Bodies[hit.RigidBodyIndex].Entity;
                if (entityManager.HasComponent<RootPhenotypeEntity>(hitEntity))
                {
                    Entity rootPhenotypeEntity = entityManager.GetComponentData<RootPhenotypeEntity>(hitEntity).Value;
                    phenotypeGid = entityManager.GetComponentData<PhenotypeGid>(rootPhenotypeEntity).Value;
                    return true;
                }
            }
            return false;
        }

        private static List<LimbEntityCreationRequest> ConvertToLimbCreationRequests(PhenotypeEntityCreationInfo<SimsPhenotype> info)
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
                    PhysicsPositionOffset = (float3)info.PhysicsPositionOffset,
                    AllowInterPhenotypeCollisions = info.AllowInterPhenotypeCollisions ? (byte)1 : (byte)0
                };
                requests.Add(request);
            }

            return requests;
        }

        private static List<JointEntityCreationRequest> ConvertToJointCreationRequests(PhenotypeEntityCreationInfo<SimsPhenotype> info)
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

        private static RootPhenotypeEntityCreationRequest ConvertToRootPhenotypeCreationRequest(PhenotypeEntityCreationInfo<SimsPhenotype> info, BlobBuilder builder)
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
