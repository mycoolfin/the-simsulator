using System;
using System.Linq;
using System.Collections.Generic;

namespace mycoolfin.TheSimsulator.Sims
{
    public class SimsPhenotypeFactory : IPhenotypeFactory<SimsGenotype, SimsPhenotype>
    {
        public SimsPhenotype ConstructPhenotype(SimsGenotype genotype)
        {
            Dictionary<Limb, List<Limb>> parentToChildLimbsMap = new();
            Dictionary<Limb, Limb> childToParentLimbMap = new();
            Dictionary<ISignalReceiver, InputSetDefinition> receiverToInputDefinitionSetMap = new();

            // Create the brain.
            Brain brain = BrainCreator.CreateBrain(genotype.NeuronDefinitions.Where(nd => nd.ContainerGid == SimsGenotype.BRAIN_GID), receiverToInputDefinitionSetMap);

            // Create the limbs.
            List<Limb> limbs = LimbCreator.CreateLimbs(genotype, parentToChildLimbsMap, childToParentLimbMap, receiverToInputDefinitionSetMap);

            // Wire up the nervous system.
            WiringTool.WireUpNervousSystem(
                brain,
                limbs,
                parentToChildLimbsMap,
                childToParentLimbMap,
                receiverToInputDefinitionSetMap
            );

            return new SimsPhenotype(brain, limbs);
        }
    }
}
