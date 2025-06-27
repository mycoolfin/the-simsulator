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
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {        
        PhysicsWorldSingleton worldSingleton = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW;
        SimulationSingleton simulationSingleton = SystemAPI.GetSingletonRW<SimulationSingleton>().ValueRW;

        state.Dependency = new DisableInterPhenotypePairsJob
        {
            PhenotypeLookup = state.GetComponentLookup<PhenotypeId>(isReadOnly: true),
            NumDynamicBodies = worldSingleton.PhysicsWorld.NumDynamicBodies
        }.Schedule(simulationSingleton, ref worldSingleton.PhysicsWorld, state.Dependency);
    }

    [BurstCompile]
    struct DisableInterPhenotypePairsJob : IBodyPairsJob
    {
        [ReadOnly] public ComponentLookup<PhenotypeId> PhenotypeLookup;
        public int NumDynamicBodies;

        public void Execute(ref ModifiableBodyPair pair)
        {
            bool isDynamicDynamic = pair.BodyIndexA < NumDynamicBodies && pair.BodyIndexB < NumDynamicBodies;
            if (!isDynamicDynamic)
                return;

            Entity entityA = pair.EntityA;
            Entity entityB = pair.EntityB;

            if (!PhenotypeLookup.HasComponent(entityA) || !PhenotypeLookup.HasComponent(entityB))
                return;

            int idA = PhenotypeLookup[entityA].Value;
            int idB = PhenotypeLookup[entityB].Value;

            if (idA != idB)
                pair.Disable();
        }
    }
}
