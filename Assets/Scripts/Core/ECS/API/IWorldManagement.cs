using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
    public interface IWorldManagement
    {
        World GetWorld(string worldName);
        World GetOrCreateWorld(string worldName);
        void DestroyWorld(World world);
    }
}
