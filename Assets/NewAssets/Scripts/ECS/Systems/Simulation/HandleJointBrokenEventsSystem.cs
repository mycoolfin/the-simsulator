using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(JointBreakSystem))]
public partial struct HandleJointBrokenEventsSystem : ISystem
{
    public readonly void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<JointBrokenEvent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        using EntityCommandBuffer ecb = new(Allocator.TempJob);
        new JointBrokenEventHandlerJob
        {
            Ecb = ecb.AsParallelWriter()
        }
        .ScheduleParallel(state.Dependency).Complete();
        ecb.Playback(state.EntityManager);
    }
}

[BurstCompile]
public partial struct JointBrokenEventHandlerJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;

    public void Execute(ref DynamicBuffer<JointBrokenEvent> eventBuffer, ref DynamicBuffer<LimbStatus> limbStatuses, ref NeuralGraphRef neuralGraphRef, ref DynamicBuffer<EmitterState> emitterStates)
    {
        if (eventBuffer.Length == 0)
            return;

        ref readonly BlobAssetReference<CompiledNeuralGraph> graph = ref neuralGraphRef.Value;
        ref BlobArray<CompiledNeuralGraph.ArraySlice> sensorSlices = ref graph.Value.SensorSlices;
        ref BlobArray<CompiledNeuralGraph.ArraySlice> actuatorSlices = ref graph.Value.ActuatorSlices;
        ref BlobArray<CompiledNeuralGraph.ArraySlice> limbNeuronSlices = ref graph.Value.LimbNeuronSlices;
        ref BlobArray<CompiledNeuralGraph.ActuatorMeta> actuatorsMeta = ref graph.Value.Actuators;
        ref BlobArray<CompiledNeuralGraph.NeuronMeta> limbNeuronsMeta = ref graph.Value.LimbNeurons;
        ref BlobArray<CompiledNeuralGraph.NeuronMeta> brainNeuronsMeta = ref graph.Value.BrainNeurons;
        ref BlobArray<CompiledNeuralGraph.InputMeta> inputMetas = ref graph.Value.Inputs;
        ushort sensorEmitterStartIndex = graph.Value.SensorEmitterStartIndex;
        ushort actuatorNeuronEmitterStartIndex = graph.Value.ActuatorNeuronEmitterStartIndex;
        ushort limbNeuronEmitterStartIndex = graph.Value.LimbNeuronEmitterStartIndex;
        NativeArray<float> e = emitterStates.Reinterpret<float>().AsNativeArray(); // Reinterpret as float for potentially better Burst performance.

        for (int i = 0; i < eventBuffer.Length; i++)
        {
            JointBrokenEvent jointBrokenEvent = eventBuffer[i];

            // Tag the detached limb entity.
            Ecb.AddComponent(0, jointBrokenEvent.DetachedLimbEntity, new DetachedLimbTag());

            // Update the on-board limb status.
            LimbStatus limbStatus = limbStatuses[jointBrokenEvent.LimbIndex];
            limbStatus.AttachmentState = AttachmentState.Detached;
            limbStatuses[jointBrokenEvent.LimbIndex] = limbStatus;

            // Zero out the emitter states for the detached limb.
            CompiledNeuralGraph.ArraySlice sensorsSlice = sensorSlices[jointBrokenEvent.LimbIndex];
            CompiledNeuralGraph.ArraySlice actuatorSlice = actuatorSlices[jointBrokenEvent.LimbIndex];
            CompiledNeuralGraph.ArraySlice limbNeuronSlice = limbNeuronSlices[jointBrokenEvent.LimbIndex];
            ushort senStartIndex = (ushort)(sensorEmitterStartIndex + sensorsSlice.startIndex);
            ushort actStartIndex = (ushort)(actuatorNeuronEmitterStartIndex + actuatorSlice.startIndex);
            ushort lnStartIndex = (ushort)(limbNeuronEmitterStartIndex + limbNeuronSlice.startIndex);
            for (int j = 0; j < sensorsSlice.count; j++) e[senStartIndex + j] = 0f;
            for (int j = 0; j < actuatorSlice.count; j++) e[actStartIndex + j] = 0f;
            for (int j = 0; j < limbNeuronSlice.count; j++) e[lnStartIndex + j] = 0f;
        }

        eventBuffer.Clear();
    }
}
