# MIST PROTOCOL

Low-poly 3D first-person shooter set in a storm-locked Ceylon tea factory.
SE3032 Graphics & Visualization + SE3062 Intelligent Systems joint project.
Built with Unity 6 (6000.6.4f1, URP) and Blender 5.2.

## Story

Nuwara Eliya hills, 02:14 AM. For 94 years the **Glenmist Tea Factory** rolled
Ceylon tea. Last year the company installed **OVERSEER**, an AI built to run the
factory without workers. Tonight a storm hit. OVERSEER locked the gates and
turned the maintenance robots into weapons.

**Nila**'s father, the night supervisor, sent one last radio message:
*"The kill switch is in the control room. My keycard is on my desk in the loft..."*

1. **Front Gate** (yard): spawn point, in the mist.
2. **Withering Loft**: find Appa's keycard on his desk.
3. **Rolling Room**: swipe the keycard to unlock the Drying Chamber gate.
4. **Drying Chamber**: full of pressurised gas canisters.
5. **Control Room**: hold the kill switch for 8 s while OVERSEER throws everything at you.
   Ending: the machines fall silent... and Appa answers the radio.

## Controls

| Key | Action |
|---|---|
| WASD / Mouse | Move / look |
| LMB / R | Shoot / reload |
| Shift / Ctrl or C | Sprint (loud) / crouch (quiet, beats drone sonar) |
| E (hold for kill switch) | Use doors, card reader, kill switch |
| F, then LMB | Pick up a crate or canister, then throw it (a thrown crate makes noise and lures drones) |
| Walk into big crates | Push barricades |
| F1 | **AI debug overlay**: each agent's live decision and plan, planned paths, sound rings, director state |
| Enter | Start / restart |

## Running it

1. Open the `MistProtocol` folder in Unity Hub (editor 6000.6.4f1).
2. Open `Assets/_Project/Scenes/GlenmistFactory.unity` and press Play.

The whole level is generated from code, so you can rebuild it at any time:

- **Menu → Mist Protocol → Build Everything + Bake Lighting** (or *no lightmap bake* for a fast rebuild)
- Headless: `Unity -batchmode -quit -projectPath . -executeMethod MistProtocol.EditorTools.MistBuild.BuildAll`
- Re-export the Blender models: `Blender -b -P Art/Blender/build_models.py`
- Automated play-through test: **Mist Protocol → Run Smoke Test**. Writes `Logs/smoke/report.txt` and screenshots.

---

## Team roles and ownership

Grades are based on **your own Git commits**. Commit only in your own folders,
in small, regular commits. In the viva, everyone must be able to explain their own code.

### Graphics & Visualization roles

| Student | GV role | Owns (files) |
|---|---|---|
| 1 | **World Builder**: level, lighting, texturing, NavMesh | `Editor/LevelBuilder.cs`, `Editor/TextureFactory.cs`, `Editor/MeshFactory.cs`, `Editor/MaterialFactory.cs`, `Editor/MistBuild.cs` (NavMesh/light bake), `Scripts/World/` |
| 2 | **Systems Engineer**: player interaction physics | `Scripts/Player/`, `Scripts/Interaction/` (HingeJoint doors, pushable barricades, throwables, exploding canisters) |
| 3 | **Core Developer**: custom 3D models | `Art/Blender/build_models.py` (9 models, palette UVs), `Editor/MistImportSettings.cs`, `Editor/PrefabFactory.cs` |
| 4 | **Agent Controller**: path to smooth motion and animation | `Scripts/Animation/` (AgentMotor path smoothing/steering, Walker/Drone/Spider procedural animators) |

### Intelligent Systems roles (one agent each)

| Student | Agent | Architecture | Files |
|---|---|---|---|
| 1 | **Hunter Drone** | FSM + Dijkstra sound propagation + decaying suspicion heat map + sonar | `AI/HunterDrone/`, `AI/Perception/SoundSystem.cs` |
| 2 | **Sentinel** | GOAP (forward A* over a bitmask world state) + squad blackboard + tactical A* flank paths | `AI/Sentinel/` |
| 3 | **Saboteur** | Utility AI (7 actions, response curves, compensation factor, momentum) | `AI/Saboteur/` |
| 4 | **OVERSEER Director** | Pacing FSM (BuildUp, Peak, Relax, Final) + player profile EMA + influence map + A*-scored spawn selection | `AI/Director/` |

Shared by everyone: `AI/Navigation/` (NavGrid, weighted A*, string-pulling), `AI/Perception/VisionSensor.cs`, `AI/Agents/`.

---

## Viva notes: design and optimization justification

### Visual cohesion and art direction
- One art style: low-poly, flat-shaded, "hill-country tea factory at night". Lime-washed walls, green corrugated cladding, red brick, warm sodium lamps against cold moonlit mist.
- **Every custom model shares one 256×256 palette texture and one material** (`M_MistPalette`). That gives a consistent look and lets the SRP Batcher and GPU instancing batch the draw calls.

### Modeling (Core Developer)
- 9 models built from scratch in Blender with Python, so they are fully reproducible. Triangle counts: Sentinel 992, Drone 524, Saboteur 376, Rifle 280, props 84–320.
- UVs: Smart UV Project, then each part's islands are packed inside its colour swatch with a 6 px margin. Point filtering and no mipmaps, so colours never bleed.
- Moving parts (legs, arms, rotors, claw) are separate objects whose pivot sits on the joint. That allows procedural animation without a skinned mesh, which costs less CPU.
- Import settings are enforced in code: bake axis conversion, mesh compression, read/write off, lightmap UV2.

### World building
- Procedural, tileable textures (fBm noise on a wrapped domain), with normal maps derived from the height field.
- Box meshes use **world-scaled UVs**, so texel density is identical everywhere. Identical sizes share one mesh asset.
- Lighting: baked point lights (Progressive GPU, 2 bounces, AO), a Mixed moon light, a few realtime flicker lamps, and the player's realtime shadowed headlamp. Exponential-squared fog plus soft mist particles. URP post-processing: ACES, bloom, vignette, grain.
- Static batching, baked occlusion culling, and four reflection probes.
- The NavMesh bake collects only the level hierarchy. Dynamic objects are excluded with `NavMeshModifier`.

### Physics (Systems Engineer)
- Doors are real `HingeJoint` + spring bodies. They can be shoved, shot, and opened by agents.
- Barricades are 60 kg rigidbodies. When one settles, it rebuilds part of the NavGrid, so agents re-path at once.
- Held props are moved by setting their velocity toward a hold point, not by parenting. They still collide with the world, so they can't be pushed through walls.
- Gas canisters use `AddExplosionForce` with falloff damage, and can set off chain reactions.
- Impact noise is fed into the AI hearing system.

### Agent movement (Agent Controller)
- The A* cell array goes through string-pulling, then Catmull-Rom resampling, then pure-pursuit steering. Acceleration is limited and agents slow down on arrival.
- Yaw is slerped toward the travel direction, or toward an aim target (for strafing). Agents also keep separation from each other.
- Gait phase is driven by distance travelled, so feet don't slide at any speed.

### AI performance
- Brains think at 8 Hz, not every frame. A* uses stamped buffers (nothing to clear between searches) and a binary heap.
- Sound propagation is bounded by loudness. The GOAP search is capped at a few hundred nodes.
