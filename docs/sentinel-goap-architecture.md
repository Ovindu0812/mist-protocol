# Sentinel GOAP (Goal-Oriented Action Planning) Architecture

## 1. System Overview
The Sentinel tactical robot utilizes a Goal-Oriented Action Planner (GOAP) to dynamically select and execute combat behaviors based on environmental states and sensory inputs.

---

## 2. GOAP Action Set
- **PatrolAction:** Loops across predefined waypoint transforms.
- **InvestigateAction:** Routes toward auditory stimuli (gunshots, footsteps, crate impacts).
- **AlertSquadAction:** Broadcasts player sightings to nearby squad members within radio range.
- **TakeCoverAction / ShootFromCoverAction:** Evaluates tactical cover positions behind obstacles.
- **FlankAction / ApproachAction:** Positions Sentinel for optimal firing line-of-sight.

---

## 3. Sensory Input Channels
- **Vision:** Frustum field of view cone ($75^\circ$) with raycast occlusion checks.
- **Audition:** Decibel threshold acoustic stimulus propagation registered via `SoundSystem`.
