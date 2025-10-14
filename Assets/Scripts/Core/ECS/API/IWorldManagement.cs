using System;
using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
    public interface IWorldManagement
    {
        World GetWorld(string worldName);
        World GetOrCreateWorld(string worldName, Action<World> onWorldCreated = null);
        void DestroyWorld(World world);
    }
}
