using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using mycoolfin.TheSimsulator.Sims.Genotype;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(UpdateSensorsSystemGroup))]
[UpdateBefore(typeof(UpdateActuatorsSystemGroup))]
public partial struct UpdateNeuronsSystem : ISystem
{
    private BufferLookup<LimbStatus> limbStatusLookup;

    public void OnCreate(ref SystemState state)
    {
        limbStatusLookup = state.GetBufferLookup<LimbStatus>(true);

        state.RequireForUpdate<NeuralGraphRef>();
        state.RequireForUpdate<EmitterState>();
        state.RequireForUpdate<LimbStatus>();
    }

    public void OnUpdate(ref SystemState state)
    {
        limbStatusLookup.Update(ref state);

        state.Dependency = new UpdateNeuronJob
        {
            LimbStatusLookup = limbStatusLookup,
            ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
            DeltaTime = SystemAPI.Time.DeltaTime,
            InverseDeltaTime = 1f / math.max(SystemAPI.Time.DeltaTime, 1e-5f),
        }.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct UpdateNeuronJob : IJobEntity
{
    [ReadOnly] public BufferLookup<LimbStatus> LimbStatusLookup;
    public float ElapsedTime;
    public float DeltaTime;
    public float InverseDeltaTime;

    public void Execute(Entity rootPhenotypeEntity, in LimbCount limbCount, in PhenotypeCreatedAt createdAt, in NeuralGraphRef graphRef, ref DynamicBuffer<EmitterState> emitterStates)
    {
        DynamicBuffer<LimbStatus> limbStatuses = LimbStatusLookup[rootPhenotypeEntity];
        float phenotypeElapsedTime = ElapsedTime - createdAt.Value;
        ref readonly BlobAssetReference<CompiledNeuralGraph> graph = ref graphRef.Value;
        ref BlobArray<CompiledNeuralGraph.ArraySlice> actuatorSlices = ref graph.Value.ActuatorSlices;
        ref BlobArray<CompiledNeuralGraph.ArraySlice> limbNeuronSlices = ref graph.Value.LimbNeuronSlices;
        ref BlobArray<CompiledNeuralGraph.ActuatorMeta> actuatorsMeta = ref graph.Value.Actuators;
        ref BlobArray<CompiledNeuralGraph.NeuronMeta> limbNeuronsMeta = ref graph.Value.LimbNeurons;
        ref BlobArray<CompiledNeuralGraph.NeuronMeta> brainNeuronsMeta = ref graph.Value.BrainNeurons;
        ushort actuatorNeuronEmitterStartIndex = graph.Value.ActuatorNeuronEmitterStartIndex;
        ushort limbNeuronEmitterStartIndex = graph.Value.LimbNeuronEmitterStartIndex;
        ushort brainNeuronEmitterStartIndex = graph.Value.BrainNeuronEmitterStartIndex;
        NativeArray<float> e = emitterStates.Reinterpret<float>().AsNativeArray(); // Reinterpret as float for potentially better Burst performance.

        // Update actuator neurons and limb neurons.
        for (ushort limbIndex = 0; limbIndex < limbCount.Value; limbIndex++)
        {
            if (limbStatuses[limbIndex].AttachmentState == AttachmentState.Detached)
                continue;

            CompiledNeuralGraph.ArraySlice actuatorSlice = actuatorSlices[limbIndex];
            CompiledNeuralGraph.ArraySlice limbNeuronSlice = limbNeuronSlices[limbIndex];
            ushort actStartIndex = (ushort)(actuatorNeuronEmitterStartIndex + actuatorSlice.startIndex);
            ushort lnStartIndex = (ushort)(limbNeuronEmitterStartIndex + limbNeuronSlice.startIndex);
            
            for (int i = 0; i < actuatorSlice.count; i++)
                UpdateNeuron(in graph, ref e, (ushort)(actStartIndex + i),
                    in actuatorsMeta[actuatorSlice.startIndex + i].neuronMeta,
                    phenotypeElapsedTime, DeltaTime, InverseDeltaTime);
            for (int i = 0; i < limbNeuronSlice.count; i++)
                UpdateNeuron(in graph, ref e, (ushort)(lnStartIndex + i),
                    in limbNeuronsMeta[limbNeuronSlice.startIndex + i],
                    phenotypeElapsedTime, DeltaTime, InverseDeltaTime);
        }

        // Update brain neurons.
        for (int i = 0; i < graph.Value.BrainNeuronCount; i++)
            UpdateNeuron(in graph, ref e, (ushort)(brainNeuronEmitterStartIndex + i),
                in brainNeuronsMeta[i],
                phenotypeElapsedTime, DeltaTime, InverseDeltaTime);
    }

    private static void UpdateNeuron(in BlobAssetReference<CompiledNeuralGraph> graph, ref NativeArray<float> e, ushort emitterIndex, in CompiledNeuralGraph.NeuronMeta neuronMeta, float phenotypeElapsedTime, float DeltaTime, float InverseDeltaTime)
    {
        float previousValue = e[emitterIndex];
        float newValue = Activate(neuronMeta.activationFunction, in neuronMeta, in graph, e, previousValue, phenotypeElapsedTime, DeltaTime, InverseDeltaTime);
        e[emitterIndex] = newValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Activate(ActivationFunction fn, in CompiledNeuralGraph.NeuronMeta neuronMeta, in BlobAssetReference<CompiledNeuralGraph> g, in NativeArray<float> e, float previousValue, float t, float dt, float invDt)
    {
        ushort start = neuronMeta.firstInput;
        byte count = neuronMeta.inputCount;

        return fn switch
        {
            ActivationFunction.Abs => Abs(V(0, start, count, g, e)),
            ActivationFunction.Atan => Atan(V(0, start, count, g, e)),
            ActivationFunction.Cos => Cos(V(0, start, count, g, e), V(1, start, count, g, e), V(2, start, count, g, e), t),
            ActivationFunction.Differentiate => Differentiate(V(0, start, count, g, e), V(1, start, count, g, e), previousValue, invDt),
            ActivationFunction.Divide => Divide(V(0, start, count, g, e), V(1, start, count, g, e)),
            ActivationFunction.Expt => Expt(V(0, start, count, g, e)),
            ActivationFunction.GreaterThan => GreaterThan(V(0, start, count, g, e), V(1, start, count, g, e)),
            ActivationFunction.If => If(V(0, start, count, g, e), V(1, start, count, g, e), V(2, start, count, g, e)),
            ActivationFunction.Integrate => Integrate(V(0, start, count, g, e), V(1, start, count, g, e), previousValue, dt),
            ActivationFunction.Interpolate => Interpolate(V(0, start, count, g, e), V(1, start, count, g, e), V(2, start, count, g, e)),
            ActivationFunction.Log => Log(V(0, start, count, g, e), V(1, start, count, g, e), V(2, start, count, g, e)),
            ActivationFunction.Max => Max(V(0, start, count, g, e), V(1, start, count, g, e), V(2, start, count, g, e)),
            ActivationFunction.Memory => Memory(V(0, start, count, g, e), V(1, start, count, g, e), previousValue),
            ActivationFunction.Min => Min(V(0, start, count, g, e), V(1, start, count, g, e), V(2, start, count, g, e)),
            ActivationFunction.OscillateSaw => OscillateSaw(V(0, start, count, g, e), V(1, start, count, g, e), t),
            ActivationFunction.OscillateWave => OscillateWave(V(0, start, count, g, e), V(1, start, count, g, e), V(2, start, count, g, e), t),
            ActivationFunction.Product => Product(V(0, start, count, g, e), V(1, start, count, g, e), V(2, start, count, g, e)),
            ActivationFunction.Sigmoid => Sigmoid(V(0, start, count, g, e), V(1, start, count, g, e), V(2, start, count, g, e)),
            ActivationFunction.SignOf => SignOf(V(0, start, count, g, e), V(1, start, count, g, e), V(2, start, count, g, e)),
            ActivationFunction.Sin => Sin(V(0, start, count, g, e), V(1, start, count, g, e), V(2, start, count, g, e), t),
            ActivationFunction.Smooth => Smooth(V(0, start, count, g, e), V(1, start, count, g, e), previousValue, dt),
            ActivationFunction.Sum => Sum(V(0, start, count, g, e), V(1, start, count, g, e), V(2, start, count, g, e)),
            ActivationFunction.SumThreshold => SumThreshold(V(0, start, count, g, e), V(1, start, count, g, e), V(2, start, count, g, e)),
            _ => V(0, start, count, g, e)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float V(int inputIndex, ushort inputStartIndex, byte inputCount, in BlobAssetReference<CompiledNeuralGraph> graph, in NativeArray<float> emitterStates)
    {
        if (inputIndex >= inputCount) return 0f;
        ushort emitterIndex = graph.Value.Inputs[inputStartIndex + inputIndex].sourceEmitterGlobalIndex;
        return emitterIndex == CompiledNeuralGraph.BIAS_INDEX ? 1f : emitterStates[emitterIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float OscillateSaw(float a, float b, float t)
    {
        return math.select(2 * math.frac(t / ((1f + a) * b)) - 1f, 0f, a <= -1f || b == 0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Abs(float a)
    {
        return math.abs(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Atan(float a)
    {
        return math.atan(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Cos(float a, float b, float c, float t)
    {
        return math.clamp(math.cos((t * a) + b) * c, -1f, 1f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Differentiate(float a, float b, float previousValue, float invDt)
    {
        return math.clamp((a - previousValue) * invDt * b, -1f, 1f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Divide(float a, float b)
    {
        return math.select(a / b, 0f, b == 0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Expt(float a)
    {
        return math.clamp(math.exp(a), -1f, 1f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float GreaterThan(float a, float b)
    {
        return math.select(0f, 1f, a > b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float If(float a, float b, float c)
    {
        return math.select(c, b, a > 0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Integrate(float a, float b, float previousValue, float dt)
    {
        return math.clamp(previousValue + (a * dt) - (math.abs(b) * previousValue * dt), -1f, 1f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Interpolate(float a, float b, float c)
    {
        return math.lerp(a, b, c);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Log(float a, float b, float c)
    {
        return math.log(math.max(1e-5f, a + b + c));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Max(float a, float b, float c)
    {
        return math.max(a + b + c, 0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Memory(float a, float b, float previousValue)
    {
        return math.select(previousValue, b, a > 0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Min(float a, float b, float c)
    {
        return math.min(a + b + c, 0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float OscillateWave(float a, float b, float c, float t)
    {
        return math.clamp(math.sin(a * t + b) * c, -1f, 1f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Product(float a, float b, float c)
    {
        return a * b * c;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Sigmoid(float a, float b, float c)
    {
        return 1f / (1f + math.exp(-(a + b + c)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float SignOf(float a, float b, float c)
    {
        return math.sign(a + b + c);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Sin(float a, float b, float c, float t)
    {
        return math.clamp(math.sin((t * a) + b) * c, -1f, 1f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Smooth(float a, float b, float previousValue, float dt)
    {
        return math.lerp(previousValue, a, math.saturate(b * dt));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Sum(float a, float b, float c)
    {
        return a + b + c;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float SumThreshold(float a, float b, float c)
    {
        return math.min(a + b, c);
    }
}
