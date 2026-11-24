# Environmental Clue Narrative Flow & Spatial Wayfinding

This document defines the structural narrative, clue sequencing, and wayfinding mechanics tracking Appa's captivity throughout the Glenmist facility.

---

## 1. Narrative Clue Sequencing

The environmental clues left behind by Nila's father serve as both narrative anchors and gameplay signposts guiding the player toward the Secret Containment Laboratory:

1. **Clue 1: Dropped Boot (Withering Loft - Sector A)**
   - *Inspection Dialogue:* "Appa's work boot. Heavily scuffed. Looks like he was dragged backwards toward the rolling machinery."
   - *Wayfinding Outcome:* Calibrates HUD objective marker toward the Rolling Mill pass-through.

2. **Clue 2: Cracked Radio-Phone (Rolling Mill - Sector B)**
   - *Inspection Dialogue:* "His two-way maintenance radio. Antenna snapped off, display frozen on emergency distress channel 4."
   - *Wayfinding Outcome:* Activates acoustic tracking toward Sector C (Fermentation & Drying Chamber).

3. **Clue 3: Supervisor Keycard / ID Badge (Drying Chamber - Sector C)**
   - *Inspection Dialogue:* "Appa's biometric ID badge. Blood on the lanyard. The magnetic strip has clearance for the deep sub-basement vaults."
   - *Wayfinding Outcome:* Grants authorization for the High-Security Sifter gate.

4. **Clue 4: Hand-Drawn Facility Blueprint (Sub-Level Transit - Sector D)**
   - *Inspection Dialogue:* "A schematic of the hidden laboratory beneath Sector E. He circled the primary circuit breaker and wrote: 'Lure the guardian into the high-voltage pit'."
   - *Wayfinding Outcome:* Unlocks Sector E vault access and sets primary boss encounter trigger.

---

## 2. Interaction & Visual Cueing

- **Visual Signaling:** Each clue incorporates a soft cyan/gold point light with vertical sinusoidal bobbing ($f = 2.5\text{ rad/s}, A = 0.04\text{m}$) to stand out against industrial gloom.
- **Trigger Volumes:** Clue colliders are configured with `isTrigger = true` to prevent any physics obstruction or camera clipping during inspection.
- **One-Way Inspection:** Once examined, the clue enters an inspected state, deactivating its prompt and updating global game state in `GameManager`.
