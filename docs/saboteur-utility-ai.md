# Saboteur Quadruped Crawler Utility AI Specification

## 1. Unit Overview
The Saboteur is a low-slung quadruped crawler automated robot designed to compromise player cover, manipulate tea crates, and conduct surprise flanking attacks from unexpected low-angle approaches.

---

## 2. Utility AI Decision Engine
Rather than relying on finite state machines or GOAP trees, the Saboteur scores available actions continuously through weighted utility curves:
- **DragBarricadeAction:** Prioritizes displaced barricades to restore factory defensive geometry or deny player cover.
- **ProwlAction:** Sneaks along perimeter walls, avoiding direct line of sight.
- **AmbushAction:** Sudden burst acceleration to disorient the player when in close proximity.

---

## 3. Kinetic Attributes
- **Base Speed:** $3.4\text{ m/s}$
- **Movement Momentum:** High turning agility with low body profile ($H \approx 0.65\text{m}$)
- **Auditory Footprint:** Low acoustic frequency walking clatter.
