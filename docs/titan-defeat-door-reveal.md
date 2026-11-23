# Titan Defeat & Hidden Blast Door Reveal Sequence - Mist Protocol

This document outlines the state machine flow, audiovisual feedback, and objective progression triggered when Titan Enforcer Unit-94 is neutralized in Sector E.

---

## 1. Sequence Trigger & Event Propagation

```
[TitanBossBrain Death]
         |
         v
[GameManager.Instance.OnTitanDefeated()]
         |
         +--> Sets IsTitanDefeated = true
         +--> Broadcasts Overseer alert & drops Master Cell Key
         +--> Invokes FinalRescueMission.Instance.OnTitanDefeated()
                    |
                    v
         [SecretLaboratoryDoor.RevealDoor()]
                    |
                    +--> Spawns hydraulic steam & floor dust VFX
                    +--> Plays "door", "steam", and "zap" procedural audio cues
                    +--> Powers up keycard terminal status light (orange standby)
                    +--> Smoothly animates Lab_WallCoverPlate down into the floor (-3.8m)
                    +--> Points HUD Objective Waypoint directly to the door terminal
```

---

## 2. Concealment & Emergence Design

### Pre-Defeat State (Concealed Wall)
- **Wall Cover Plate (`Lab_WallCoverPlate`):** Matches the concrete/metal texture of the Sector E perimeter wall (`localScale = (0.25, 3.55, 2.5)`).
- **Interactability:** `CanInteract` returns `false`.
- **Prompt:** `[Sealed Wall Mechanism - Defeat Titan]`.

### Reveal State (Post-Defeat)
- **Cover Plate Retraction:** Interpolates downwards into the foundation using `Mathf.SmoothStep` over 2.0 seconds.
- **Dust & Steam Venting:** High-pressure steam vents from the upper lintel while ground dust bursts from the base.
- **Terminal Activation:** Terminal point light illuminates at a calibrated intensity of `1.4f` (range `3.2f`) to prevent bloom glare.
- **Key Requirement:** Prompts the player to swipe the Master Access Key dropped from Titan's core wreckage.

---

## 3. Laboratory Entry Sequence

1. Player interacts with terminal using `[E] Insert Master Access Key into revealed door terminal`.
2. Master Key authorization is verified.
3. Heavy sliding blast door slides open along the Z-axis (+2.6m) over 1.8 seconds.
4. Door colliders disable, granting unobstructed access into the Secret Laboratory.
5. In-room lighting activates and Appa's emergency radio broadcast is received.
