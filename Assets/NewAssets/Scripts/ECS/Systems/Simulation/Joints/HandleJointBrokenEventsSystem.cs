using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using mycoolfin.TheSimsulator.Sims.Phenotype;

namespace mycoolfin.TheSimsulator.UnityIntegration.ECS.Systems.Simulation.Joints
{
    using Components.Phenotype;
    using NeuralNetwork;

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(JointBreakSystem))]
    public partial struct HandleJointBrokenEventsSystem : ISystem
    {
        private ComponentLookup<RootPhenotypeEntity> rootPhenotypeEntityLookup;
        private ComponentLookup<LimbIndex> limbIndexLookup;

        public void OnCreate(ref SystemState state)
        {
            rootPhenotypeEntityLookup = state.GetComponentLookup<RootPhenotypeEntity>(isReadOnly: true);
            limbIndexLookup = state.GetComponentLookup<LimbIndex>(isReadOnly: true);

            state.RequireForUpdate<PhenotypeEntitiesMetadata>();
            state.RequireForUpdate<JointBrokenEvent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            DynamicBuffer<JointBrokenEvent> eventBuffer = SystemAPI.GetSingletonBuffer<JointBrokenEvent>();
            if (eventBuffer.Length == 0)
                return; // No events to handle.

            rootPhenotypeEntityLookup.Update(ref state);
            limbIndexLookup.Update(ref state);

            using EntityCommandBuffer ecb = new(Allocator.TempJob);

            // Build lookup from parent → [children].
            int limbCount = SystemAPI.GetSingleton<PhenotypeEntitiesMetadata>().TotalLimbCount;
            int parentToChildCapacity = limbCount * 2; // 2x capacity for good hash map performance.
            using NativeParallelMultiHashMap<Entity, Entity> parentToChildrenLookup = new(parentToChildCapacity, Allocator.TempJob);
            JobHandle buildLookupJobHandle = new BuildParentToChildLookupJob
            {
                ParentToChildLookup = parentToChildrenLookup.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);

            // Handle events.
            int maxDetachedLimbCount = eventBuffer.Length * SimsPhenotype.MAX_LIMBS;
            using NativeParallelMultiHashMap<Entity, byte> RootPhenotypeToDetachedLimbIndicesLookup = new(maxDetachedLimbCount * 2, Allocator.TempJob);

            // Pre-allocate buffer with enough space for each parallel execution to have its own slice.
            // We need MAX_LIMBS + 1 to handle the worst case where we have MAX_LIMBS limbs in a chain
            // (the +1 accounts for the stack temporarily holding both parent and child during processing).
            int bufferSizePerEvent = SimsPhenotype.MAX_LIMBS + 1;
            using NativeArray<Entity> tempEntityBuffer = new(eventBuffer.Length * bufferSizePerEvent, Allocator.TempJob);

            JobHandle eventHandlerJobHandle = new JointBrokenEventHandler
            {
                Ecb = ecb.AsParallelWriter(),
                EventBuffer = eventBuffer.AsNativeArray(),
                ParentToChildrenLookup = parentToChildrenLookup.AsReadOnly(),
                RootPhenotypeEntityLookup = rootPhenotypeEntityLookup,
                LimbIndexLookup = limbIndexLookup,
                RootPhenotypeToDetachedLimbIndicesLookup = RootPhenotypeToDetachedLimbIndicesLookup.AsParallelWriter(),
                TempEntityBuffer = tempEntityBuffer,
                BufferSizePerEvent = bufferSizePerEvent
            }.ScheduleParallel(eventBuffer.Length, 64, buildLookupJobHandle);

            // Update detached limb data.
            new UpdateRootPhenotypeDetachedLimbsJob
            {
                RootPhenotypeToDetachedLimbIndicesLookup = RootPhenotypeToDetachedLimbIndicesLookup.AsReadOnly()
            }.ScheduleParallel(eventHandlerJobHandle).Complete();

            // Clear the event buffer before playing back the ECB to avoid invalidation.
            eventBuffer.Clear();

            ecb.Playback(state.EntityManager);
        }
    }

    [BurstCompile]
    [WithNone(typeof(DetachedLimbTag))] // Ignore already detached limbs.
    public partial struct BuildParentToChildLookupJob : IJobEntity
    {
        public NativeParallelMultiHashMap<Entity, Entity>.ParallelWriter ParentToChildLookup;

        public void Execute(Entity childEntity, in ParentLimb parentLimb)
        {
            ParentToChildLookup.Add(parentLimb.Value, childEntity);
        }
    }

    [BurstCompile]
    public partial struct JointBrokenEventHandler : IJobFor
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        [ReadOnly] public NativeArray<JointBrokenEvent> EventBuffer;
        [ReadOnly] public NativeParallelMultiHashMap<Entity, Entity>.ReadOnly ParentToChildrenLookup;
        [ReadOnly] public ComponentLookup<RootPhenotypeEntity> RootPhenotypeEntityLookup;
        [ReadOnly] public ComponentLookup<LimbIndex> LimbIndexLookup;
        public NativeParallelMultiHashMap<Entity, byte>.ParallelWriter RootPhenotypeToDetachedLimbIndicesLookup;
        [NativeDisableParallelForRestriction] public NativeArray<Entity> TempEntityBuffer;
        [ReadOnly] public int BufferSizePerEvent;

        public void Execute(int index)
        {
            JointBrokenEvent jointBrokenEvent = EventBuffer[index];
            Entity detachedLimb = jointBrokenEvent.DetachedLimbEntity;
            Entity rootPhenotypeEntity = RootPhenotypeEntityLookup[detachedLimb].Value;

            // Get this execution's slice of the buffer.
            int bufferStartIndex = index * BufferSizePerEvent;
            NativeSlice<Entity> myBuffer = TempEntityBuffer.Slice(bufferStartIndex, BufferSizePerEvent);

            // Use the slice as a stack.
            int stackIndex = 0;
            myBuffer[stackIndex++] = detachedLimb;

            while (stackIndex > 0)
            {
                Entity d = myBuffer[--stackIndex];

                // Tag the limb.
                Ecb.AddComponent(index, d, new DetachedLimbTag());

                // Add the detached limb index to the lookup.
                byte currentLimbIndex = LimbIndexLookup[d].Value;
                RootPhenotypeToDetachedLimbIndicesLookup.Add(rootPhenotypeEntity, currentLimbIndex);

                // Find and enqueue any child limbs.
                if (ParentToChildrenLookup.TryGetFirstValue(d, out var childLimb, out var iterator))
                {
                    do
                    {
                        myBuffer[stackIndex++] = childLimb;
                    }
                    while (ParentToChildrenLookup.TryGetNextValue(out childLimb, ref iterator));
                }
            }
        }
    }

    [BurstCompile]
    public partial struct UpdateRootPhenotypeDetachedLimbsJob : IJobEntity
    {
        [ReadOnly] public NativeParallelMultiHashMap<Entity, byte>.ReadOnly RootPhenotypeToDetachedLimbIndicesLookup;

        public void Execute(Entity rootPhenotypeEntity, ref DynamicBuffer<LimbStatus> limbStatuses, ref NeuralGraphRef neuralGraphRef, ref DynamicBuffer<EmitterState> emitterStates)
        {
            if (RootPhenotypeToDetachedLimbIndicesLookup.TryGetFirstValue(rootPhenotypeEntity, out byte limbIndex, out var iterator))
            {
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
                NativeArray<float> e = emitterStates.Reinterpret<float>().AsNativeArray();

                do
                {
                    // Update the on-board limb status.
                    LimbStatus limbStatus = limbStatuses[limbIndex];
                    limbStatus.AttachmentState = AttachmentState.Detached;
                    limbStatuses[limbIndex] = limbStatus;

                    // Zero out the emitter states for the detached limb.
                    CompiledNeuralGraph.ArraySlice sensorsSlice = sensorSlices[limbIndex];
                    CompiledNeuralGraph.ArraySlice actuatorSlice = actuatorSlices[limbIndex];
                    CompiledNeuralGraph.ArraySlice limbNeuronSlice = limbNeuronSlices[limbIndex];
                    ushort senStartIndex = (ushort)(sensorEmitterStartIndex + sensorsSlice.startIndex);
                    ushort actStartIndex = (ushort)(actuatorNeuronEmitterStartIndex + actuatorSlice.startIndex);
                    ushort lnStartIndex = (ushort)(limbNeuronEmitterStartIndex + limbNeuronSlice.startIndex);
                    for (int j = 0; j < sensorsSlice.count; j++) e[senStartIndex + j] = 0f;
                    for (int j = 0; j < actuatorSlice.count; j++) e[actStartIndex + j] = 0f;
                    for (int j = 0; j < limbNeuronSlice.count; j++) e[lnStartIndex + j] = 0f;
                }
                while (RootPhenotypeToDetachedLimbIndicesLookup.TryGetNextValue(out limbIndex, ref iterator));
            }
        }
    }
}
