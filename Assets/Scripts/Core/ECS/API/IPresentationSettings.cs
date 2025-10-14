using Unity.Entities;
using Unity.Mathematics;

namespace mycoolfin.TheSimsulator.UnityIntegration.Core.ECS.API
{
    public interface IPresentationSettings
    {
        void SetWorldVisualOffset(World world, float4x4 transformMatrix);

        void SetColorByFitness(World world, bool enabled);

        void SetFilterBySurvivors(World world, bool enabled, int maxSurvivors = 0);
    }
}
