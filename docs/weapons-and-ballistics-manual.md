# 🔫 Mist Protocol - Weapons Arsenal & Ballistics Specification Manual

## 1. Overview
The weaponry in Mist Protocol features a tactile multi-weapon arsenal designed for rapid adaptability in subterranean corridor combat and wide containment vaults. All ballistics are simulated via raycasts with dynamic recoil, pellet dispersion, and procedural reload animations.

---

## 2. Weapon Specifications Matrix

| Metric | Mist Rifle (Slot 1) | Mist Shotgun (Slot 2) | Mist Revolver (Slot 3) | Mist SMG (Slot 4) |
| :--- | :--- | :--- | :--- | :--- |
| **Firing Mode** | Full Auto | Pump-Action Semi | Heavy Semi-Auto | High-RPM Full Auto |
| **Damage per Hit** | $16.0\text{ HP}$ | $11.0\text{ HP} \times 8\text{ pellets}$ | $48.0\text{ HP}$ | $10.0\text{ HP}$ |
| **Max Potential Volley**| $16.0\text{ HP}$ | $88.0\text{ HP}$ | $48.0\text{ HP}$ | $10.0\text{ HP}$ |
| **Rate of Fire** | $545\text{ RPM}$ ($0.11\text{s}$) | $75\text{ RPM}$ ($0.80\text{s}$) | $150\text{ RPM}$ ($0.40\text{s}$) | $800\text{ RPM}$ ($0.075\text{s}$) |
| **Magazine Capacity** | $30\text{ rounds}$ | $6\text{ shells}$ | $6\text{ rounds}$ | $35\text{ rounds}$ |
| **Reserve Ammo** | $180\text{ rounds}$ | $36\text{ shells}$ | $36\text{ rounds}$ | $210\text{ rounds}$ |
| **Base Spread** | $0.50^\circ$ | $3.50^\circ$ | $0.25^\circ$ | $1.20^\circ$ |
| **Max Bloom Angle** | $2.80^\circ$ | $4.20^\circ$ | $1.80^\circ$ | $3.80^\circ$ |
| **Effective Range** | $80\text{ meters}$ | $35\text{ meters}$ | $100\text{ meters}$ | $50\text{ meters}$ |
| **Impact Impulse** | $6.0\text{ N}$ | $14.0\text{ N}$ | $18.0\text{ N}$ | $4.5\text{ N}$ |
| **Reload Duration** | $1.60\text{ seconds}$ | $2.20\text{ seconds}$ | $1.90\text{ seconds}$ | $1.40\text{ seconds}$ |

---

## 3. Kinetic Armor Penetration Rules

### 3.1 Standard Armor Plate Interaction
When firing at armored units (such as the Titan Automaton in Phase 1):
$$\text{Damage}_{\text{Dealt}} = \text{BaseDamage} \times 0.45$$
- Deflection sparks emit along the surface normal.
- Audio plays metallic ricochet ping (`impact` SFX).

### 3.2 Explosive Circumvention
- Hits dealing $\ge 40\text{ damage}$ (gas canister chain reactions) completely bypass kinetic armor, dealing $100\%$ raw explosive damage and inflicting a $3.5\text{s}$ hydraulic stabilizer stagger.

### 3.3 Core Fracture Amplification
- Once the boss's HP drops below $50\%$, armor plates rupture:
$$\text{Damage}_{\text{Dealt}} = \text{BaseDamage} \times 1.35$$
- Close-range weapons (Mist Shotgun and Mist SMG) deliver maximum burst DPS during this phase.

---

## 4. Controls & Input Mapping
- **Shoot**: Left Mouse Button (held for automatic weapons).
- **Reload**: R key (replenishes magazine from reserve).
- **Weapon Selection**:
  - `1`: Mist Rifle
  - `2`: Mist Shotgun
  - `3`: Mist Revolver
  - `4`: Mist SMG
  - `Q`: Quick-swap to next weapon in cycle.
  - `Mouse Scroll Wheel`: Scroll up/down to cycle weapons.
