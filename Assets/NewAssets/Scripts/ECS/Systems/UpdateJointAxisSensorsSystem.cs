using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct UpdateJointAxisSensorsSystem : ISystem
{
    private ComponentLookup<LocalTransform> localTransformLookup;
    private BufferLookup<EmitterState> emitterStatesLookup;

    public void OnCreate(ref SystemState state)
    {
        localTransformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
        emitterStatesLookup = state.GetBufferLookup<EmitterState>(isReadOnly: false);

        EntityQuery entityQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<PhysicsConstrainedBodyPair>(),
                ComponentType.ReadOnly<PhysicsJoint>(),
                ComponentType.ReadOnly<NeuralNetworkEntity>()
            },
            Any = new[]
            {
                ComponentType.ReadOnly<JointAxisX>(),
                ComponentType.ReadOnly<JointAxisY>(),
                ComponentType.ReadOnly<JointAxisZ>()
            }
        });
        state.RequireForUpdate(entityQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        localTransformLookup.Update(ref state);
        emitterStatesLookup.Update(ref state);

        UpdateJointAxisXSensorJob xJob = new()
        {
            TransformLookup = localTransformLookup,
            EmitterStateBuffers = emitterStatesLookup
        };
        state.Dependency = xJob.ScheduleParallel(state.Dependency);

        UpdateJointAxisYSensorJob yJob = new()
        {
            TransformLookup = localTransformLookup,
            EmitterStateBuffers = emitterStatesLookup
        };
        state.Dependency = yJob.ScheduleParallel(state.Dependency);

        UpdateJointAxisZSensorJob zJob = new()
        {
            TransformLookup = localTransformLookup,
            EmitterStateBuffers = emitterStatesLookup
        };
        state.Dependency = zJob.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct UpdateJointAxisXSensorJob : IJobEntity
{
    public ComponentLookup<LocalTransform> TransformLookup;
    public BufferLookup<EmitterState> EmitterStateBuffers;

    public void Execute(in PhysicsConstrainedBodyPair pair, in PhysicsJoint joint, in JointAxisX jointAxis, in NeuralNetworkEntity neuralNetworkEntity)
    {
        JointAxisSensorJobCore.Axis axis = jointAxis.SwapXZ ? JointAxisSensorJobCore.Axis.Z : JointAxisSensorJobCore.Axis.X;
        JointAxisSensorJobCore.ExecuteAxis(pair, joint, neuralNetworkEntity, axis, jointAxis.SensorEmitterIndex, jointAxis.AngleLimit, ref TransformLookup, ref EmitterStateBuffers);
    }
}

[BurstCompile]
public partial struct UpdateJointAxisYSensorJob : IJobEntity
{
    public ComponentLookup<LocalTransform> TransformLookup;
    public BufferLookup<EmitterState> EmitterStateBuffers;

    public void Execute(in PhysicsConstrainedBodyPair pair, in PhysicsJoint joint, in JointAxisY jointAxis, in NeuralNetworkEntity neuralNetworkEntity)
    {
        JointAxisSensorJobCore.ExecuteAxis(pair, joint, neuralNetworkEntity, JointAxisSensorJobCore.Axis.Y, jointAxis.SensorEmitterIndex, jointAxis.AngleLimit, ref TransformLookup, ref EmitterStateBuffers);
    }
}

[BurstCompile]
public partial struct UpdateJointAxisZSensorJob : IJobEntity
{
    public ComponentLookup<LocalTransform> TransformLookup;
    public BufferLookup<EmitterState> EmitterStateBuffers;

    public void Execute(in PhysicsConstrainedBodyPair pair, in PhysicsJoint joint, in JointAxisZ jointAxis, in NeuralNetworkEntity neuralNetworkEntity)
    {
        JointAxisSensorJobCore.Axis axis = jointAxis.SwapXZ ? JointAxisSensorJobCore.Axis.X : JointAxisSensorJobCore.Axis.Z;
        JointAxisSensorJobCore.ExecuteAxis(pair, joint, neuralNetworkEntity, axis, jointAxis.SensorEmitterIndex, jointAxis.AngleLimit, ref TransformLookup, ref EmitterStateBuffers);
    }
}

[BurstCompile]
public static class JointAxisSensorJobCore
{
    public enum Axis : byte { X, Y, Z }

    [BurstCompile]
    public static void ExecuteAxis(
        in PhysicsConstrainedBodyPair pair,
        in PhysicsJoint joint,
        in NeuralNetworkEntity neuralNetworkEntity,
        in Axis axis,
        ushort emitterIndex,
        float angleLimit,
        ref ComponentLookup<LocalTransform> transformLookup,
        ref BufferLookup<EmitterState> emitterStateBuffers
    )
    {
        if (!transformLookup.HasComponent(pair.EntityA) || !transformLookup.HasComponent(pair.EntityB))
            return;

        if (!emitterStateBuffers.HasBuffer(neuralNetworkEntity.Value))
            return;

        DynamicBuffer<EmitterState> buffer = emitterStateBuffers[neuralNetworkEntity.Value];
        if (emitterIndex >= (ushort)buffer.Length)
            return;

        float angle = GetJointAngle(transformLookup[pair.EntityA].Rotation, transformLookup[pair.EntityB].Rotation, joint.BodyAFromJoint, axis);
        float emitterValue = math.abs(angleLimit) < 1e-6f ? 0f : math.clamp(angle / angleLimit, -1f, 1f);
        buffer[emitterIndex] = new EmitterState { Value = emitterValue };
    }

    /// <summary>
    /// Extracts signed angle (in radians) between EntityA and EntityB around the joint frame defined by BodyAFromJoint, for the specified axis.
    /// </summary>
    /// <param name="worldRotA">Parent (entity A) world rotation</param>
    /// <param name="worldRotB">Child (entity B) world rotation</param>
    /// <param name="jointFrameA">The BodyAFromJoint frame</param>
    /// <param name="axis">Axis around which to compute the angle</param>
    /// <returns>float3: angles around X, Y, and Z of the joint frame, in radians</returns>
    [BurstCompile]
    public static float GetJointAngle(in quaternion worldRotA, in quaternion worldRotB, in BodyFrame jointFrameA, in Axis axis)
    {
        float3 worldPrimaryAxis = math.rotate(worldRotA, jointFrameA.Axis);
        float3 worldSecondaryAxis = math.rotate(worldRotA, jointFrameA.PerpendicularAxis);
        float3 worldTertiaryAxis = math.normalize(math.cross(worldSecondaryAxis, worldPrimaryAxis));

        float3 axisDir = axis switch
        {
            Axis.X => worldPrimaryAxis,
            Axis.Y => worldSecondaryAxis,
            _ => worldTertiaryAxis
        };

        float3 secondaryAxisDir = axis switch
        {
            Axis.X => worldSecondaryAxis,
            Axis.Y => worldTertiaryAxis,
            _ => worldPrimaryAxis
        };

        float3x3 worldJointBasis = new(worldPrimaryAxis, worldSecondaryAxis, worldTertiaryAxis);
        quaternion jointFrameWorld = new(worldJointBasis);

        quaternion relativeRotation = math.mul(math.inverse(worldRotA), worldRotB);
        quaternion relativeToJoint = math.mul(math.inverse(jointFrameWorld), relativeRotation);

        return GetAngleAroundAxis(relativeToJoint, axisDir, secondaryAxisDir);
    }

    /// <summary>
    /// Computes the signed angle (in radians) around a world-space axis from a rotation.
    /// </summary>
    /// <param name="axis">World-space axis (normalized)</param>
    /// <param name="reference">World-space perpendicular reference vector (normalized)</param>
    /// <returns>Signed angle in radians (-π to π)</returns>
    [BurstCompile]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetAngleAroundAxis(in quaternion relativeRotation, in float3 axis, in float3 reference)
    {
        float3 rotated = math.rotate(relativeRotation, reference);
        float3 tangent = math.normalize(math.cross(axis, reference));

        float x = math.dot(rotated, reference);
        float y = math.dot(rotated, tangent);

        return math.atan2(y, x);
    }
}
