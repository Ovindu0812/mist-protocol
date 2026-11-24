# 🎓 Agent Controller (Student 4) - Comprehensive Viva Defense Guide

**Student ID:** `IT24103271`  
**Email:** `it24103271@my.sliit.lk`  
**Dual Modules:** Graphics and Visualization (20%) & Intelligent Systems (20%)

---

## 1. Executive Summary & Assigned Responsibilities

| Module | Assigned Role | Primary Systems & Scripts | Core Responsibilities |
| :--- | :--- | :--- | :--- |
| **Graphics & Visualization** | **Agent Animation & Movement** | `AgentMotor.cs`<br>`PathUtils.cs`<br>`WalkerAnimator.cs`<br>`DroneAnimator.cs`<br>`SpiderAnimator.cs` | Translating raw discrete A* path arrays into smooth $C^1$ continuous 3D motion, pure-pursuit steering, dynamic turn damping, and procedural limb/rotor animations without external clips. |
| **Intelligent Systems** | **Autonomous Agent (Hunter Drone)** | `HunterDroneBrain.cs`<br>`SuspicionMap.cs`<br>`SoundSystem.cs`<br>`VisionSensor.cs` | Designing the acoustic-driven aerial enemy class: topological Dijkstra sound propagation, exponential decaying suspicion map ($t_{1/2} = 45\text{s}$), active Doppler sonar ping, and circle-strafing flight dynamics. |

---

## 2. Frequently Asked Viva Questions & High-Scoring Answers

### Q1: "How do you convert discrete A* grid paths into smooth character motion without robotic staircasing?"
> **Answer:**  
> "Raw 8-directional A* produces a discrete list of grid coordinates that would cause jagged staircase movement. We solve this in two stages:
> 1. **String-Pulling (`PathUtils.Simplify`):** We raycast between non-adjacent waypoints across the grid. If a direct line of sight is walkable, intermediate waypoints are pruned, reducing node count by ~70%.
> 2. **Centripetal Catmull-Rom Spline Interpolation (`PathUtils.Smooth`):** We fit a cubic spline across the pruned corners using four-point Hermite interpolation with tangent continuity:
>    $$P(t) = 0.5 \cdot [ 2p_1 + (-p_0 + p_2)t + (2p_0 - 5p_1 + 4p_2 - p_3)t^2 + (-p_0 + 3p_1 - 3p_2 + p_3)t^3 ]$$
>    Each interpolated sample point is verified against `NavGrid.IsWalkableWorld()` to ensure the curved trajectory never clips wall geometry."

### Q2: "Why did you choose procedural animation over pre-baked Mecanim animation clips?"
> **Answer:**  
> "Procedural animation offers three key engineering advantages:
> 1. **Zero Asset Overhead:** Keeps the repository lightweight and self-contained without needing heavy FBX animation packages.
> 2. **Zero Foot Sliding:** In `WalkerAnimator.cs`, limb swing cadence is phase-locked directly to displacement distance traveled ($\Delta \phi = \frac{v \cdot \Delta t}{L_{\text{stride}}} \cdot \pi$). As speed changes, the feet step at the exact physical rate required, completely eliminating foot sliding.
> 3. **Dynamic Physical Responsiveness:** In `DroneAnimator.cs`, pitch and roll bank angles respond directly to instantaneous acceleration vectors and turn rates, providing natural aerodynamic tilt that reacts to real-time physics forces."

### Q3: "How does the Hunter Drone detect players if it doesn't have line of sight?"
> **Answer:**  
> "The Hunter Drone utilizes an **acoustic perception pipeline** rather than omniscient line of sight:
> - Whenever the player fires a weapon, runs, or throws a physics crate, a `SoundStimulus` event is emitted.
> - `SoundSystem.cs` simulates acoustic propagation using a **Dijkstra search over the navigation grid**. This means sound travels through open doorways and corridors rather than piercing solid walls.
> - Received sounds are accumulated into an exponential decaying **Suspicion Heat Map** ($t_{\text{half}} = 45\text{s}$). When patrolling, the drone moves toward the hottest sector instead of wandering randomly."

### Q4: "What makes the Hunter Drone's aerial combat dynamic and engaging?"
> **Answer:**  
> "Rather than rushing directly toward the player's crosshairs, the Hunter Drone executes a **tangential circle-strafe orbit** at an 8-meter standoff radius. To prevent the player from easily predicting its flight path, the drone has a dynamic chance each tick to reverse its orbit direction, and instantly flips when approaching physical wall boundaries."

---

## 3. Code Navigation Cheat Sheet (Live Code Walkthrough)

When the examiner asks to inspect your code, immediately open:

1. **Catmull-Rom Spline & String-Pulling:**  
   `Assets/_Project/Scripts/AI/Navigation/PathUtils.cs` (lines 12–62)
2. **Dynamic Turn Damping & Pure-Pursuit Steering:**  
   `Assets/_Project/Scripts/Animation/AgentMotor.cs` (lines 75–155)
3. **Phase-Locked Bipedal Gait & Torso Recoil:**  
   `Assets/_Project/Scripts/Animation/WalkerAnimator.cs` (lines 30–63)
4. **Aerodynamic Drone Banking & Rotor Spin:**  
   `Assets/_Project/Scripts/Animation/DroneAnimator.cs` (lines 26–47)
5. **Hunter Drone FSM & Acoustic Orbiting:**  
   `Assets/_Project/Scripts/AI/HunterDrone/HunterDroneBrain.cs` (lines 70–95, 172–203)
6. **Decaying Suspicion Map:**  
   `Assets/_Project/Scripts/AI/HunterDrone/SuspicionMap.cs` (lines 32–52)

---

## 4. Live Demonstration Checklist

- [ ] Run the game in Unity Editor or Standalone build.
- [ ] Press **`F1`** to toggle the AI Debug Overlay (displays current FSM state, suspicion heat, and path validity).
- [ ] Shoot the rifle in an adjacent corridor; show how the Hunter Drone hears the sound through the doorway and navigates to investigate.
- [ ] Throw a physics crate; show how the impact sound lures the drone away.
- [ ] Crouch behind low cover when the drone pulses its blue sonar ping to prove stealth evasion works.
- [ ] Observe the drone's banking roll when turning corners and circling during combat.
