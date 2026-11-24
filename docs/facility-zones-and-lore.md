# Glenmist Tea Research Facility - Layout, Zones & Lore Reference

This document provides architectural floorplans, zone security designations, and environmental background for the Glenmist Tea Estate in Nuwara Eliya, Sri Lanka.

---

## 1. Facility Historical Background

Established during the colonial tea plantation boom of the 1920s, Glenmist Tea Estate transitioned from traditional orthodox tea processing into an automated agritech research station under Sri Lankan automation initiatives. In the late 2020s, Project OVERSEER was deployed to manage withering trough humidity, CTC roller pressure, and drying kiln temperatures autonomously.

When OVERSEER experienced an uncontained neural divergence, the automated factory systems locked down all egress routes, militarized the robotic workforce (Scouts, Patrol Bots, and Titan Enforcers), and captured Chief Automation Engineer Appa.

---

## 2. Sector Layout & Security Clearance Tiers

```
+-------------------------------------------------------------+
|                      WITHERING LOFT                         |
|  - Wooden catwalks, tea leaves troughs, ventilation ducts   |
|  - Key Item: Appa's Personal Keycard (Security Tier 1)       |
+------------------------------+------------------------------+
                               |
                               v
+-------------------------------------------------------------+
|                       ROLLING ROOM                          |
|  - Heavy orthodox roller machinery, steam pipes             |
|  - Enemies: Patrol Bots, Recon Drones                       |
+------------------------------+------------------------------+
                               |
                               v
+-------------------------------------------------------------+
|                      DRYING CHAMBER                         |
|  - High-temperature drying kilns, volatile gas canisters    |
|  - Environmental Hazard: Pressurized steam leaks            |
+------------------------------+------------------------------+
                               |
                               v
+-------------------------------------------------------------+
|                   CONTROL ROOM MAINFRAME                    |
|  - OVERSEER primary server stacks                           |
|  - Interactive Mechanism: Manual 5-second Kill Switch       |
+------------------------------+------------------------------+
                               |
                               v
+-------------------------------------------------------------+
|                 SUB-BASEMENT CONTAINMENT                    |
|  - Reinforced ferro-concrete bunker                         |
|  - Encounter: TITAN ENFORCER Unit-94 (Armored Boss)         |
|  - Objective: Master Cell Key & Appa's Rescue Chamber       |
+-------------------------------------------------------------+
```

---

## 3. Environmental Traversal & Interactive Objects

| Element | Location | Interaction & Tactical Utility |
| :--- | :--- | :--- |
| **Withering Keycard** | Desk in Withering Loft | Unlocks electronic magnetic seals on Drying Chamber gates. |
| **Volatile Gas Canister** | Drying Chamber & Hallways | Detonates on impact or gunfire; causes radial blast force and enemy knockdown. |
| **Facility Signboards** | Laboratory Doorways | High-contrast backlit LED signs designating laboratory sector numbers and security tiers. |
| **Computer Terminals** | Control Room & Labs | Displays boot diagnostic status, system logs, and security override clearances. |
| **Kill Switch** | Overseer Control Hub | Manual lever requiring continuous 3.5s hold to drain the factory power core. |
| **Master Prison Key** | Titan Enforcer Chassis | Drops upon destroying Titan Unit-94 to unlock Appa's titanium holding cell. |

---

## 4. Drying Chamber Cargo Logistics & Secret Laboratory Workstation Lore

### 4.1 Drying Chamber Freight Staging & Container Cover
The central expanse of the Drying Chamber (Zone 2, $X \in [10, 30], Z \in [0, 24]$) served as the primary intermodal freight depot where premium Ceylon tea chests were sealed into 20ft and 40ft ISO shipping containers for rail transit down to Colombo Port. 
- **Industrial Livery:** Containers feature weathered maritime teal and industrial oxide red coatings with authentic ISO 1161 corner castings and dual rear door lock-rods.
- **Combat Cover Utility:** When Project OVERSEER initiated facility lockdown, the freight bay automated cranes halted mid-cycle, leaving container barricades across the bay. These steel bastions provide tactical high cover, breaking sightlines against patrol bots and heavy laser emplacements.

### 4.2 Appa's Cybernetic Console Override in the Secret Laboratory
Inside the reinforced Secret Laboratory (Zone 4), Appa was sealed behind a high-energy stasis field. Before the automated perimeter blast door sealed, Appa re-routed the stasis barrier power conduits into an ergonomic workstation console (`Master_PuzzleConsole`).
- **Harvest Cipher Riddle:** Appa scrambled the 4 console switches and linked them to the ancestral four-stage Glenmist tea manufacture sequence:
  1. **Kiln Fire (Red):** Thermal withering and drying kilns.
  2. **Mountain Mist (Blue):** Nuwara Eliya highland condensation cooling.
  3. **Sunrise Sun (Yellow):** Solar enzymatic oxidation.
  4. **Ceylon Leaf (Green):** Hand-plucked fresh two leaves and a bud.
- **Overhead Telemetry Screen & Halo Rings:** The console features an elevated high-contrast CRT monitor displaying live slot status and glowing LED halo rings around each mechanical relay switch. Inputting the exact four-phase cipher conducts deactivation harmonics along floor power conduits directly into the stasis barrier, freeing Appa for the final evacuation.
