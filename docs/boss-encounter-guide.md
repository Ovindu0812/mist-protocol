# 🤖 Titan Automaton Encounter - Combat Strategy & Technical Guide

## 1. Boss Profile: The Titan Automaton
The **Titan Automaton** is the facility's heavy security combat platform, stationed within the sealed Containment Vault to prevent access to the inner research labs.

```
       [Titan Automaton]
       - Max Health: 650 HP
       - Kinetic Armor: 55% Bullet Mitigation
       - Primary Weapon: Heavy Dual-Barrel Pulse Cannon
       - Searchlight: Dynamic High-Intensity Detection Beam
```

---

## 2. Combat Phases & Mechanics

### Phase 1: Heavy Armor Defense (100% - 50% HP)
- **Damage Resistance**: Standard small-arms ammunition suffers a $55\%$ damage reduction due to thick reinforced steel plating. Bullets produce bright ricochet sparks and metallic pinging sounds.
- **Explosive Vulnerability**: Damage values exceeding $40\text{ dmg}$ (such as gas canister explosions) completely bypass armor mitigation, staggering the Titan and dealing $100\%$ raw damage.
- **Searchlight Tracking**: The frontal spotlight sweeps the chamber in a $65^\circ$ arc. When Nila is illuminated, the Titan switches from patrol to aggressive pursuit mode.

### Phase 2: Enraged & Fractured Core (< 50% HP)
- **Armor Fracture**: The outer plating ruptures in an explosive burst of sparks and pressurized steam vents.
- **Vulnerability Shift**: Fractured areas lose bullet mitigation, increasing small-arms effectiveness by $1.5\times$.
- **Aggression Buff**: Movement and turn rate increase by $35\%$. The searchlight switches from amber to an aggressive flashing crimson strobe.
- **Ground Slam Shockwave**: When closing within $6\text{ meters}$, the Titan executes a devastating ground stomp, emitting a radial physics impulse that pushes the player back and knocks over obstacles.

---

## 3. Containment Vault Tactical Layout
```
┌────────────────────────────────────────────────────────┐
│ [Blast Door]                [Debris Barricade]         │
│                                                        │
│     [Gas Canister A]              [Gas Canister B]     │
│                                                        │
│                    [TITAN AUTOMATON]                   │
│                                                        │
│     [Pillar Cover]                [Pillar Cover]       │
│                                                        │
│              [Gas Canister C]                          │
└────────────────────────────────────────────────────────┘
```

### Key Tactics:
1. **Canister Kiting**: Lure the Titan near volatile gas canisters scattered around the vault. Detonating canisters with pistol or rifle shots inflicts devastating $60+\text{ damage}$ stagger bursts.
2. **Pillar Line-of-Sight**: Break visual contact around concrete load-bearing pillars to force the Titan to lose tracking and resume search routines.
3. **Weak Point Exploitation**: Once armor fractures in Phase 2, prioritize high fire-rate weapons (Mist SMG or Shotgun) to rapidly drain the remaining health pool.

---

## 4. Victory & Loot Drops
Upon core depletion:
1. **Master Prison Cell Key** spawns at the wreckage point (`MasterCellKey.cs`).
2. **Father's Annotated Blueprints** spawn adjacent to the key (`FatherClue.cs`).
3. Vault blast doors unlock automatically, clearing the path to the Secret Laboratory.
