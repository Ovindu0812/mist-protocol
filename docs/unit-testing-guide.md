# Automated Unit Testing Guide - Mist Protocol

This guide outlines the automated unit test architecture, suites, and procedures implemented for Mist Protocol using Unity's Test Framework (NUnit integration).

---

## 1. Test Architecture Overview

Unit tests in Mist Protocol are located in `Assets/_Project/Tests/` and organized into isolated test fixtures that validate core gameplay mathematical models, algorithmic soundness, and state machines without requiring full scene loading.

| Test Fixture | Target Component / Subsystem | Verification Scope |
| :--- | :--- | :--- |
| `HealthComponentTests` | `MistProtocol.Core.Health` | Damage absorption, healing clamps, invulnerability frames, and death events. |
| `PathfindingTests` | `MistProtocol.AI.Nav.MinHeap` | Binary heap priority ordering, heapify sift-up/down, and Manhattan heuristic calculations. |
| `WeaponDataTests` | `MistProtocol.Player.WeaponDef` | Ammo consumption, reload reserve math, empty-magazine dryfire states, and fire rate intervals. |
| `PuzzleSequenceTests` | `MistProtocol.Interaction.PuzzleColorSwitch` | 4-color switch ordering, sequence comparison, and incorrect input reset logic. |
| `InfluenceMapTests` | `MistProtocol.AI.InfluenceMap` | Threat propagation, distance falloff, decay over time, and multi-agent spatial accumulation. |

---

## 2. Test Suites Reference

### 2.1 HealthComponentTests (`HealthComponentTests.cs`)
- **`Damage_DecreasesHealth_AndTriggersDamagedEvent`**: Verifies that invoking `TakeDamage(30f)` on a 100 HP entity decrements current health to 70 and raises `Damaged` with exact parameters.
- **`Healing_ClampsAtMaxHealth`**: Ensures that applying healing does not exceed `MaxHealth`.
- **`FatalDamage_TriggersDiedEvent_AndSetsZero`**: Confirms that receiving damage greater than current HP clamps health to 0 and invokes `Died`.
- **`Invulnerability_BlocksDamage`**: Validates that when invulnerability frames are active, incoming damage calls are discarded.

### 2.2 PathfindingTests (`PathfindingTests.cs`)
- **`MinHeap_PopsLowestCostElementFirst`**: Tests the binary priority queue to ensure nodes with lower `F = G + H` costs are dequeued prior to higher-cost nodes.
- **`MinHeap_MaintainsOrderOnRandomInserts`**: Confirms heap invariant holds under stress insertions of arbitrary cost sequences.
- **`Heuristic_CalculatesCorrectManhattanDistance`**: Checks grid Manhattan distance metric for admissible A* path search heuristics.

### 2.3 WeaponDataTests (`WeaponDataTests.cs`)
- **`Fire_ConsumesAmmoFromCurrentMagazine`**: Validates that single-shot and burst firing subtract the correct ammo counts.
- **`Reload_TransfersAmmoFromReserveUpToCapacity`**: Tests partial and full reload transfers, handling cases where reserves have fewer rounds than magazine capacity.
- **`EmptyMagazine_PreventsFiringAndTriggersDryfire`**: Asserts that firing with zero ammo does not decrement below zero and flags dryfire conditions.

### 2.4 PuzzleSequenceTests (`PuzzleSequenceTests.cs`)
- **`CorrectSequence_SolvesPuzzle`**: Simulates activating switches in the target sequence (e.g., Red -> Blue -> Yellow -> Green) and confirms puzzle solved state.
- **`IncorrectSequence_ResetsActiveSwitches`**: Simulates entering a wrong sequence, confirming that activated switches reset to default states.

### 2.5 InfluenceMapTests (`InfluenceMapTests.cs`)
- **`ThreatPropagation_DecaysWithDistance`**: Verifies that threat levels fall off monotonically according to spatial distance from an enemy position.
- **`TemporalDecay_ReducesStaleThreat`**: Confirms that over elapsed simulation frames, unrefreshed threat grid cells decay toward zero.

---

## 3. Running Tests

### Option A: Unity Editor Test Runner
1. In the Unity top menu bar, navigate to **Window > General > Test Runner**.
2. Select the **EditMode** or **PlayMode** tab.
3. Click **Run All** to execute the entire test suite, or double-click an individual fixture to run targeted tests.
4. Green checkmarks indicate passed test assertions; failures report line numbers and mismatch diffs in the inspector console.

### Option B: Command-Line Interface (CI / Automated Build)
Run tests headlessly via Unity's command-line batchmode:
```bash
Unity.exe -runTests -batchmode -projectPath . -testResults ./TestResults.xml -testPlatform EditMode
```
The resulting `TestResults.xml` standard NUnit XML file can be parsed by CI pipelines (GitHub Actions, Jenkins, or GitLab CI).
