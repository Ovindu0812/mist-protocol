# Agent Controller: Path Translation & Procedural Animation Pipeline

## 1. Role Overview (Graphics & Visualization - Student 4)
The **Agent Controller** bridges the discrete mathematical navigation representation produced by the Intelligent Systems pathfinding algorithms (A*, Dijkstra) and continuous, lifelike 3D motion in the Unity engine.

The core challenge is eliminating grid artifacting ("staircase walking"), abrupt angular snaps, and foot sliding without relying on pre-baked humanoid animation clips that inflate build sizes.

---

## 2. Path Array Translation Architecture

```
[ A* NavGrid Path Array ] (List<Vector3> grid nodes)
           │
           ▼
[ String-Pulling Optimization ] (PathUtils.Simplify: LOS raycast elimination)
           │
           ▼
[ Catmull-Rom Spline Resampling ] (PathUtils.Smooth: C1 continuous curvature)
           │
           ▼
[ AgentMotor Kinematics ] (Pure-pursuit steering, dynamic turn damping, arrival deceleration)
           │
           ▼
[ Procedural Animators ] (WalkerAnimator, DroneAnimator, SpiderAnimator)
```

### 2.1 String-Pulling (Corner Pruning)
The raw output of the 8-directional A* algorithm contains intermediate waypoint cells along straight corridors. `PathUtils.Simplify` uses line-of-sight raycasting across the grid cells:
- Anchor node $p_0$ is compared against $p_i$.
- If `grid.WalkableLine(p_0, p_i)` is true, intermediate nodes $p_1 \dots p_{i-1}$ are pruned.
- When an obstruction is encountered, $p_{i-1}$ becomes the new anchor.
- Reduces path node count by 65–80%, converting staircases into minimal corner lists.

### 2.2 Centripetal Catmull-Rom Spline Smoothing
Sharp angular turns at corner waypoints cause unnatural robotic snapping. `PathUtils.Smooth` applies a cubic Hermite spline across four control points $(p_0, p_1, p_2, p_3)$:

$$P(t) = 0.5 \cdot \Big[ 2p_1 + (-p_0 + p_2)t + (2p_0 - 5p_1 + 4p_2 - p_3)t^2 + (-p_0 + 3p_1 - 3p_2 + p_3)t^3 \Big]$$

Where:
- $t \in [0, 1]$ parameterizes the curve between $p_1$ and $p_2$.
- $p_0$ and $p_3$ provide directional tangent continuity ($C^1$ continuity).
- Spline samples are dynamically validated against `NavGrid.IsWalkableWorld(p)` to guarantee that curves never clip wall colliders.

---

## 3. AgentMotor Kinematics & Pure-Pursuit Steering

`AgentMotor.cs` executes continuous movement along the resampled spline:
- **Look-Ahead Target Tracking:** The agent projects a pure-pursuit steering vector toward the current spline waypoint.
- **Dynamic Turn Rate Damping:**
  $$\text{TurnRate} = \text{Lerp}(\text{BaseRate} \cdot 0.85, \text{BaseRate} \cdot 1.2, 1.0 - \text{Speed01} \cdot 0.35)$$
  Higher linear speeds widen turn radii, preventing instant 180-degree snaps.
- **Arrival Deceleration:** Near the terminal waypoint, velocity smoothly interpolates to zero, avoiding overshoot oscillations.

---

## 4. Procedural Animation Subsystems

### 4.1 Hunter Drone (`DroneAnimator.cs`)
- **Rotor Dynamics:** Counter-rotating quad rotors spin at 1,800 RPM base, scaling with linear velocity.
- **Aerodynamic Banking:** Pitch and roll are derived from local acceleration vectors:
  $$\text{Pitch} = \text{Clamp}(\text{LocalAcc}_z \cdot 4.0, -\theta_{\max}, \theta_{\max})$$
  $$\text{Roll} = \text{Clamp}(-\text{LocalAcc}_x \cdot 4.0, -\theta_{\max}, \theta_{\max})$$
- **Hover Bob:** Sinusoidal vertical displacement with amplitude dampening at high speeds for flight stabilization.

### 4.2 Sentinel Biped (`WalkerAnimator.cs`)
- **Phase-Locked Cadence:** Stride phase advances by linear distance traveled ($\Delta \phi = \frac{v \cdot \Delta t}{L_{\text{stride}}} \cdot \pi$), eliminating foot sliding at any speed.
- **Limb Counter-Oscillation:** Left and right legs swing in exact anti-phase ($\sin(\phi)$ vs $-\sin(\phi)$).
- **Torso Recoil & Weapon Aiming:** Inverse kinematics tilt torso and head toward player position while absorbing weapon recoil impulses.
