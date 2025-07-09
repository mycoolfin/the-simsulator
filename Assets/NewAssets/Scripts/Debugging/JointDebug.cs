using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public class JointDebug : MonoBehaviour
{
    public bool ShowJoints = false;

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

            if (!em.HasComponent<LocalToWorld>(pair.EntityA) || !em.HasComponent<LocalToWorld>(pair.EntityB))
                continue;

            var aL2W = em.GetComponentData<LocalToWorld>(pair.EntityA);
            var bL2W = em.GetComponentData<LocalToWorld>(pair.EntityB);

            // Position of joint anchors in world space
            float3 aPosWorld = math.transform(aL2W.Value, joint.BodyAFromJoint.Position);
            float3 bPosWorld = math.transform(bL2W.Value, joint.BodyBFromJoint.Position);

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(aPosWorld, 0.1f);
            Gizmos.color = Color.purple;
            Gizmos.DrawSphere(bPosWorld, 0.1f);

            // Joint axes in world space (rotate from local to world using L2W's rotation basis)
            float3x3 aRot = new float3x3(aL2W.Value.c0.xyz, aL2W.Value.c1.xyz, aL2W.Value.c2.xyz);
            float3x3 bRot = new float3x3(bL2W.Value.c0.xyz, bL2W.Value.c1.xyz, bL2W.Value.c2.xyz);

            float3 aAxisWorld = math.mul(aRot, joint.BodyAFromJoint.Axis);
            float3 bAxisWorld = math.mul(bRot, joint.BodyBFromJoint.Axis);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(aPosWorld, aPosWorld + aAxisWorld * 0.5f);
            Gizmos.color = new Color(0.5f, 0f, 0f); // dark red
            Gizmos.DrawLine(bPosWorld, bPosWorld + bAxisWorld * 0.5f);

            float3 aPerpendicularAxisWorld = math.mul(aRot, joint.BodyAFromJoint.PerpendicularAxis);
            float3 bPerpendicularAxisWorld = math.mul(bRot, joint.BodyBFromJoint.PerpendicularAxis);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(aPosWorld, aPosWorld + aPerpendicularAxisWorld * 0.5f);
            Gizmos.color = new Color(0f, 0.3f, 0f); // dark green
            Gizmos.DrawLine(bPosWorld, bPosWorld + bPerpendicularAxisWorld * 0.5f);

            float3 aForwardAxisWorld = math.cross(aAxisWorld, aPerpendicularAxisWorld);
            float3 bForwardAxisWorld = math.cross(bAxisWorld, bPerpendicularAxisWorld);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(aPosWorld, aPosWorld + aForwardAxisWorld * 0.5f);
            Gizmos.color = new Color(0f, 0f, 0.5f); // dark blue
            Gizmos.DrawLine(bPosWorld, bPosWorld + bForwardAxisWorld * 0.5f);
        }
    }
}
