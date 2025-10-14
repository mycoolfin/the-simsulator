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

            // Remove scaling from L2W: extract pure rotation (orthonormalize basis) and translation only
            float3 aPos = aL2W.Position;
            float3 bPos = bL2W.Position;

            // Orthonormalize rotation basis (Gram-Schmidt)
            float3 aRight = math.normalize(aL2W.Value.c0.xyz);
            float3 aUp = math.normalize(aL2W.Value.c1.xyz - aRight * math.dot(aL2W.Value.c1.xyz, aRight));
            float3 aForward = math.cross(aRight, aUp);
            float3x3 aBasis = new float3x3(aRight, aUp, aForward);
            quaternion aRot = new quaternion(aBasis);

            float3 bRight = math.normalize(bL2W.Value.c0.xyz);
            float3 bUp = math.normalize(bL2W.Value.c1.xyz - bRight * math.dot(bL2W.Value.c1.xyz, bRight));
            float3 bForward = math.cross(bRight, bUp);
            float3x3 bBasis = new float3x3(bRight, bUp, bForward);
            quaternion bRot = new quaternion(bBasis);

            // Position of joint anchors in world space (ignore scale)
            float3 aPosWorld = aPos + math.rotate(aRot, joint.BodyAFromJoint.Position);
            float3 bPosWorld = bPos + math.rotate(bRot, joint.BodyBFromJoint.Position);

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(aPosWorld, 0.1f);
            Gizmos.color = Color.purple;
            Gizmos.DrawSphere(bPosWorld, 0.1f);

            // Joint axes in world space (ignore scale, use only rotation)
            float3 aAxisWorld = math.rotate(aRot, joint.BodyAFromJoint.Axis);
            float3 bAxisWorld = math.rotate(bRot, joint.BodyBFromJoint.Axis);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(aPosWorld, aPosWorld + aAxisWorld * 0.5f);
            Gizmos.color = new Color(0.5f, 0f, 0f); // dark red
            Gizmos.DrawLine(bPosWorld, bPosWorld + bAxisWorld * 0.5f);

            float3 aPerpendicularAxisWorld = math.rotate(aRot, joint.BodyAFromJoint.PerpendicularAxis);
            float3 bPerpendicularAxisWorld = math.rotate(bRot, joint.BodyBFromJoint.PerpendicularAxis);

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
