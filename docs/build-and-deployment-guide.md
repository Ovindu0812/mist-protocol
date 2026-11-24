# Build and Deployment Guide

This document outlines the build configuration standards, platform compilation targets, standalone packaging procedures, and pre-release verification checklist for Mist Protocol.

---

## 1. Build Environment Standards

- **Target Engine:** Unity 6000.0.x / Unity 2022.3 LTS
- **Scripting Backend:** IL2CPP (Release Target), Mono (Development/QA Target)
- **API Compatibility Level:** .NET Standard 2.1 / .NET Framework 4.8
- **Graphics API:** DirectX 11 / DirectX 12 (Windows Standalone x64), Vulkan fallback

---

## 2. Compilation and Code Verification

Prior to pushing changes or generating player binaries, execute local compilation checks to confirm zero warnings and zero syntax or assembly reference errors:

```powershell
# Run headless Roslyn C# verification
dotnet build "Assembly-CSharp.csproj" --no-restore
```

### 2.1 Quality Standards
- Strict adherence to nullable reference types and boundary assertions.
- All non-essential allocations in high-frequency update loops (`Update()`, `FixedUpdate()`) must be pre-allocated or pooled.
- Clean separation between gameplay logic and runtime presentation assets.

---

## 3. Pre-Release QA Checklist

1. [ ] **NavMesh Bake:** Verify `NavGrid` bounds span all sectors including Sector E Vault ($X \in [-32, 56]$, $Z \in [0, 96]$).
2. [ ] **Titan Boss Room:** Confirm `BossChamberTrapTrigger` arming logic and electrical breaker sequence.
3. [ ] **Holding Cell Rescue:** Confirm `SecretLaboratoryDoor` responds to `MasterCellKey` dropped by Titan.
4. [ ] **AI Locomotion:** Ensure Sentinels, Drones, and Saboteurs execute movement without disabling physics colliders.
5. [ ] **UI Presentation:** Confirm HUD scalar properly scales across 16:9, 16:10, and 21:9 aspect ratios.
