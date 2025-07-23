using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace mycoolfin.TheSimsulator.Sims.Phenotype
{
    using Core.Phenotype;
    using Genotype;

    public class SimsPhenotypeFactory : IPhenotypeFactory<SimsGenotype, SimsPhenotype>
    {
        private readonly int constructYieldAfterCount;

        public SimsPhenotypeFactory(int constructYieldAfterCount = 100)
        {
            this.constructYieldAfterCount = constructYieldAfterCount;
        }

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

        public virtual IEnumerator ConstructPhenotypes(IReadOnlyList<SimsGenotype> genotypes, IPhenotypeFactory<SimsGenotype, SimsPhenotype>.OnPhenotypeConstructedDelegate onConstructed)
        {
            for (int i = 0; i < genotypes.Count; i++)
            {
                onConstructed(i, ConstructPhenotype(genotypes[i]));
                if (i % constructYieldAfterCount == 0 && i > 0)
                    yield return null;
            }
        }
    }
}
