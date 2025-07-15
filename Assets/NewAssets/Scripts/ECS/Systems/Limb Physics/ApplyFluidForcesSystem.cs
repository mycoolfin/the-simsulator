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

[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateBefore(typeof(PhysicsInitializeGroup))]
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
partial struct ApplyFluidForcesJob : IJobEntity
{
    public float FluidDensity;
    public float DeltaTime;

    public void Execute(ref PhysicsVelocity velocity, in PhysicsMass mass, in LocalTransform transform, in PostTransformMatrix postTransform)
    {
        float invMass = mass.InverseMass;
        if (invMass <= 0f)
            return; // Skip if static or kinematic.

        float3 worldVel = velocity.Linear;
        float speedSq = math.lengthsq(worldVel);
        if (speedSq < 1e-6f)
            return; // Skip if not moving.

        float speed = math.sqrt(speedSq);
        float3 velDir = worldVel / speed;

        // Face normals in world space.
        float3 nx = math.rotate(transform.Rotation, new float3(1f, 0f, 0f));
        float3 ny = math.rotate(transform.Rotation, new float3(0f, 1f, 0f));
        float3 nz = math.rotate(transform.Rotation, new float3(0f, 0f, 1f));

        // Face areas.
        float3 scale = postTransform.Value.Scale();
        float ax = scale.y * scale.z;
        float ay = scale.x * scale.z;
        float az = scale.x * scale.y;

        float3 force = float3.zero;

        // Aggregate per-face forces.
        force += FaceForces(nx, ax, velDir, speed, speedSq, FluidDensity);
        force += FaceForces(ny, ay, velDir, speed, speedSq, FluidDensity);
        force += FaceForces(nz, az, velDir, speed, speedSq, FluidDensity);

        velocity.Linear += DeltaTime * invMass * force;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float3 FaceForces(float3 normal, float area, float3 velDir, float speed, float speedSq, float fluidDensity)
    {
        float dDot = math.dot(velDir, normal);
        float3 signedNormal = math.sign(dDot) * normal;
        float absC = math.abs(dDot);
        if (absC < 1e-3f) return float3.zero;

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

        return dragF + liftF;
    }
}
