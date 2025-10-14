using System;
using System.Collections.Generic;
using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
    public class WorldManagement : IWorldManagement
    {
        public World GetWorld(string worldName)
        {
            // Find the world by name.
            foreach (World world in World.All)
            {
                if (world != null && world.IsCreated && 
                    string.Equals(world.Name, worldName, System.StringComparison.Ordinal))
                    return world;
            }
            return null;
        }

        public World GetOrCreateWorld(string worldName, Action<World> onWorldCreated)
        {
            // If this world already exists, return it.
            World existingWorld = GetWorld(worldName);
            if (existingWorld != null && existingWorld.IsCreated)
                return existingWorld;

            World world = new(worldName);

            // Discover all systems using the default filter.
            WorldSystemFilterFlags flags = WorldSystemFilterFlags.Default;
            IReadOnlyList<Type> systems = DefaultWorldInitialization.GetAllSystems(flags);

            // Add discovered systems to the world’s root-level system groups.
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);

            // Register the world to Unity's PlayerLoop so it actually updates.
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);

            onWorldCreated?.Invoke(world);

            return world;
        }

        public void DestroyWorld(World world)
        {
            if (world == null || !world.IsCreated)
                return;

            // If the destroyed world is currently default, clear the reference.
            if (World.DefaultGameObjectInjectionWorld == world)
                World.DefaultGameObjectInjectionWorld = null;

            world.Dispose();
        }
    }
}
