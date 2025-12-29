# Developer Guide

## Requirements

- **Unity 6.3+**

## Project Architecture

The codebase is split into two main sections:

### 1. Platform-Agnostic Package (`mycoolfin.TheSimsulator`)
Pure C# package designed to be engine-agnostic, enabling use outside Unity if needed.

### 2. Unity Integration (`mycoolfin.TheSimsulator.UnityIntegration`)
Unity-specific implementation using ECS/DOTS (Data-Oriented Technology Stack).

Both sections follow a similar structure:
- **Core** - Shared/common functionality
- **Sims** - Sims EVC (Evolved Virtual Creatures) model implementation
- Additional model implementations can be added alongside Sims

## Code Organization

### Unity Integration Architecture

The Unity integration code uses the following architecture:

```
Assets/Scripts/
├── Core/                   # Shared Unity integration code
│   ├── ECS/                # Core ECS systems and components
│   │    ├── API/           # Shared API code for interacting with ECS
│   │    ├── Components/    # Shared ECS components
│   │    ├── Systems/       # Core ECS systems
│   │    ├── Builders/      # Shared entity construction utilities
│   ├── Evolution/          # Shared Evolution code
│   ├── UI/                 # Shared UI code
│   └── Utilities/          # Shared general utility code
│
└── Sims/                   # Sims-specific implementation
    ├── ECS/                # Sims ECS implementation
    │   ├── API/            # IECSAPI implementation
    │   ├── Components/     # Sims-specific components
    │   ├── Systems/        # Sims simulation systems
    │   └── Builders/       # Sims entity builders
    ├── Evolution/          # Concrete EvolutionSimulatorBase implementation
    └── UI/                 # Sims UI controller implementations
```

### Separation of Concerns

**ECS Layer**: All DOTS/ECS code is isolated in dedicated modules
- Systems process entities and components
- Builders create entities with appropriate components
- API interfaces provide clean access points

**MonoBehaviour Layer**: Traditional Unity scripts interact with ECS worlds via a model-specific `IECSAPI` implementation
- UI controllers
- Scene management
- User input handling

This separation allows the ECS code to remain pure and performant while providing a familiar Unity interface for higher-level logic.

## Key Concepts

### Entities
Creatures are represented as collections of entities:
- **RootPhenotype** - Top-level creature entity with creature metadata and neural network
- **Limb** - Body segments with physics properties, light sensors and contact sensors
- **Joint** - Connections between limbs with motors and joint angle sensors

### Neural Networks
Each creature has a compiled neural network that:
- Receives sensor inputs (joint angles, touch, light)
- Evaluates neuron functions
- Outputs actuator commands (joint motor forces)

### Evolution Pipeline
1. Generate random population
2. Evaluate fitness in simulation trials
3. Select top performers
4. Breed and mutate to create next generation
5. Repeat

## Development Workflow

### Opening the Project
1. Install Unity 6.3 or later
2. Clone the repository
3. Open the project in Unity

### Building
Use Unity's built-in build system:
- **File → Build Settings**
- Select target platform
- Click **Build** or **Build and Run**

## Code Style

- Follow existing conventions
- Use Burst compilation attributes where applicable (`[BurstCompile]`)
- Document complex code
- Use meaningful component and system names

## Performance Considerations

- This project uses **DOTS/ECS for performance** - avoid GameObject-heavy patterns
- Burst compiler optimizes hot paths - ensure systems are Burst-compatible
- Entity queries and parallel jobs are used extensively

## Headless Mode

The project supports headless execution for batch simulations, see the [Headless Mode Guide](/docs/HEADLESS.md) for details.

## Resources

- [Unity ECS Documentation](https://docs.unity3d.com/Packages/com.unity.entities@latest)
- [Karl Sims' Original Paper](https://www.karlsims.com/papers/siggraph94.pdf)
- [User Guide](/docs/USER_GUIDE.md)
- [Discord Community](https://discord.gg/ygxhStrE)
