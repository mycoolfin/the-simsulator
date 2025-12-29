# Parameters Guide

## Overview

The Simsulator exposes two categories of parameters that control the evolutionary process and creature characteristics:

### Core Model Parameters
These parameters define the fundamental structure and behavior of creatures and their evolutionary mechanisms. They are **not adjustable via the user interface** but are exposed in source code for advanced users. These parameters are less commonly modified, or in some cases, changing them can break compatibility with existing creature populations. Most of these parameters were not specified in Sims' original paper and have been chosen based on qualitative assessment and system stability requirements.

### Evolution Run Parameters
These are the parameters users typically adjust when configuring an evolution run through the UI. They include population size, fitness criteria, simulation duration, and other high-level configuration options that don't affect the fundamental creature structure. See the [User Guide](USER_GUIDE.md#evolution-parameters) for details on these parameters.

---

## Core Model Parameters

The following parameters are defined in the source code and require recompilation to modify. Only the reproduction probabilities match those specified in Sims' original paper—all others have been determined through experimentation.

### Genotype Structure

These parameters control the genetic representation of creatures before they are instantiated as physical entities.

| Parameter | Range/Value | Location | Description |
|-----------|-------------|----------|-------------|
| **Nodes per genotype** | [1, 5] | [MIN_NODES](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/SimsGenotype.cs#L10), [MAX_NODES](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/SimsGenotype.cs#L11) | Maximum number of nodes that can be encoded in a creature's genotype. Each node represents one or more potential limbs in the phenotype. |
| **Brain neurons** | [0, 10] | [MIN_BRAIN_NEURON_DEFINITIONS](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/SimsGenotype.cs#L13), [MAX_BRAIN_NEURON_DEFINITIONS](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/SimsGenotype.cs#L14) | Number of neurons available in the creature's centralised neuron set. |
| **Node dimensions (W/H/L)** | [0.2, 2.0] m | [MIN_DIMENSION](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Components/Node.cs#L10), [MAX_DIMENSION](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Components/Node.cs#L11) | Allowable range for the width, height, and length of each limb node. |
| **Recursive limit per node** | [1, 10] | [MIN_RECURSIVE_LIMIT](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Components/Node.cs#L12), [MAX_RECURSIVE_LIMIT](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Components/Node.cs#L13) | Controls how many times a node can recursively instantiate itself during phenotype construction. |
| **Neurons per node** | [0, 10] | [MIN_NEURON_DEFINITIONS](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Components/Node.cs#L14), [MAX_NEURON_DEFINITIONS](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Components/Node.cs#L15) | Number of local neurons that can be associated with each limb node. |
| **Outgoing connections per node** | [0, 4] | [MIN_CONNECTIONS](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Components/Node.cs#L16), [MAX_CONNECTIONS](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Components/Node.cs#L17) | Maximum number of child nodes that can connect to a parent node, determining the branching complexity of the creature's structure. |
| **Child scale factor** | [0.5, 2.0] | [MIN_SCALE](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Components/Connection.cs#L16), [MAX_SCALE](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Components/Connection.cs#L17)  | Multiplier applied to child limb dimensions relative to their parent. Values < 1.0 create tapering structures; values > 1.0 create expanding structures. |

### Phenotype Constraints

These limits are enforced during the construction of the physical creature from the genotype.

| Parameter | Range/Value | Location | Description |
|-----------|-------------|----------|-------------|
| **Total constructed limbs** | [1, 20] | [MAX_LIMBS](/Packages/com.mycoolfin.thesimsulator/Sims/Phenotype/SimsPhenotype.cs#L10) | Hard limit on the number of physical limbs created during phenotype construction. Breadth-first search traversal of the genotype tree stops when this limit is reached. |
| **Constructed limb dimensions** | [0.05, 10] m | [MIN_LIMB_DIMENSION](/Packages/com.mycoolfin.thesimsulator/Sims/Phenotype/SimsPhenotype.cs#L11), [MAX_LIMB_DIMENSION](/Packages/com.mycoolfin.thesimsulator/Sims/Phenotype/SimsPhenotype.cs#L12) | Physical dimensions of instantiated limbs are clamped to this range regardless of genotype specifications. Prevents extreme sizes that can cause physics instability. |

### Reproduction Probabilities

These probabilities determine which genetic operator is used when creating offspring. They must sum to 100%.

| Parameter | Value | Location | Description |
|-----------|-------|----------|-------------|
| **Asexual** | 40% | [AsexualProbability](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Creation/SimsGenotypeFactory.cs#L11) | Offspring is a mutated copy of a single parent. Allows incremental refinement of successful designs. |
| **Crossover** | 30% | [CrossoverProbability](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Creation/SimsGenotypeFactory.cs#L12) | Offspring combines genetic material from two parents through subtree exchange. Enables mixing of successful traits. |
| **Grafting** | 30% | [GraftingProbability](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Creation/SimsGenotypeFactory.cs#L13) | Offspring is created by grafting a subtree from one parent onto another parent's structure. Creates novel structural combinations. |

### Mutation Parameters (Gaussian)

All mutations use Gaussian (normal) distributions with the specified standard deviations (σ). The mean is always the current value.

| Parameter | Standard Deviation (σ) | Location | Description |
|-----------|----------------------|----------|-------------|
| **Dimension mutation** | 0.1 × *d* | [sigma](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Mutation/NodeMutator.cs#L34) | Standard deviation for mutating limb dimensions, where *d* is the current dimension value. Larger limbs experience proportionally larger mutations. |
| **Recursive limit mutation** | 1.0 | [sigma](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Mutation/NodeMutator.cs#L51) | Standard deviation for changes to node recursion depth. When rounded, results in integer changes of typically ±1 or ±2. |
| **Joint angle limit mutation** | 0.05 × *θ*<sub>max</sub> | [sigma](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Mutation/JointDefinitionMutator.cs#L34) | Standard deviation for joint angle range mutations, where *θ*<sub>max</sub> is the size of the angle range. |
| **Signal input weight mutation** | 0.1 | [sigma](/Packages/com.mycoolfin.thesimsulator/Sims/Genotype/Mutation/SignalInputDefinitionMutator.cs#L28) | Standard deviation for neural network connection weights. Fixed absolute value since weights are typically normalized. |

### Physics Parameters

These parameters control the physical simulation of creature joints and motors.

| Parameter | Value | Location | Description |
|-----------|-------|----------|-------------|
| **Motor spring frequency** | 10.0 Hz | [MOTOR_SPRING_FREQUENCY](/Assets/Scripts/Sims/ECS/Builders/JointEntityBuilder.cs#L37) | Natural frequency of the spring system used for joint motors. Higher values create stiffer, more responsive joints. |
| **Motor damping ratio** | 0.9 | [MOTOR_DAMPING_RATIO](/Assets/Scripts/Sims/ECS/Builders/JointEntityBuilder.cs#L38) | Damping ratio for joint motors. Values < 1.0 are underdamped (oscillatory), = 1.0 is critically damped, > 1.0 is overdamped (sluggish). |
