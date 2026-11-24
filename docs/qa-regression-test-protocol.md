# Automated QA Verification Protocol & Regression Test Suite

## 1. Quality Assurance Overview
This protocol establishes the standardized regression testing checklist for verifying combat interactions, AI pathfinding, boss phase transitions, and UI responsiveness.

---

## 2. Test Suite Categories
- **AI Navigation & Kinematics:**
  - `NavGridGeometryTests`
  - `PathfindingTests`
  - `AgentMotorKinematicsTests`
- **Combat & Boss Mechanics:**
  - `TitanBossStateTests`
  - `HealthComponentTests`
  - `WeaponDataTests`
- **Encounter & Progression:**
  - `FacilityEncounterSquadsTests`
  - `RollingRoomEncounterTests`
  - `WitheringLoftKeycardTaskTests`
  - `SecretLaboratoryDoorTests`
  - `PuzzleSequenceTests`

---

## 3. Execution Standard
All automated test fixtures must execute with 0 failures and 0 compilation warnings across Debug and Release profiles.
