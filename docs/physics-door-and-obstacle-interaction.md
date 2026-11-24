# Physics Door and Obstacle Interaction

This document outlines the architecture and runtime dynamics of simulated physical doors, dynamic hinges, and interactive obstacles across the Glenmist facility.

---

## 1. Joint Physics & Springs

Doors in the Glenmist facility are not animated via keyframes; they are simulated physical `Rigidbody` entities attached to anchor posts via Unity `HingeJoint` components:

- **Spring Force:** 120.0 N·m drives the door toward its target angular displacement.
- **Damper:** 18.0 N·m·s absorbs oscillation to prevent perpetual swinging.
- **Angular Limits:** Hinge limits are bounded symmetrically between `-openAngle` and `+openAngle` (typically $\pm 100^\circ$).

---

## 2. Dynamic Obstructions & Jamming

Because the door is an active physical object:
- Heavy cargo crates, debris, and fallen machinery can physically wedge into the doorway gap, preventing the spring joint from achieving full closure.
- High-velocity bullet impacts apply angular impulses (`AddForceAtPosition`), causing unlatched doors to visibly recoil or kick open upon being shot.
- Enemy AI pathfinding periodically samples `DoorCells` on the `NavGrid`. If a door is closed, agents will approach and trigger an interaction to swing it open.

---

## 3. Containment Vault Special Rules

For `Door_Containment` (Sector E Titan Chamber):
- The door is initialized wide open ($120^\circ$) to permit unobstructed passage.
- `Physics.IgnoreCollision` is established between the door leaf collider and the player's `CharacterController`, guaranteeing that sudden physics jitter cannot pin or clip the player into geometry.
- Locking calls on `Door_Containment` are intercepted and rejected, ensuring the player is never trapped by inadvertent trigger events.
