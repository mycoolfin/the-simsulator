using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public class JointDebug : MonoBehaviour
{
    public bool ShowJoints = false;
    public bool MoveJoints = false;

    private void Update()
    {
        UpdateJointMotorsSystem.Enabled = MoveJoints;
    }

    private void OnDrawGizmos()
    {
        if (!ShowJoints)
            return;

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        var em = world.EntityManager;

        using var joints = em.CreateEntityQuery(typeof(PhysicsJoint), typeof(PhysicsConstrainedBodyPair)).ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var jointEntity in joints)
        {
            var joint = em.GetComponentData<PhysicsJoint>(jointEntity);
            var pair = em.GetComponentData<PhysicsConstrainedBodyPair>(jointEntity);

            if (!em.HasComponent<LocalTransform>(pair.EntityA) || !em.HasComponent<LocalTransform>(pair.EntityB))
                continue;

            var aTransform = em.GetComponentData<LocalTransform>(pair.EntityA);
            var bTransform = em.GetComponentData<LocalTransform>(pair.EntityB);

            float3 aPosWorld = math.transform(aTransform.ToMatrix(), joint.BodyAFromJoint.Position);
            float3 bPosWorld = math.transform(bTransform.ToMatrix(), joint.BodyBFromJoint.Position);

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(aPosWorld, 0.1f);
            Gizmos.color = Color.purple;
            Gizmos.DrawSphere(bPosWorld, 0.1f);

            float3 aAxisWorld = math.rotate(aTransform.Rotation, joint.BodyAFromJoint.Axis);
            float3 bAxisWorld = math.rotate(bTransform.Rotation, joint.BodyBFromJoint.Axis);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(aPosWorld, aPosWorld + aAxisWorld * 0.5f);
            Gizmos.color = Color.darkRed;
            Gizmos.DrawLine(bPosWorld, bPosWorld + bAxisWorld * 0.5f);

            float3 aPerpendicularAxisWorld = math.rotate(aTransform.Rotation, joint.BodyAFromJoint.PerpendicularAxis);
            float3 bPerpendicularAxisWorld = math.rotate(bTransform.Rotation, joint.BodyBFromJoint.PerpendicularAxis);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(aPosWorld, aPosWorld + aPerpendicularAxisWorld * 0.5f);
            Gizmos.color = Color.darkGreen;
            Gizmos.DrawLine(bPosWorld, bPosWorld + bPerpendicularAxisWorld * 0.5f);

            float3 aForwardAxisWorld = math.rotate(aTransform.Rotation, math.cross(joint.BodyAFromJoint.Axis, joint.BodyAFromJoint.PerpendicularAxis));
            float3 bForwardAxisWorld = math.rotate(bTransform.Rotation, math.cross(joint.BodyBFromJoint.Axis, joint.BodyBFromJoint.PerpendicularAxis));
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(aPosWorld, aPosWorld + aForwardAxisWorld * 0.5f);
            Gizmos.color = Color.darkBlue;
            Gizmos.DrawLine(bPosWorld, bPosWorld + bForwardAxisWorld * 0.5f);
        }
    }
}
