# Procedural Audio Synthesis and Sound Propagation Engine

## 1. Synthesis Philosophy
Mist Protocol utilizes purely synthesized procedural audio clips generated mathematically in memory without external audio assets. This guarantees instant asset loading and zero external file dependencies.

---

## 2. Core Audio Synthesizers
- **Gunshots:** Decaying high-frequency white noise burst with exponential amplitude falloff.
- **Footsteps / Stomps:** Low-frequency sine wave thump ($45\text{Hz} - 80\text{Hz}$) modulated with dynamic velocity.
- **Explosions / Impacts:** Filtered noise rumble coupled with sub-bass harmonic distortion.
- **Electronic Alarms:** Intermittent frequency-modulated square wave siren.

---

## 3. Acoustic Propagation and AI Awareness
The `SoundSystem` broadcasts emitted `SoundStimulus` events with calibrated decibel intensity and distance attenuation. AI agents within radius register the stimulus and transition to `InvestigateAction` states.
