using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
    public interface IWorldManagement
    {
        World CreateWorld(string worldName);
        void DestroyWorld(World world);
    }
}
