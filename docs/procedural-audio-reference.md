# 🔊 Procedural Audio Synthesis - DSP Reference & Architecture

## 1. Overview
Mist Protocol uses real-time procedural audio synthesis via `ProceduralAudio.cs` to generate high-fidelity sound effects directly into Unity `AudioClip` buffers at runtime. This architectural decision eliminates audio file bloat from the repository, avoids third-party licensing constraints, and permits dynamic parameter modulation based on gameplay state.

---

## 2. DSP Synthesis Methods

### 2.1 Bandpass Shaped Noise (`Noise`)
Synthesizes kinetic and mechanical sound effects (gunshots, explosions, footsteps, impacts):
$$\text{Sample}(t) = \text{UniformNoise}(-1, 1) \cdot e^{-\lambda t} \cdot \text{LowpassFilter}(f_c)$$
- $\lambda$: Exponential envelope decay coefficient.
- $f_c$: Cutoff frequency in Hertz.
- Used for: `rifle`, `shotgun`, `explosion`, `step`, `impact`, `steam`.

### 2.2 Frequency-Modulated Sinusoidal Tone (`Tone`)
Synthesizes electronic, digital, and harmonic UI cues:
$$y(t) = A(t) \cdot \sin(2\pi \cdot f(t) \cdot t)$$
$$f(t) = f_0 + \Delta f \cdot e^{-\beta t}$$
- Used for: `beep`, `zap`, `alarm`, `success`, `heartbeat`.

---

## 3. Acoustic Sound Effect Registry

| SFX Identifier | DSP Archetype | Base Pitch | Duration | Usage Context |
| :--- | :--- | :--- | :--- | :--- |
| `rifle` | Shaped Noise | $900\text{ Hz}$ | $0.18\text{ s}$ | Player assault rifle discharge |
| `shotgun` | Low-end Noise | $480\text{ Hz}$ | $0.32\text{ s}$ | Multi-pellet heavy blast |
| `revolver` | Mid-punch Noise | $720\text{ Hz}$ | $0.24\text{ s}$ | High-caliber hand cannon |
| `smg` | Rapid High Noise | $1400\text{ Hz}$ | $0.11\text{ s}$ | High fire-rate burst |
| `heartbeat` | Sub-bass Dual Pulse | $48\text{ Hz}$ | $0.35\text{ s}$ | Player critical health warning ($<25\%\text{ HP}$) |
| `dryfire` | Micro Click | $2200\text{ Hz}$ | $0.06\text{ s}$ | Empty magazine hammer click |
| `slam` | Seismic Impact | $65\text{ Hz}$ | $1.20\text{ s}$ | Titan ground stomp shockwave |
| `steam` | High-pass Hiss | $3200\text{ Hz}$ | $0.85\text{ s}$ | Armor fracture steam vent burst |
| `alarm` | Alternating Warble | $880\text{ Hz}$ | $0.45\text{ s}$ | Laboratory incorrect sequence warning |
| `success` | Harmonic Chime | $1046\text{ Hz}$ | $0.50\text{ s}$ | Barrier disengagement confirmation |

---

## 4. Acoustic Propagation Model
- **Spatial Attenuation**: Standard linear falloff from $r_{\min} = 2\text{ m}$ to $r_{\max} = 45\text{ m}$.
- **Randomized Pitch Jitter**: Every spatial playback applies a subtle randomized pitch factor $\pm 7\%$ ($\text{pitch} \in [0.93, 1.07]$) to prevent acoustic repetition fatigue.
