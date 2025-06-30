using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(PhysicsSimulationGroup))]
[UpdateAfter(typeof(PhysicsCreateBodyPairsGroup))]
[UpdateBefore(typeof(PhysicsCreateContactsGroup))]
public partial struct DisableInterPhenotypePairsSystem : ISystem
{
    private ComponentLookup<PhenotypeGid> PhenotypeGidLookup;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhenotypeGid>();
        PhenotypeGidLookup = state.GetComponentLookup<PhenotypeGid>(isReadOnly: true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {        
        PhysicsWorldSingleton worldSingleton = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW;
        SimulationSingleton simulation = SystemAPI.GetSingleton<SimulationSingleton>();

        if (simulation.Type == SimulationType.NoPhysics)
            return;

        PhenotypeGidLookup.Update(ref state);

        state.Dependency = new DisableInterPhenotypePairsJob
        {
            PhenotypeGidLookup = PhenotypeGidLookup,
            NumDynamicBodies = worldSingleton.PhysicsWorld.NumDynamicBodies
        }.Schedule(simulation, ref worldSingleton.PhysicsWorld, state.Dependency);
    }

    [BurstCompile]
    struct DisableInterPhenotypePairsJob : IBodyPairsJob
    {
        [ReadOnly] public ComponentLookup<PhenotypeGid> PhenotypeGidLookup;
        public int NumDynamicBodies;

        public void Execute(ref ModifiableBodyPair pair)
        {
            bool isDynamicDynamic = pair.BodyIndexA < NumDynamicBodies && pair.BodyIndexB < NumDynamicBodies;
            if (!isDynamicDynamic)
                return;

            Entity entityA = pair.EntityA;
            Entity entityB = pair.EntityB;

            if (!PhenotypeGidLookup.HasComponent(entityA) || !PhenotypeGidLookup.HasComponent(entityB))
                return;

            ulong idA = PhenotypeGidLookup[entityA].Value;
            ulong idB = PhenotypeGidLookup[entityB].Value;

            if (idA != idB)
                pair.Disable();
        }
    }
}
