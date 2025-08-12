using System.Collections.Generic;
using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
    public class WorldManagement : IWorldManagement
    {
        public World CreateWorld(string worldName)
        {
            World world = new(worldName);

            // Discover all systems using the default filter.
            WorldSystemFilterFlags flags = WorldSystemFilterFlags.Default;
            IReadOnlyList<System.Type> systems = DefaultWorldInitialization.GetAllSystems(flags);

            // Add discovered systems to the world’s root-level system groups.
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);

            // Register the world to Unity's PlayerLoop so it actually updates.
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);

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
