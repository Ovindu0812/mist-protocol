# Ballistics and Recoil Specification

This document details the ballistics model, recoil kick mechanics, bloom dispersion, and ammunition economics implemented across player weaponry in Mist Protocol.

---

## 1. Weapon Arsenal Specifications

| Weapon Class | Mag Size | Reserve | Fire Rate | Base Damage | Max Range | Pellets | Pattern / Behavior |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Mist Rifle** | 30 | 180 | 0.11s (545 RPM) | 16 | 80m | 1 | Full-auto precision rifle |
| **Mist Shotgun** | 8 | 48 | 0.75s (80 RPM) | 12 | 25m | 8 | High-spread close combat |
| **Mist Revolver** | 6 | 36 | 0.40s (150 RPM) | 45 | 60m | 1 | Heavy kinetic sidearm |
| **Mist SMG** | 40 | 240 | 0.08s (750 RPM) | 11 | 45m | 1 | High cadence spray |

---

## 2. Recoil and Heat Dissipation

Firing a weapon applies instantaneous procedural rotational impulses to the viewmodel and camera transform:

1. **Recoil Pitch (`_kick`):** An instantaneous upward kick added per shot (`recoilPitch`), recovered dynamically via dampening interpolation:
   ```
   _kick = Mathf.Lerp(_kick, 0f, RecoilRecoverySpeed * dt)
   ```
2. **Heat Accumulation (`_heat`):** Continuous automatic fire drives the heat value from 0.0 to 1.0. Heat directly scales projectile cone dispersion (bloom).
3. **Spread Calculation:**
   ```
   SpreadRadius = BaseSpread * (1.0 + HeatMultiplier * _heat)
   ```

---

## 3. Acoustic Perception & Alert Propagation

Gunshots produce sound spheres that trigger enemy perception within the AI alert pipeline:
- **Mist Rifle:** 34.0m acoustic alert radius
- **Mist Shotgun:** 45.0m acoustic alert radius
- **Mist Revolver:** 40.0m acoustic alert radius
- **Mist SMG:** 30.0m acoustic alert radius

AI listeners within line of sight receive immediate combat awareness, while obstructed listeners investigate the last heard auditory origin.
