# Overseer AI Director Pacing

This document outlines the design and mathematical implementation of the Overseer AI dynamic spawn director and intensity management curves.

---

## 1. Pacing State Machine

The Overseer director regulates combat tension through an automated wave cycle modelled on the classic AI Director architecture:

```
[BuildUp] -> (Intensity >= 0.70) -> [Peak] -> (Duration elapsed) -> [Relax] -> (Rest finished) -> [BuildUp]
```

- **BuildUp Phase:** Spawns tactical patrols and scout drones ahead of the player's path. Tension rises dynamically from damage events, shots fired, and active enemy sights.
- **Peak Phase:** Intensity reaches or exceeds 0.70. Maximum concurrent unit caps are engaged. Spawns are biased heavily toward combat Sentinels and aggressive swarm units.
- **Relax Phase:** After sustained peak combat, the director enters a pacification cooldown lasting 20–35 seconds. Threat spawns cease, allowing player recovery and scavenging.

---

## 2. Player Profiling & Adaptive Counters

The Overseer tracks player behavior using exponentially-weighted moving averages (time constant $\tau = 30$ seconds):

1. **Camping Profile:** If the player remains stationary within small radii, high-mobility Hunter Drones with auditory tracking are dispatched to flush cover.
2. **Physics Barricading Profile:** If physics interactions exceed baseline thresholds, Saboteur units are deployed to manipulate, unseat, and roll away physics obstacles.
3. **High Aggression Profile:** High weapon discharge rates trigger armored Sentinel flanking maneuvers utilizing cover nodes.

---

## 3. Dynamic Spawn Scoring

Candidate vents and spawn nodes are scored before entity instantiation:
```
Score = 2.0 - |PathDistance - 20| / 10 + (AheadDot * 1.2) - (ClearedInfluence * 2.0) - (RecencyPenalty * 2.0)
```
Spawns behind the player's direct field-of-view and along forward navigation corridors are prioritized to eliminate visual pop-in artifacts.
