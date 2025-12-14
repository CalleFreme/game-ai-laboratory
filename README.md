# A course repo for learning video game AI techniques with C# and Unity

## Project folder structure

```
Assets/
├── Common/                                    # Shared, reusable scripts
│   └── Scripts/
│       └── Common/
│           ├── SimplePlayerController.cs
│           └── SimpleCameraFollow.cs
│
├── Demos/
│   ├── Day01_FSM/                             # Finite State Machine demo
│   │   └── Scripts/
│   │       └── SimpleChaser.cs                # Simple chasing enemy
│   │
│   ├── Day02_AStar/                           # Grid-based A* demo
│   │   └── Scripts/
│   │       ├── Grid/
│   │       │   └── GridManager.cs             # Manages the grid layout
│   │       └── Pathfinding/
│   │           └── Pathfinder.cs               # Implements A* pathfinding algorithm
│   └── Day03_SteeringSwarm/                    # Steering behaviors demo
│       └── Scripts/
│           ├── AI/
│           |   └── SteeringAgent.cs			 # Steering agent base class
|		    └── Agents/
│               └── AgentSpawner.cs        # Agent that seeks a target
```

## Overview

This repository serves as a practical resource for learning various AI techniques used in video game development, specifically utilizing C# and Unity. The project is structured to facilitate hands-on learning through demos and tutorials.

## Getting Started

To get started with this project, clone the repository and open it in Unity. You can explore the demos to see how different AI techniques are implemented and experiment with the provided scripts.

## Demos/Lab solutions

### Day 01 - Finite State Machine (FSM)

### Day 02 - Grid-based A* Pathfinding

### Day 03 - Steering, Seeking, Swarming

- Location: `Assets/Demos/Day03_SteeringSwarm`
- `SteeringAgent` blends seek/arrive, separation, obstacle avoidance, and ground-following forces to keep the swarm cohesive while navigating obstacles.
- `AgentSpawner` populates the scene with a configurable flock, optionally wiring each agent to a shared target for synchronized steering drills.

TODO:
- Add Cohesion and Alignment behaviors to the `SteeringAgent` to complete the classic Boids model.
- Fix: Front agent shouldn't care about agents behind it when calculating separation force. Agents moving in the same direction are constantly "steering away" from each other, wasting energy.

### Day 04 - Behavior Trees

### Day 05 - Goal Oriented Action Planning (GOAP)


## Contributing

Contributions are welcome! If you have suggestions for improvements or additional demos, please feel free to submit a pull request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.