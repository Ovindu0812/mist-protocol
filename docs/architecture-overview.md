# 🏗️ Mist Protocol - Technical Architecture & Systems Specification

## 1. Architectural Overview
Mist Protocol is built using Unity 6 (URP) and adheres to clean, decoupled, component-oriented software architecture principles. Systems communicate primarily through C# events (`GameEvents`, `Action<T>`), explicit dependency injection interfaces (`IPlayerState`, `IInteractable`), and centralized singletons where appropriate (`GameManager`, `NavGrid`, `ProceduralAudio`).

```
┌────────────────────────────────────────────────────────────────────────┐
│                          Core GameManager                              │
│         (Game Loop, Objective Tracker, Event Broker)                   │
└────────────┬─────────────────────────────┬─────────────────────────────┘
             │                             │
    ┌────────▼────────┐           ┌────────▼────────┐
    │   AI Subsystem  │           │ Player & Combat │
    │ (GOAP / Sensors)│           │ (Weapons / HUD) │
    └────────┬────────┘           └────────┬────────┘
             │                             │
    ┌────────▼─────────────────────────────▼────────┐
    │        Final Rescue & Mission Controller       │
    │    (Secret Lab, 4-Color Console, Evacuation)  │
    └───────────────────────────────────────────────┘
```

---

## 2. Artificial Intelligence Subsystem
The AI simulation is composed of four decoupled layers:

### 2.1 Navigation Grid (`NavGrid` & `AStarPathfinder`)
- **Spatial Discretization**: Uniform 2D cellular grid projected on the X-Z ground plane with a default cell size of $0.5\text{ m}$.
- **Heuristic Search**: A* algorithm utilizing octile distance heuristics with diagonal step penalty ($1.414\times \text{cost}$).
- **Binary Min-Heap Priority Queue**: Custom zero-allocation `MinHeap` implementation ensuring $O(\log N)$ extraction performance.
- **Dynamic Invalidation**: Bumps an atomic `Version` counter whenever environmental obstacles or blast doors open/close, prompting active agents to asynchronously replan paths.

### 2.2 Sensory Perception (`VisionSensor` & `SoundSystem`)
- **Acoustic Simulation**: Sound events propagate omnidirectionally with linear distance attenuation. Agents receive sound cues with loudness, position, and category (footstep, gunshot, explosion).
- **Cone Vision**: Multi-raycast sight tests evaluating field-of-view angle, obstruction masks, and ambient lighting intensity.

### 2.3 Goal-Oriented Action Planning (GOAP)
- **State Representation**: Bitmask and key-value world states (`PlayerInSight`, `WeaponArmed`, `InCover`, `TargetReachable`).
- **Action Regression**: Backward A* search through action precondition-effect graphs to formulate dynamic tactical execution plans.

### 2.4 Agent Controller & Path Translation Subsystem (`AgentMotor`, `PathUtils`)
- **String-Pulling & Spline Smoothing**: Translates discrete grid cell paths into continuous $C^1$ trajectories using line-of-sight pruning and centripetal Catmull-Rom resampling (`PathUtils.Smooth`).
- **Kinematic Steering**: Pure-pursuit lookahead tracking with dynamic angular velocity damping scaled inversely against linear speed (`AgentMotor.cs`).
- **Procedural Locomotion**: Phase-locked bipedal cadence (`WalkerAnimator.cs`) and aerodynamic quad-rotor banking physics (`DroneAnimator.cs`), decoupling AI decision-making from visual presentation without external animation clip overhead.

---

## 3. Combat & Ballistics Pipeline
### 3.1 Hitscan & Pellet Simulation (`PlayerWeapon`)
- Raycast-based ballistics supporting instantaneous collision detection.
- Configurable weapon archetypes:
  - **Mist Rifle**: Fully automatic precision assault rifle ($16\text{ dmg}$, $0.11\text{s interval}$).
  - **Mist Shotgun**: 8-pellet buckshot spread with high point-blank kinetic impulse ($11\text{ dmg/pellet}$).
  - **Mist Revolver**: High-damage semi-automatic magnum ($48\text{ dmg}$).
  - **Mist SMG**: High fire-rate compact submachine gun ($10\text{ dmg}$, $0.075\text{s interval}$).
- **Dynamic Recoil & Bloom**: Camera pitch impulse coupled with continuous spread expansion and exponential recovery damping.

### 3.2 Procedural Audio DSP Engine (`ProceduralAudio`)
- Zero external audio asset footprint: All SFX synthesized algorithmically in memory at startup.
- Synthesized waveforms:
  - Bandpass noise bursts (gunshots, footsteps, impacts).
  - Frequency-modulated sine/square sweeps (beeps, alarms, UI confirmation, laser discharges).
  - Multi-tone harmonic intervals for puzzle validation feedback.

---

## 4. Final Rescue Mission State Machine
```
[Titan Core Key Collected]
         │
         ▼
[Locate Secret Laboratory Blast Door]
         │
         ▼
[Explore Lab: Collect 4 Glenmist Lore Clues]
         │
         ▼
[Solve Appa's Harvest Cipher: Fire (Red) -> Mist (Blue) -> Sun (Yellow) -> Leaf (Green)]
         │
         ▼
[Disable Prison Energy Barrier & Speak with Father]
         │
         ▼
[Emergency Evacuation: 60-Second Countdown to Yard Escape Point]
```

### 4.1 Harvest Cipher & Unified Workstation Console Mechanics
To prevent linear mechanical bypass, all controls are centralized into a unified Master Puzzle Console Workstation (`Master_PuzzleConsole`) featuring:
- **Interactive CRT Monitor:** Real-time slot progression and cipher diagnostic feedback.
- **Scrambled Desk Buttons:** 
  - **Button 1 (Leftmost):** Emerald Leaf (Green)
  - **Button 2 (Mid-Left):** Sunrise Sun (Yellow)
  - **Button 3 (Mid-Right):** Kiln Flame (Red)
  - **Button 4 (Rightmost):** Midnight Mist (Blue)

Solving the cipher requires executing the narrative tea manufacturing order:
$$\mathbf{Red \ (Button \ 3) \rightarrow Blue \ (Button \ 4) \rightarrow Yellow \ (Button \ 2) \rightarrow Green \ (Button \ 1)}$$
Mismatches trigger an electrical short circuit discharge, room warning strobes, and prompt reset.

### 4.2 Dynamic Blast Door Emergence
Upon Titan defeat, `GameManager` signals `FinalRescueMission` to trigger `SecretLaboratoryDoor.RevealDoor()`. The concealing perimeter wall plate (`Lab_WallCoverPlate`) retracts into the floor accompanied by procedural steam, dust particle bursts, and audio cues, exposing the Master Key swipe terminal.

### 4.3 Zone 2 Drying Chamber Freight Container Architecture
To transform the previously vacant Drying Chamber (Zone 2, $X \in [10, 30], Z \in [0, 24]$) into a tactical industrial staging yard:
- **Parametric Container Assets:** 20ft and 40ft standard ISO intermodal shipping containers (`CargoContainer.fbx`) authored via Blender MCP pipeline with corrugated siding, corner castings, and dual lock-rod rear doors.
- **Runtime Layout & Obstacles:** Managed by `DryingChamberContainers.cs` and `LevelBuilder.cs`, placing tactical container barricades with dynamic URP materials (`Container_MaritimeTeal` and `Container_OxideRed`).
- **Tactical Cover & AI Navigation:** Dynamic `NavMeshObstacle` components with carving carve realistic patrol chokepoints and provide robust cover against hostile patrol units.

### 4.4 Cybernetic Master Console & Floor Power Conduits
The Secret Laboratory puzzle is unified into an ergonomic workstation console (`Master_PuzzleConsole`):
- **Visual Feedback Hierarchy:** Overhead high-contrast CRT monitor displaying live slot status (`[ 1: RED (FIRE) ]`, etc.), neon LED halo indicator rings at switch bases, and 3D metallic element nameplates.
- **Floor Conduit Network:** Dual high-voltage power conduits (`Console_Conduit_1` and `Console_Conduit_2`) route from the console along the laboratory floor into the prison stasis field, visually discharging spark overload bursts on incorrect sequences and conducting power deactivation cascades upon sequence completion.
