using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Jobs;

public struct FluidSimulationSettings : IComponentData
{
    public byte Enabled;
    public float FluidDensity;
}

[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
public partial struct ApplyFluidForcesSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsVelocity>();
        state.RequireForUpdate<PhysicsMass>();
        state.RequireForUpdate<LocalTransform>();
        state.RequireForUpdate<PostTransformMatrix>();
        state.RequireForUpdate<LimbIndex>();
        state.RequireForUpdate<FluidSimulationSettings>();
    }

    public void OnUpdate(ref SystemState state)
    {
        FluidSimulationSettings settings = SystemAPI.GetSingleton<FluidSimulationSettings>();
        if (settings.Enabled == 0)
            return;

        JobHandle handle = new ApplyFluidForcesJob
        {
            FluidDensity = settings.FluidDensity,
            DeltaTime = SystemAPI.Time.DeltaTime
        }
        .ScheduleParallel(state.Dependency);
        state.Dependency = handle;
    }
}

[BurstCompile]
[WithAll(typeof(LimbIndex))]
public partial struct ApplyFluidForcesJob : IJobEntity
{
    public float FluidDensity;
    public float DeltaTime;

    public void Execute(
        ref PhysicsVelocity velocity,
        in PhysicsMass mass,
        in LocalTransform transform,
        in PostTransformMatrix postTransform)
    {
        float invMass = mass.InverseMass;
        if (invMass <= 0f)
            return; // Static or kinematic

        float3 position = transform.Position;
        quaternion rotation = transform.Rotation;
        float3 scale = postTransform.Value.Scale();

        float3 totalForce = float3.zero;
        float3 totalTorque = float3.zero;

        // Apply fluid forces to all 6 faces (±X, ±Y, ±Z)
        // Format: faceNormal, tangent1, tangent2, size1, size2

        // ±X
        ApplyFace(+1, 0, 0, 0, 1, 0, 0, 0, 1, scale.y, scale.z, position, rotation, FluidDensity, velocity, ref totalForce, ref totalTorque);
        ApplyFace(-1, 0, 0, 0, 1, 0, 0, 0, 1, scale.y, scale.z, position, rotation, FluidDensity, velocity, ref totalForce, ref totalTorque);
        // ±Y
        ApplyFace(0, +1, 0, 1, 0, 0, 0, 0, 1, scale.x, scale.z, position, rotation, FluidDensity, velocity, ref totalForce, ref totalTorque);
        ApplyFace(0, -1, 0, 1, 0, 0, 0, 0, 1, scale.x, scale.z, position, rotation, FluidDensity, velocity, ref totalForce, ref totalTorque);
        // ±Z
        ApplyFace(0, 0, +1, 1, 0, 0, 0, 1, 0, scale.x, scale.y, position, rotation, FluidDensity, velocity, ref totalForce, ref totalTorque);
        ApplyFace(0, 0, -1, 1, 0, 0, 0, 1, 0, scale.x, scale.y, position, rotation, FluidDensity, velocity, ref totalForce, ref totalTorque);

        // Apply accumulated force and torque
        velocity.Linear += DeltaTime * invMass * totalForce;

        float3x3 localInvInertiaMatrix = float3x3.Scale(mass.InverseInertia);
        float3x3 R = new(rotation);
        float3x3 invInertiaWorld = math.mul(math.mul(R, localInvInertiaMatrix), math.transpose(R));

        velocity.Angular += DeltaTime * math.mul(invInertiaWorld, totalTorque);
    }

    [BurstCompile]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyFace(
            float nx, float ny, float nz,
            float t1x, float t1y, float t1z,
            float t2x, float t2y, float t2z,
            float sizeX, float sizeY,
            in float3 position, in quaternion rotation,
            in float FluidDensity,
            in PhysicsVelocity velocity,
            ref float3 totalForce,
            ref float3 totalTorque
    )
    {
        float3 localNormal = new(nx, ny, nz);
        float3 worldNormal = math.rotate(rotation, localNormal);
        float3 t1 = new(t1x, t1y, t1z);
        float3 t2 = new(t2x, t2y, t2z);
        float areaPerQuadrant = 0.25f * sizeX * sizeY;

        for (int i = -1; i <= 1; i += 2)
            for (int j = -1; j <= 1; j += 2)
            {
                float3 localOffset = 0.5f * localNormal
                                   + 0.25f * i * sizeX * t1
                                   + 0.25f * j * sizeY * t2;

                float3 worldOffset = math.rotate(rotation, localOffset);
                float3 worldPos = position + worldOffset;
                float3 r = worldPos - position;

                float3 localVelocity = velocity.Linear + math.cross(velocity.Angular, r);
                float speedSq = math.lengthsq(localVelocity);
                if (speedSq < 1e-6f) continue;

                float speed = math.sqrt(speedSq);
                float3 velDir = localVelocity / speed;

                float dDot = math.dot(velDir, worldNormal);
                if (dDot <= 1e-3f) continue; // Facing away.

                ComputeFluidForce(worldNormal, velDir, areaPerQuadrant, speed, speedSq, FluidDensity, out float3 force);
                totalForce += force;
                totalTorque += math.cross(r, force);
            }
    }

    [BurstCompile]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ComputeFluidForce(in float3 normal, in float3 velDir, float area,  float speed, float speedSq, float fluidDensity, out float3 force)
    {
        float dDot = math.dot(velDir, normal);
        float3 signedNormal = math.sign(dDot) * normal;
        float absC = math.min(math.abs(dDot), 1f);
        if (absC < 1e-3f) { force = float3.zero; return; }

        float d = 1f - absC;
        float Cd = 0.5f + 1.5f * (d * d);
        float root = math.sqrt(1f - absC * absC);
        float Cl = 1.2f * absC * root;
        float projV = absC * speed;

        float dragMag = 0.5f * fluidDensity * projV * projV * area * Cd;
        float3 dragF = -dragMag * signedNormal;

        float liftMag = 0.5f * fluidDensity * speedSq * area * Cl * absC;
        float3 liftDir = math.normalizesafe(math.cross(math.cross(velDir, signedNormal), velDir));
        float3 liftF = liftMag * liftDir;

        force = dragF + liftF;
    }
}
