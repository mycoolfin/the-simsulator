using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;

namespace mycoolfin.TheSimsulator.UnityIntegration.Sims.ECS.Systems.Simulation.Limbs
{
    using Components.Phenotype;

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

            state.Dependency = new ApplyFluidForcesJob
            {
                FluidDensity = settings.FluidDensity,
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(LimbIndex))]
    public partial struct ApplyFluidForcesJob : IJobEntity
    {
        public float FluidDensity;
        public float DeltaTime;

        // Numerical stability constants
        private const float MinSpeedSq = 1e-3f;
        private const float MinAbsC = 1e-2f;
        private const float MinCrossMagSq = 1e-6f;
        private const float MinDotThreshold = 1e-3f;

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

            // Clamp forces to prevent excessive values.
            totalForce = math.clamp(totalForce, new float3(-1000f), new float3(1000f));
            totalTorque = math.clamp(totalTorque, new float3(-1000f), new float3(1000f));

            // Validate inertia values
            if (math.any(mass.InverseInertia > 1000f) || math.any(mass.InverseInertia < 0f))
                return;

            // Apply accumulated force and torque
            float3x3 localInvInertiaMatrix = float3x3.Scale(mass.InverseInertia);
            float3x3 R = new(rotation);
            float3x3 invInertiaWorld = math.mul(math.mul(R, localInvInertiaMatrix), math.transpose(R));

            float3 linearVelocityDelta = DeltaTime * invMass * totalForce;
            float3 angularVelocityDelta = DeltaTime * math.mul(invInertiaWorld, totalTorque);

            // Clamp velocity deltas to prevent extreme values
            linearVelocityDelta = math.clamp(linearVelocityDelta, new float3(-100f), new float3(100f));
            angularVelocityDelta = math.clamp(angularVelocityDelta, new float3(-10f), new float3(10f));

            // Check for NaN or Inf values
            if (math.any(math.isnan(linearVelocityDelta)) || math.any(math.isinf(linearVelocityDelta)) ||
                math.any(math.isnan(angularVelocityDelta)) || math.any(math.isinf(angularVelocityDelta)))
                return;

            // Apply with additional safety clamping
            velocity.Linear = math.clamp(velocity.Linear + linearVelocityDelta, new float3(-200f), new float3(200f));
            velocity.Angular = math.clamp(velocity.Angular + angularVelocityDelta, new float3(-50f), new float3(50f));
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

                    if (speedSq < MinSpeedSq) continue;

                    float speed = math.sqrt(speedSq);
                    float3 velDir = localVelocity / speed;

                    float dDot = math.dot(velDir, worldNormal);
                    if (dDot <= MinDotThreshold) continue; // Facing away.

                    ComputeFluidForce(worldNormal, velDir, dDot, areaPerQuadrant, speed, speedSq, FluidDensity, out float3 force);
                    totalForce += force;
                    totalTorque += math.cross(r, force);
                }
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ComputeFluidForce(in float3 normal, in float3 velDir, float dDot, float area, float speed, float speedSq, float fluidDensity, out float3 force)
        {
            float3 signedNormal = math.sign(dDot) * normal;
            float absC = math.min(math.abs(dDot), 1f);

            // More conservative threshold to prevent numerical issues
            if (absC < MinAbsC) { force = float3.zero; return; }

            // Compute drag coefficient using stable formulation
            float d = 1f - absC;
            float Cd = 0.5f + 1.5f * (d * d);

            // Lift coefficient calculation
            float absCsq = absC * absC;
            float oneMinusAbsCsq = math.max(1f - absCsq, 0f); // Clamp to prevent negative values
            float root = math.sqrt(oneMinusAbsCsq);
            float Cl = 1.2f * absC * root;

            float projV = absC * speed;
            float dragMag = 0.5f * fluidDensity * projV * projV * area * Cd;
            float3 dragF = -dragMag * signedNormal;

            // Lift force calculation
            float liftMag = 0.5f * fluidDensity * speedSq * area * Cl * absC;

            // Compute lift direction
            float3 crossVelNormal = math.cross(velDir, signedNormal);
            float crossMagSq = math.lengthsq(crossVelNormal);

            float3 liftF = float3.zero;
            if (crossMagSq > MinCrossMagSq) // Only compute lift if cross product is significant
            {
                float3 liftDir = math.cross(crossVelNormal, velDir) / math.sqrt(crossMagSq);
                liftF = liftMag * liftDir;
            }

            force = dragF + liftF;
        }
    }
}
