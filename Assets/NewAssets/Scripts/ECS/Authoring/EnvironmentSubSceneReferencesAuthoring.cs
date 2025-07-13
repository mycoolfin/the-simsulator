using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;

public struct EnvironmentSubScenes : IComponentData
{
    public EntitySceneReference GroundEnvironment;
    public EntitySceneReference WaterEnvironment;
}

public class EnvironmentSubSceneReferencesAuthoring : MonoBehaviour
{
    public EntitySceneReference GroundEnvironment;
    public EntitySceneReference WaterEnvironment;

    class Baker : Baker<EnvironmentSubSceneReferencesAuthoring>
    {
        public override void Bake(EnvironmentSubSceneReferencesAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new EnvironmentSubScenes
            {
                GroundEnvironment = authoring.GroundEnvironment,
                WaterEnvironment = authoring.WaterEnvironment
            });
        }
    }
}
