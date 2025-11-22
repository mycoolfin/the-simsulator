using Unity.Entities;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
    public interface IObjectEntityManagement
    {
        void CreateGroundPlane(World world);

        void DestroyGroundPlane(World world);

        void CreateLightSource(World world);

        void DestroyLightSource(World world); 
    }
}
