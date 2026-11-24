# Facility Combat Encounters and Tactical Sector Deployment

## 1. Overview
The Glenmist Tea Factory facility is divided into multiple combat zones, each populated with specialized automated defense units deployed by the OVERSEER director system:
- **Sector A (Withering Loft):** Initial combat encounter testing player stealth and basic weapon mechanics.
- **Sector B (Rolling Room):** Dense machine floor with rolling tables and high verticality.
- **Sector C (Drying Chamber):** Large warehouse floor sealed behind a keycard security gate with heavy dryer ovens.
- **Sector E (Containment Vault):** Boss arena housing the Titan Automaton Enforcer Unit-94 and the Secret Laboratory.

---

## 2. Sector A (Withering Loft) Squad Composition
- **3 Sentinels:**
  - `WitheringLoft_Sentinel_1`: Front entrance threshold patrol `[(-20, 4), (-26, 4), (-26, 14), (-20, 14)]`
  - `WitheringLoft_Sentinel_2`: Central corridor across withering trough aisles `[(-18, 14), (-13, 14), (-13, 24), (-18, 24)]`
  - `WitheringLoft_Sentinel_3`: Northern supervisor desk & keycard station guard `[(-24, 26), (-28, 26), (-28, 33), (-24, 33)]`
- **3 Hunter Drones:** Hovering at elevation $Y \in [2.4\text{m}, 2.5\text{m}]$ providing searchlight scanning.
- **2 Saboteurs:** Low-profile quadruped crawlers navigating stealth perimeter lines.

---

## 3. Sector C (Drying Chamber) Squad Composition
- **3 Sentinels:**
  - `DryingChamber_Sentinel_1`: Guarding the Tier-1 keycard security gate entrance.
  - `DryingChamber_Sentinel_2`: Deep patrol between heavy ECP drying oven bays.
  - `DryingChamber_Sentinel_3`: Chokepoint sentry monitoring the Sector E blast door corridor.
- **3 Hunter Drones:** Rooftop patrol scanning over drying machinery.
- **2 Saboteurs:** Mobile dynamic obstacle manipulators repositioning cover.

---

## 4. Lifecycle & Runtime Management
Encounter deployment is managed via `FacilityEncounterSquads.cs` ensuring seamless runtime initialization and test compatibility.
