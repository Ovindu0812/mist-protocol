# Runtime Performance Optimization Guide - Mist Protocol

This document details the architectural techniques and programming patterns utilized in Mist Protocol to ensure a stable 60+ FPS on budget GPUs and laptop hardware without garbage collection spikes.

---

## 1. Zero-Allocation Physics Queries

### The Problem
Standard Unity physics methods like `Physics.OverlapSphere`, `Physics.RaycastAll`, and `Physics.SphereCastAll` allocate a new array of `Collider[]` or `RaycastHit[]` on the managed heap each time they are invoked. In combat with multiple automatic weapons, explosions, and dozens of AI perception checks per second, these frequent allocations trigger periodic Mono/IL2CPP Garbage Collection pauses (frame stutter).

### The Solution: NonAlloc Query Buffers
All recurring physics queries use the `NonAlloc` API variants paired with statically allocated or member-cached buffers:
- **`Physics.OverlapSphereNonAlloc`**: Used in `SoundSystem` hearing checks, explosion splash radii, and area damage queries.
- **`Physics.RaycastNonAlloc`**: Employed in `PlayerWeapon` hitscan ballistics and line-of-sight checks in `AgentBase`.

```csharp
// Example Zero-Alloc Pattern in Mist Protocol:
private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];

public static bool CheckLineOfSight(Vector3 origin, Vector3 target, LayerMask mask)
{
    Vector3 dir = target - origin;
    int count = Physics.RaycastNonAlloc(origin, dir.normalized, HitBuffer, dir.magnitude, mask);
    return count == 0;
}
```

---

## 2. Procedural Audio DSP & Runtime Caching

### Zero Disk Footprint & Instant Loading
Mist Protocol does not load multi-megabyte uncompressed `.wav` or compressed `.mp3` assets from disk:
- All sound effects (weapon fire, explosions, reloads, hums, radio squelches) are procedurally synthesized at startup using mathematical wave synthesis (sine waveforms, white noise generators, frequency modulation, and low-pass filtering).
- Generated clips are cached in a statically allocated lookup table (`ProceduralAudio.Cache`), ensuring `O(1)` dictionary retrieval with zero repetitive memory allocation during gameplay.
- Audio pooling recycles temporary `AudioSource` game objects upon playback completion.

---

## 3. Spatial Influence Map Optimization

### Grid-Based Staggered Updates
Rather than calculating complex multi-agent tactical vectors on every frame:
- The `InfluenceMap` divides the Glenmist factory layout into discrete 2D spatial cells.
- Threat propagation uses integer coordinate strides and exponential decay lookups.
- AI Director updates are throttled to a fixed 10 Hz cadence (`0.1s` interval), decoupling tactical reasoning from the rendering frame rate.

---

## 4. Draw Call Batching & Material Management

### Shared Materials
- In-world props, modular walls, crates, and puzzle switches share unified Universal Render Pipeline (URP) materials.
- Procedural elements reuse standard fallback shaders (`Universal Render Pipeline/Lit`) to allow SRP Batcher and GPU Instancing to merge multiple draws into single GPU passes.

---

## 5. UI Canvas Segregation

- High-frequency UI elements (reticle crosshair bloom, health bar fill, minimap blips) are isolated on sub-canvases from static HUD elements (compass backing, objective labels).
- This prevents full-canvas graphic mesh regeneration when only dynamic HUD elements update.
