using System.Collections.Generic;
using System.Diagnostics;
using mycoolfin.TheSimsulator.Sims;
using Unity.Collections;
using Unity.Entities;

public static class EntityCreationAPI
{
    public static void CreateEntitiesFromPhenotypes(List<SimsPhenotype> phenotypes)
    {
        World world = World.DefaultGameObjectInjectionWorld;

        EntityManager entityManager = world.EntityManager;
        EntityArchetype archetype = entityManager.CreateArchetype(
            typeof(LimbCreationRequest)
        );

        List<LimbCreationRequest> limbRequests = new();
        foreach (SimsPhenotype phenotype in phenotypes)
        {
            limbRequests.AddRange(phenotype.ConvertToLimbCreationRequests());
        }

        using NativeArray<Entity> entities = new(limbRequests.Count, Allocator.Temp);
        entityManager.CreateEntity(archetype, entities);
        for (int i = 0; i < limbRequests.Count; i++)
        {
            entityManager.SetComponentData(entities[i], limbRequests[i]);
        }
    }

    private static List<LimbCreationRequest> ConvertToLimbCreationRequests(this SimsPhenotype phenotype)
    {
        List<LimbCreationRequest> requests = new();
        foreach (mycoolfin.TheSimsulator.Sims.Limb limb in phenotype.Limbs)
        {
            LimbCreationRequest request = new()
            {
                PhenotypeId = phenotype.Gid,
                Position = limb.Position.ToFloat3(),
                Rotation = limb.Rotation.ToQuaternion(),
                Dimensions = limb.Dimensions.ToFloat3(),
                Mass = limb.Mass,
                Color = limb.Color.ToFloat4()
            };
            requests.Add(request);
        }

        return requests;
    }
}
