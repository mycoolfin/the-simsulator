using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public static class WorldAPI
{
    public static World CreateWorld(string worldName)
    {
        World world = new(worldName);

        // Discover all systems using the default filter.
        WorldSystemFilterFlags flags = WorldSystemFilterFlags.Default;
        IReadOnlyList<System.Type> systems = DefaultWorldInitialization.GetAllSystems(flags);

        // Add discovered systems to the world’s root-level system groups.
        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);

        // Register the world to Unity's PlayerLoop so it actually updates.
        ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);

        Debug.Log($"[ECS] Created world '{worldName}' with {systems.Count} systems.");
        return world;
    }

    public static void DestroyWorld(World world)
    {
        if (world == null || !world.IsCreated)
            return;

        // If the destroyed world is currently default, clear the reference.
        if (World.DefaultGameObjectInjectionWorld == world)
            World.DefaultGameObjectInjectionWorld = null;

        world.Dispose();

        Debug.Log($"[ECS] Destroyed world '{world.Name}'.");
    }
}
