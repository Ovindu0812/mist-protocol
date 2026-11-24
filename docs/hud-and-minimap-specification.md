# HUD and Minimap Specification

This document details the visual hierarchy, real-time widget layouts, dynamic reticle dynamics, and circular tactical minimap rendering systems in Mist Protocol.

---

## 1. Visual Hierarchy & Screen Layout

The in-game HUD is rendered using a unified `ScreenSpaceOverlay` canvas pinned to a reference resolution of $1920 \times 1080$ ($0.5$ match width/height):

- **Top-Right — Tactical Minimap:** Circular radar display showing local factory terrain, detected enemy pings, objective waypoints, and father clue locations.
- **Top-Center — Boss Threat Meter:** Dynamically appears during the Titan encounter in Sector E. Shows segmented armored health bar with "ghost bar" drain animations.
- **Bottom-Left — Vital Status:** Player health gauge with progressive color shifts (Green $\to$ Amber $\to$ Flashing Red) and sprint stamina bar.
- **Bottom-Right — Weapon Status:** Current weapon nomenclature, active magazine capacity, reserve stock, and procedural reload progress indicators.
- **Top-Left — Mission Objective Tracker:** Active operation objectives, distance readouts, and sub-task checklists.

---

## 2. Dynamic Crosshair Reticle & Bloom

The central crosshair dynamically articulates based on weapon accuracy and player locomotion:
- **Stationary / Aiming:** Minimal gap spacing ($4\text{px}$).
- **Walking / Running:** Expanded spread aperture reflecting added motion penalty.
- **Recoil Bloom:** Expands in proportion to accumulated heat (`_heat * BloomMultiplier`).
- **Hit Confirmation:** Renders an animated white-to-red chevron hitmarker upon confirmed projectile connection.

---

## 3. Minimap Blip Rendering Pipeline

The minimap renders spatial radar data within a clipped circular mask:
- **Player Icon:** Centered chevron pointing in the camera forward azimuth.
- **Enemy Contacts:** Red circular blips, with acoustic pulse expansion when enemies fire or run.
- **Objective Waypoints:** Yellow diamond icons clamped to the radar circumference when out of immediate range ($> 40\text{m}$).
- **Appa Clue Waypoints:** Cyan pulsating pings indicating undiscovered environmental evidence.
