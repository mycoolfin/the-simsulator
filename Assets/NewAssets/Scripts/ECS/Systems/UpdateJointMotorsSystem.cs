using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(PhysicsSimulationGroup))]
public partial struct UpdateJointMotorsSystem : ISystem
{
    public static bool Enabled = false;

    public readonly void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<SimulationSingleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!Enabled)
            return;

        SimulationSingleton simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        if (simulation.Type == SimulationType.NoPhysics)
            return;

        state.Dependency = new UpdateHingeMotorsJob
        {
            CurrentTime = (float)SystemAPI.Time.ElapsedTime
        }.Schedule(state.Dependency);
    }
}

[BurstCompile]
public partial struct UpdateHingeMotorsJob : IJobEntity
{
    public float TargetAngle;
    public float CurrentTime;

    public void Execute(Entity entity, ref PhysicsJoint joint)
    {
        FixedList512Bytes<Constraint> constraints = joint.GetConstraints();

        // TODO: Joint-aware constraint indexing.
        ref Constraint constraint = ref constraints.ElementAt(0);

        if (constraint.Type == ConstraintType.RotationMotor)
        {
            constraint.Target = new float3(math.sin(CurrentTime), 0f, 0f);
        }

        joint.SetConstraints(constraints);
    }
}