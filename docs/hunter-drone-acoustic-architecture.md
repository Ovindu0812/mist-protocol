# Hunter Drone: Acoustic Memory Architecture & Aerial Combat Behaviors

## 1. Overview (Intelligent Systems - Student 4)
The **Hunter Drone** is an autonomous aerial enemy class governed by an acoustic-driven decision architecture. Unlike standard FPS enemies that use omniscient vision cones or simple "alert radii", the Hunter Drone "hunts by ear," tracking the player through the acoustic fingerprint left in the environment.

---

## 2. Acoustic Propagation & Sensory Pipeline

```
[ Gunshot / Footstep / Impact ] (Game Action)
               │
               ▼
   [ Dijkstra SoundSystem ] (Topological propagation along walkable corridors)
               │
               ▼
    [ SoundStimulus Struct ] (Intensity, estimated position, sound type weight)
               │
               ▼
    [ Decaying SuspicionMap ] (Exponential heat accumulation)
               │
               ▼
[ Hierarchical FSM Decisions ] (Patrol -> Investigate -> Search -> Engage)
```

### 2.1 Dijkstra Topological Sound Propagation
Sound does not pass through solid concrete walls. The acoustic solver utilizes Dijkstra search over the `NavGrid` to determine true acoustic distance:
$$I_{\text{perceived}} = I_{\text{source}} \cdot \frac{1}{1 + \alpha \cdot d_{\text{topological}}}$$
Gunfire around a corner propagates through open doorways and corridors, accurately alerting the drone without wall-hacking.

### 2.2 Stimulus Weighting & Priority
Different sounds possess varying tactical weights:
- **Explosions:** $1.2\times$ priority (highest alarm).
- **Gunshots:** $1.0\times$ priority.
- **Physical Impacts (Thrown Crates):** $0.9\times$ priority (allows player distraction tactics).
- **Footsteps:** $0.8\times$ priority.

---

## 3. Decaying Suspicion Heat Map (`SuspicionMap.cs`)

When idling or patrolling, the drone consults a coarse spatial heat map rather than wandering randomly.
- **Discretization:** The grid is downsampled by factor $4\times 4$ to maximize memory efficiency.
- **Exponential Decay:**
  $$H(t) = H(t_0) \cdot 2^{-\frac{\Delta t}{t_{\text{half}}}}$$
  Where $t_{\text{half}} = 45\text{ seconds}$.
- **Tactical Memory:** Even after the player relocates, the drone prioritizes exploring sectors with high residual suspicion, making sneaking away tense and believable.

---

## 4. Active Sonar Ping System

During the `Search` state, the drone hovers and emits an active 360-degree sonar ping:
- **Ping Radius:** $9\text{ meters}$.
- **Tactical Counterplay:** If the player is standing or running, the Doppler radar return reveals their coordinates through smoke and obstacles. However, **crouching defeats the sonar**, rewarding stealth play.

---

## 5. Aerial Orbit & Circle-Strafing Dynamics

When entering `Engage` state, the drone avoids flying directly at the player's crosshairs:
- **Tangential Orbit Angle:** Rotates at $\pm 32^\circ/\text{sec}$ around the target at an $8\text{ meter}$ standoff radius.
- **Spontaneous Reversals:** Dynamic 1.5% probability per tick to flip strafe direction ($\text{orbitDir} = -\text{orbitDir}$), breaking player predictive tracking.
- **Wall Collision Avoidance:** If the projected orbit waypoint hits an obstacle, the direction instantly inverts.
