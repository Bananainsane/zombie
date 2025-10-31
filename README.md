# Zombie Survival Game

A multiplayer zombie survival game built with Unity and Unity Netcode for networked gameplay.

**Playable Build**: The compiled `.exe` is located in `zombies/aflevering`

## Core Gameplay

Players work together to survive increasingly difficult waves of zombies. Eliminate all zombies in a round to progress. Game ends when all players die.

## Zombie AI State Machine

Zombies operate using a three-state AI system:

- **Searching**: Wander the map in coordinated patterns using sector-based distribution
- **Alerted**: Investigate last known player position when notified by nearby zombies
- **Chasing**: Actively pursue players using vision/hearing detection with flanking tactics

## Key Systems

- **Flocking Behavior**: Zombies coordinate movement using separation, alignment, and cohesion forces for realistic horde behavior
- **Sensory Detection**: Vision cones and hearing radius with player noise levels affecting detection range
- **Round Manager**: Spawns progressively more zombies each round with wave-based difficulty scaling
- **Player State**: Health system with death/respawn mechanics and team wipe detection
- **Networked Multiplayer**: Full server-authoritative architecture using Unity Netcode

Built with Unity NavMesh for pathfinding and NetworkBehaviour for synchronized multiplayer state.
