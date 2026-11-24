# Cybernetic Master Puzzle Workstation Console Specification

## 1. Architectural Overview
The **Master Puzzle Workstation Console** serves as the central interactive control terminal inside the Secret Laboratory (Zone 4) of Glenmist Facility. Rather than scattering isolated puzzle switches throughout the room, all four Glenmist harvest elemental inputs are unified onto a singular, high-tech industrial cyber-console positioned directly facing Appa's stasis containment cell.

```
+-------------------------------------------------------------------------+
|                  OVERHEAD WIDESCREEN CRT MONITOR (2.2m x 0.9m)          |
|    [ RED: FIRE ]        [ 2: ??? ]         [ 3: ??? ]        [ 4: ??? ] |
|            STATUS: PHASE 2: HIGHLAND MOUNTAIN MIST (BLUE)               |
+-------------------------------------------------------------------------+
                                    |
                    Heavy Cybernetic Console Mounting Pylon
                                    |
+-------------------------------------------------------------------------+
|  [ FIRE (RED) ]      [ MIST (BLUE) ]    [ SUN (YELLOW) ]  [ LEAF (GREEN)|
|   (Halo Ring)          (Halo Ring)        (Halo Ring)       (Halo Ring) |
|  [3D Nameplate]      [3D Nameplate]     [3D Nameplate]    [3D Nameplate]|
+-------------------------------------------------------------------------+
        ||                                                      ||
   Left Power Conduit                                      Right Power Conduit
        \\=================== Floor Routing ===================//
                                    ||
                                    \/
                        Stasis Field Energy Barrier
```

---

## 2. Structural & Ergonomic Form Factor

### 2.1 Heavy Desk Console Pedestal
- **Dimensions:** Width $3.4\,\text{m}$, Depth $1.2\,\text{m}$, Base Height $0.95\,\text{m}$.
- **Fascia Tilt:** $22^\circ$ upward rake facing the player for ergonomic interaction clearance and optimum visibility.
- **Materiality:** Matte obsidian chassis with brushed titanium edge trims and safety yellow chevron hazard strips along the bottom perimeter.

### 2.2 Widescreen Cyber Display Monitor
- **Monitor Dimensions:** $2.2\,\text{m}$ width $\times 0.9\,\text{m}$ height, elevated $1.2\,\text{m}$ above the console desk.
- **Bezel:** Recessed industrial casing with glowing blue status backlight (`Intensity: 1.8f`, `Range: 4.5m`).
- **Screen UI:**
  - **Upper Title Banner:** `APPA'S HARVEST PROTOCOL : MANUAL OVERRIDE CONSOLE`
  - **Cipher Phase Slots:** 4 dynamic slot indicators (`[ RED: FIRE ]`, `[ BLUE: MIST ]`, `[ YELLOW: SUN ]`, `[ GREEN: LEAF ]`).
  - **Real-Time Phase Guide:** Dynamic colored text detailing the required Glenmist lore phase currently awaiting input.

---

## 3. Switch Interface & Visual Hierarchy

Each of the four switches features a three-tier visual hierarchy:

| Element | Switch Color | Glenmist Lore Entity | 3D Nameplate Text | Pitch Frequency |
|---|---|---|---|---|
| **Phase 1** | **Red** | Kiln Fire / Withering Heat | `FIRE` | $1.50\,\text{Hz}$ |
| **Phase 2** | **Blue** | Highland Mountain Mist | `MIST` | $1.00\,\text{Hz}$ |
| **Phase 3** | **Yellow** | Morning Sunrise Ray | `SUN` | $1.15\,\text{Hz}$ |
| **Phase 4** | **Green** | Tender Ceylon Flush Leaf | `LEAF` | $1.30\,\text{Hz}$ |

### 3.1 Neon LED Halo Rings
- Mounted flush around the perimeter base of each switch.
- Projects high-intensity emission glow ($4.0\times$ multiplier) matching the respective element hue.
- Pulses synchronously upon player proximity.

### 3.2 3D Embossed Nameplates
- Precision text plates positioned directly in front of each switch on the tilted desk face.
- Crisp metallic lettering readable from standing interaction distance without requiring camera zoom.

---

## 4. Power Conduit Routing & Feedback Dynamics

### 4.1 Floor High-Voltage Conduits
- Two high-voltage conduit lines run along the laboratory floor:
  - `Console_Conduit_1`: Left channel connecting desk origin $(45.7, 0.05, 10.5)$ to barrier node $(45.7, 0.05, 12.8)$.
  - `Console_Conduit_2`: Right channel connecting desk origin $(47.3, 0.05, 10.5)$ to barrier node $(47.3, 0.05, 12.8)$.
- When inactive, conduits emit a dim pulsing electric blue trace.

### 4.2 Circuit Overload Sparks (Mismatch)
- If an incorrect sequence is pressed:
  1. Switches spark with local electrical discharge.
  2. High-voltage surge cascades along both floor conduits toward the barrier.
  3. Alarm buzzer and circuit trip acoustic cues sound.
  4. Red emergency strobe lights flash.
  5. Switches automatically de-energize and return to ready state.

### 4.3 Barrier Disengagement (Success)
- When the 4-phase sequence is verified:
  1. All 4 switch LED halo rings lock into radiant emerald green.
  2. Widescreen monitor displays `ACCESS GRANTED: ALL 4 PHASES VERIFIED`.
  3. Harmonic dual chime fanfares play.
  4. Energy barrier containment fields collapse, freeing Appa.
