# 📏 Mist Protocol - Engineering Standards & Code Style Guidelines

## 1. Principles & Objectives
The Mist Protocol codebase prioritizes maintainability, zero runtime garbage collection pressure, explicit modular dependencies, and clear separation of concerns.

---

## 2. C# Language & Scripting Conventions

### 2.1 Naming Rules
- **Classes, Structs & Enums**: `PascalCase` (e.g. `TitanBossBrain`, `SwitchColor`).
- **Interfaces**: Prefixed with `I` in `PascalCase` (e.g. `IInteractable`, `IPlayerState`).
- **Public Properties & Methods**: `PascalCase` (e.g. `TakeDamage()`, `CurrentBloom`).
- **Private Member Fields**: Prefixed with underscore `_camelCase` (e.g. `_stepDistance`, `_weapon`).
- **Serialized Inspector Fields**: `camelCase` without underscore (e.g. `[SerializeField] private float sprintSpeed;`).
- **Constants**: `PascalCase` (e.g. `RadarRadius`, `WorldViewRange`).

### 2.2 Memory & Allocation Guidelines
- **Avoid LINQ in Hot Paths**: LINQ allocations in `Update()`, `LateUpdate()`, or `Think()` loops are strictly prohibited due to heap garbage generation.
- **Pre-Allocated Buffers**: Use non-allocating physics queries (`Physics.OverlapSphereNonAlloc`, `Physics.RaycastNonAlloc`).
- **Procedural Synthetics**: DSP audio and procedural meshes must be generated once during initialization or level build, never per frame.

---

## 3. Git Workflow & Commit Guidelines

### 3.1 Conventional Commit Format
All commit messages must adhere to the Conventional Commits specification:
```
<type>(<scope>): <concise description in imperative present tense>
```
- **Allowed Types**:
  - `feat`: A new gameplay feature, asset, mechanic, or user interface element.
  - `fix`: A bug fix or compiler warning resolution.
  - `docs`: Technical documentation, playtest checklists, or architecture guides.
  - `chore`: Asset pipeline changes, build scripts, or folder maintenance.
  - `refactor`: Code restructuring without modifying behavior.

### 3.2 Atomicity
- Commits must be **atomic**: Each commit represents one self-contained, logically coherent modification.
- Commits must compile cleanly without introducing broken references or compiler errors.
