# Stamina & Movement Mechanics Specification - Mist Protocol

This technical design document outlines the player mobility parameters, sprint kinetics, stamina depletion curves, and auditory emission thresholds implemented in Mist Protocol.

---

## 1. Kinematic Mobility Parameters

Nila's movement is managed via a Unity `CharacterController` integrated into `PlayerController.cs`.

| Movement State | Velocity (m/s) | Stride Length | Audio Hearing Radius | Acoustic Footstep Noise |
| :--- | :--- | :--- | :--- | :--- |
| **Crouching** | 2.0 m/s | 1.4 m | 3.0 m | `IsCrouching ? 0.15f : 0.4f` (Near Silent) |
| **Walking** | 4.2 m/s | 1.9 m | 8.0 m | Standard footstep (`step`, `step_wood`, `step_metal`) |
| **Sprinting** | 6.8 m/s | 2.4 m | 17.0 m | Heavy cadence footstep; alerts nearby AI agents |

---

## 2. Stamina Resource Budgeting

### Core Constants
- **`maxStamina`**: `100.0f`
- **`staminaDrainRate`**: `22.0f / sec` (Approx. 4.5 seconds of continuous sprinting)
- **`staminaRegenRate`**: `18.0f / sec` (Approx. 5.5 seconds to fully recover from empty)
- **`staminaRegenDelay`**: `1.0f sec` (Cooldown interval before regeneration begins after releasing Shift)
- **`recoveryThreshold`**: `25.0f` (Minimum stamina required to re-engage sprint after exhaustion)

```
        Sprinting (Drain: 22/s)
[100%] ========================> [0% EXHAUSTED]
                                       |
                                       | (1.0s delay)
                                       v
[100% READY] <================== [25% RECOVERED]
          Regeneration (18/s)
```

---

## 3. Exhaustion Penalty & Feedback

When stamina reaches zero:
1. **Kinetic Penalty**: `IsSprinting` immediately drops to false; velocity clamps to `walkSpeed` (4.2 m/s).
2. **Audio Feedback**: Procedural audio synthesizes an accelerated heartbeat cue (`heartbeat` at `pitch = 1.3f`) signaling physical exertion.
3. **HUD Feedback**: The stamina gauge beneath the health bar pulses rapidly in warning amber/red (`flash = 0.5f + 0.5f * sin(time * 14f)`).
4. **Recovery Lock**: Sprinting remains disabled until stamina recharges past 25%, preventing stutter-sprinting.

---

## 4. Ground Contextual Audio Simulation

Footsteps emit specialized procedural audio synthesis depending on the ground physics material:
- **`step_metal`**: Catwalks, iron grates, and pipes (bandpass filtered noise centered at 950 Hz).
- **`step_wood`**: Withering loft wooden planks and crate tops (low-frequency resonance at 220 Hz).
- **`step`**: Concrete laboratory tiles and standard factory flooring (neutral impact noise at 300 Hz).
