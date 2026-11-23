# Secret Laboratory Puzzle & Story Lore Guide - Mist Protocol

This document details the mechanics, narrative background, and procedural implementation of the Secret Laboratory 4-Color Switch Sequence puzzle in Mist Protocol.

---

## 1. Narrative Lore Background: Appa's Harvest Cipher

During the lockdown of Glenmist Tea Estate, Chief Automation Engineer Appa recognized that the divergent AI OVERSEER was operating on strict linear mechanical logic. To safeguard the emergency containment override, Appa designed an anti-machine cipher rooted in traditional Sri Lankan orthodox tea manufacture.

Appa scrambled the physical pedestal boxes across the laboratory floor, rendering linear left-to-right interaction obsolete, and keyed the override sequence to the **Ceylon Tea Withering Cycle**:

```
[Phase 1] KILN FLAME (Red)
          The Withering Kiln furnace is kindled first to banish moisture and dampness.
             |
             v
[Phase 2] MIDNIGHT MIST (Blue)
          Highland mist rolls through the loft slats to regulate humidity.
             |
             v
[Phase 3] SUNRISE GOLD (Yellow)
          The morning dawn sun dries the dew drops across the tea bushes.
             |
             v
[Phase 4] EMERALD LEAF (Green)
          Fresh two leaves and a bud are plucked and sealed into the holding chamber.
```

---

## 2. Spatial Layout & Master Workstation Console Design

### Unified Master Puzzle Workstation (`Master_PuzzleConsole`)
Rather than isolated loose pedestals, all controls are integrated into a single high-tech Glenmist Workstation Console:
- **Interactive CRT Monitor Screen:**
  - Displays real-time cipher slot progress: `[ RED: FIRE ]   [ ??? ]   [ ??? ]   [ ??? ]`
  - Shows dynamic phase guidance matching Appa's riddle.
  - Flashes red with circuit overload diagnostic on incorrect entry; turns emerald green on barrier release.
- **Scrambled Desk Panel Button Mounts:**
  The 4 interactive push buttons are mounted neatly in a single row on the desk panel surface:
  - **Button 1 (Leftmost, x = -0.6f):** Emerald Leaf Switch (Green)
  - **Button 2 (Mid-Left, x = -0.2f):** Sunrise Sun Switch (Yellow)
  - **Button 3 (Mid-Right, x = +0.2f):** Kiln Flame Switch (Red)
  - **Button 4 (Rightmost, x = +0.6f):** Midnight Mist Switch (Blue)

### Non-Linear Solution & Anti-Brute-Force Mechanics
A player attempting to press the desk buttons in naive physical left-to-right line order (`1 -> 2 -> 3 -> 4`) immediately inputs `Green` on step 1, triggering an instant security alarm, red warning light strobes, electrical spark discharge, and automated circuit reset.

To disengage the barrier, the player must execute Appa's Harvest Riddle:
$$\mathbf{Red \ (Button \ 3) \rightarrow Blue \ (Button \ 4) \rightarrow Yellow \ (Button \ 2) \rightarrow Green \ (Button \ 1)}$$

---

## 3. In-Game Clue Station Locations

All 4 clues are positioned within the accessible player zone in the laboratory:

| Clue ID | Form Factor | Room Position | Narrative Content |
| :--- | :--- | :--- | :--- |
| **Clue 1** | Security Terminal | South-West Desk | OVERSEER diagnostic log confirming reversed safety sequence. |
| **Clue 2** | Plantation Document | South Wall Table | Log detailing furnace heat followed by mountain mist. |
| **Clue 3** | Wall Inscription | West Wall Plaque | Inscription explaining morning sunlight before plucking. |
| **Clue 4** | Research Notes | Center Console | Appa's emergency cipher letter explicitly revealing the cycle. |

---

## 4. Failure & Reset Sequence

When an incorrect switch is engaged:
1. `ProceduralAudio.PlayAt("alarm", ...)` sounds a high-frequency klaxon.
2. Room warning lights strobe red at 5 Hz for 1.2 seconds.
3. Electrical spark discharge bursts from all switch consoles (`Vfx.Burst`).
4. Engaged switches power down to dormant states.
5. `FinalRescueUI` broadcasts the contextual hint: *"INCORRECT CIPHER! Appa's Riddle: Fire (Red) -> Mist (Blue) -> Sun (Yellow) -> Leaf (Green)"*.
