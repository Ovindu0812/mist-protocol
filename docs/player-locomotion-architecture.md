# Player Locomotion Architecture

This document outlines the technical design, kinematics, and state orchestration of the first-person player controller within the Glenmist Tea Processing Facility.

---

## 1. Locomotion Kinematics

The player controller utilizes Unity's `CharacterController` component, integrating a kinematic velocity solver with acceleration and deceleration smoothing curves.

### 1.1 Velocity Integration
```
V_target = (D_forward * Input_Y + D_right * Input_X) * TargetSpeed
V_current = Lerp(V_current, V_target, AccelerationRate * dt)
```

- **Base Walk Speed:** 4.5 m/s
- **Sprint Multiplier:** 1.8x (8.1 m/s)
- **Crouch Multiplier:** 0.5x (2.25 m/s)
- **Ground Snapping Velocity:** -2.0 m/s downward bias when grounded to maintain surface contact across sloped walkways and corrugated steel ramps.

---

## 2. Crouch and Stance Dynamics

The stance system dynamically interpolates between upright standing and ducking/crouching postures.

### 2.1 CharacterController Geometry
- **Stand Height:** 1.80 m (`Center = (0, 0.9, 0)`)
- **Crouch Height:** 1.00 m (`Center = (0, 0.5, 0)`)
- **Ceiling Raycast Probe:** Before uncrouching, a vertical capsule/sphere cast of radius 0.35m is cast upward to verify 0.85m of overhead clearance. If obstructed by low overhead pipes or ducting, uncrouching is suppressed.

---

## 3. Stamina Economy

Sprinting is governed by a bounded stamina reservoir:
- **Maximum Stamina:** 100.0 units
- **Depletion Rate:** 18.0 units/sec while sprinting with non-zero movement input
- **Recharge Delay:** 1.25 sec cooldown after sprinting ends
- **Recovery Rate:** 22.0 units/sec
- **Exhaustion State:** When stamina drops to 0.0, sprint is forcibly disabled until stamina recovers above 20.0 units.

---

## 4. Camera View & Head Bob

The camera pivot is managed independently from the root body transform:
- **Pitch Angle Limit:** -85.0 to +85.0 degrees
- **Sub-tick Mouse Delta Smoothing:** Frame-rate independent mouse input processing
- **Procedural Head Bob:** Dual sine wave generator mapping step frequency to walking velocity, dampening down to zero during crouching or stationary aim.
