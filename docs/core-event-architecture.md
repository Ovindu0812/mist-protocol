# Core Event Architecture & Decoupled Messaging

This document details the event-driven communication model, static state lifecycle management, and domain-reload mitigation in Mist Protocol.

---

## 1. Global Event Bus Pattern (`GameEvents`)

To prevent tight coupling between distinct subsystems (Player, AI Director, Procedural Audio, HUD UI, and World Interaction), the architecture employs a static broadcast bus `GameEvents`:

### 1.1 Broadcast Channels
- **`AgentKilled (AgentKind, Vector3)`:** Broadcast whenever an enemy unit is eliminated. Used by `HUD` for kill counters, `OverseerDirector` for pacing curves, and `ProceduralAudio` for spatial feedback.
- **`PlayerDamaged (float)`:** Fired when damage penetrates shields or health. Triggers HUD red vignette flashes and Overseer combat intensity escalation.
- **`PlayerShotFired` & `PlayerShotHit (bool)`:** Monitored by `PlayerProfile` to calculate dynamic accuracy metrics without referencing weapon scripts.
- **`PhysicsInteraction`:** Signals manipulation of environment objects, alerting nearby Saboteurs to investigate disruption of factory geometry.
- **`OverseerMessage (string)`:** Broadcasts narrative dialogue and security announcements to the HUD and audio subsytems.

---

## 2. Domain Reloading & Static Lifetime (`StaticReset`)

With Unity's **Configurable Enter Play Mode** enabled (disabling domain reloads to accelerate iteration times), static event delegates persist across play sessions unless explicitly sanitized.

- Every static event register invokes `StaticReset.Register(Reset)` during `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.
- On scene teardown or play-mode exit, `StaticReset.ExecuteAll()` clears all delegates (`null`), preventing memory leaks, phantom subscriptions, and invocations on destroyed `MonoBehaviour` targets.
