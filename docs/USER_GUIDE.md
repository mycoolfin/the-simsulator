# User Guide

Welcome to The Simsulator! This guide will help you create, evolve, and explore virtual creatures inspired by Karl Sims' groundbreaking work in evolutionary computation.

## Table of Contents

- [Getting Started](#getting-started)
- [Evolution Basics](#evolution-basics)
- [Trial Types](#trial-types)
- [Evolution Parameters](#evolution-parameters)
- [Typical Evolution Progression](#typical-evolution-progression)
- [Tips & Best Practices](#tips--best-practices)

## Getting Started

### First Launch

1. Download and extract the [latest release]((https://github.com/mycoolfin/the-simsulator/releases/latest)).
2. Run the executable for your platform:
   - **macOS**: Double-click `TheSimsulator.app`
   - **Windows**: Double-click `TheSimsulator.exe`
   - **Linux**: Run `./TheSimsulator.x86_64`

### Controls

- **Move** - WASD
- **Jump** - Space
- **Sprint** - Shift
- **Select/Activate** - Mouse left click
- **Grab** - Mouse left drag

### Your First Evolution

1. Hit the big, circular green button on the control panel.
2. Watch as creatures are randomly generated and evaluated.
3. After each generation, the best performers breed to create the next generation.
4. Observe how creatures improve over time!

## Evolution Basics

### How Evolution Works

The Simsulator uses a genetic algorithm inspired by natural selection:

1. **Random Generation**: Initial population created with random body structures and neural networks
2. **Evaluation**: Each creature is placed in the trial environment and evaluated for fitness
3. **Selection**: Top performers are selected as parents
4. **Reproduction**: Selected creatures' genes are combined and randomly mutated to create offspring
5. **Repeat**: New generation is evaluated, and the cycle continues

### Creature Anatomy

Each creature consists of:
- **Limbs**: Connected rigid parts
- **Joints**: Connections between segments allowing movement
- **Neural Network**: Brain that controls joint movements based on sensors
- **Sensors**: Provide input to the neural network about the environment, e.g. joint angle, touch pressure, light intensity

### Genetic Encoding

Creatures' genes encode:
- Body structure and proportions
- Joint types and constraints
- Neural network architecture and weights
- Sensor placement and types

## Trial Types

### Ground Distance

**Goal**: Travel as far as possible on flat ground

**What to Expect**:
- Creatures evolve walking, crawling, or rolling behaviors
- Leg-like appendages often emerge
- Coordinated rhythmic movements develop
- Good for beginners

**Tip**: Use default parameters for best results

### Water Distance

**Goal**: Travel as far as possible in water

**What to Expect**:
- Swimming and paddling behaviors emerge
- Flatter, fin-like appendages develop
- Different movement patterns than ground locomotion
- Slightly more challenging than ground trials

**Tip**: May require more generations to see good swimmers

### Ground Light Following

**Goal**: Move toward and follow a light source on ground

**What to Expect**:
- Creatures develop directional locomotion
- Seeking and tracking behaviors emerge
- Requires photosensors to detect light
- Preference for 3+ "legs" to help with quick turns

**Tip**: Increase assessment time to 15+ seconds for better evaluation

### Water Light Following

**Goal**: Move toward and follow a light source in water

**What to Expect**:
- Combines swimming and navigation challenges
- Most complex trial type
- Can produce sophisticated behaviors
- Takes longest to evolve successfully

**Tip**: Increase assessment time to 20+ seconds for better evaluation

## Evolution Parameters

### Population Size

**Range**: 50+  
**Default**: 100  
**Impact**:  
- Larger = More genetic diversity, better results, slower generations
- Smaller = Faster generations, less diversity, may get stuck in local optima

**Recommendations**:
- Quick experiments: 100-200
- Standard runs: 500-1000

### Max Generations

**Range**: 1+  
**Default**: 100  
**Impact**: Total evolution time and quality of final results

**Recommendations**:
- Quick test: 50
- Standard run: 100-200

### Survival Rate

**Range**: 0.0-1.0  
**Default**: 0.2  
**Impact**: 
- Higher = More parents, less selection pressure, slower evolution
- Lower = Fewer parents, stronger selection, risk of losing diversity

**Recommendations**:
- Aggressive: 0.1
- Balanced: 0.2
- Conservative: 0.3+

### Mutation Rate

**Range**: 0.0+  
**Default**: 1.0  
**Impact**:
- Higher = More variation, exploration, possible disruption of good solutions
- Lower = Refinement of existing solutions, less exploration

**Recommendations**:
- Standard: 1.0
- Exploration: 2.0-5.0

### Settle Time

**Range**: 0.0+ seconds  
**Default**: 10.0  
**Impact**:  
- Higher = Slower runs, more time for creatures to stabilise
- Lower = Faster runs, more "cheaty" behaviour (like falling over instead of walking)

**Recommendations**:
- Most trials: 5.0-10.0 seconds

### Assessment Time

**Range**: 0.0+ seconds  
**Default**: 10.0  
**Impact**: 
- Longer = Slower runs, more accurate fitness evaluation
- Shorter = Faster runs, less reliable fitness scores

**Recommendations**:
- Distance trials: 5-10 seconds
- Light following: 20-30 seconds

## Typical Evolution Progression

**Early Generations (1-10)**:
- Mostly random thrashing and twitching
- Occasional lucky movers
- High fitness variance

**Middle Generations (10-30)**:
- Coordinated movements emerge
- Clear body plans develop
- Fitness improvements plateau temporarily

**Late Generations (30+)**:
- Refined, efficient behaviors
- Specialized body structures
- Incremental fitness improvements

## Tips & Best Practices

### For Best Results

1. **Start Simple**: Begin with default parameters
2. **Experiment**: Try different parameter combinations to learn their effects
3. **Re-evolve**: Save creatures from previous runs and try evolving them under new conditions
5. **Share Discoveries**: Post screenshots and results in the Discord community

### Performance Optimization

- Close other applications during fast-forward evolution
- Use headless mode for long experiments (see [Headless Mode Guide](HEADLESS.md))

---

**Happy Evolving!**

Have questions? Join our [Discord](https://discord.gg/ygxhStrE) or open an issue on [GitHub](https://github.com/mycoolfin/the-simsulator/issues).
