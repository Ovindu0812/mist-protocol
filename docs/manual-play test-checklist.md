# 📋 Mist Protocol - Comprehensive Manual Playtest Checklist & QA Matrix

## 1. Test Environment & Build Metadata
- **Project**: Mist Protocol (Unity 6 URP + Procedural Audio)
- **Target Platform**: Windows 64-bit Desktop / DirectX 12
- **Testing Scope**: Core Movement, Weapons Arsenal, AI Combat, Titan Boss Encounter, Secret Laboratory Puzzle, Emergency Evacuation

---

## 2. Test Execution Matrix

### 2.1 Player Movement & Locomotion
| ID | Test Case | Action / Steps | Expected Result | Status |
| :--- | :--- | :--- | :--- | :--- |
| **MOV-01** | WASD Movement | Press W, A, S, D keys | Smooth directional movement at 4.2 m/s with procedural footstep audio. | [x] PASS |
| **MOV-02** | Sprint Locomotion | Hold Left Shift while moving forward | Movement speed accelerates to 6.8 m/s with enhanced camera head-bobbing. | [x] PASS |
| **MOV-03** | Crouch Locomotion | Hold Left Ctrl or C key | Camera height drops to 1.1m, speed drops to 2.0 m/s, near-silent acoustic signature. | [x] PASS |
| **MOV-04** | Jump & Landing | Press Spacebar while moving | Player ascends to 1.0m height; upon landing, procedural thud sound and subtle screen shake trigger. | [x] PASS |

---

### 2.2 Weapons & Ballistics Pipeline
| ID | Test Case | Action / Steps | Expected Result | Status |
| :--- | :--- | :--- | :--- | :--- |
| **WPN-01** | Primary Fire | Click Left Mouse Button | Fires active weapon with muzzle flash, tracer line, recoil pitch impulse, and procedural gunshot sound. | [x] PASS |
| **WPN-02** | Weapon Switching | Press 1, 2, 3, 4 or Q key / Scroll Wheel | Cycles between Mist Rifle, Shotgun, Revolver, and SMG with mechanical click SFX and HUD banner. | [x] PASS |
| **WPN-03** | Reload Mechanics | Press R key when magazine < max | Initiates procedural weapon dip animation and reload audio; replenishes magazine from reserve. | [x] PASS |
| **WPN-04** | Dry-Fire Audio | Click LMB when magazine is 0 | Plays mechanical dry-fire click; prevents shooting without crashing. | [x] PASS |
| **WPN-05** | Recoil Bloom | Fire continuous bursts | Crosshair dynamically expands outward; recovers smoothly when firing stops. | [x] PASS |

---

### 2.3 Tactical HUD & Audio Feedback
| ID | Test Case | Action / Steps | Expected Result | Status |
| :--- | :--- | :--- | :--- | :--- |
| **HUD-01** | Minimap Radar | Observe top-right circular radar | Factory blueprint scrolls beneath player marker; active enemy robots display orange blips. | [x] PASS |
| **HUD-02** | Objective Tracker | Move toward active objective | Minimap displays real-time distance in meters (`OBJ: Xm`); turns green `[NEAR]` when $\le 6\text{m}$. | [x] PASS |
| **HUD-03** | Low Health Pulse | Allow player HP to drop below 25% | Rhythmic procedural heartbeat audio plays every 0.95s; red vignette pulses on screen. | [x] PASS |
| **HUD-04** | Low Ammo Warning | Deplete magazine to $\le 25\%$ capacity | Ammo counter text flashes red warning indicator. | [x] PASS |

---

### 2.4 Titan Boss Encounter (Containment Vault)
| ID | Test Case | Action / Steps | Expected Result | Status |
| :--- | :--- | :--- | :--- | :--- |
| **BOS-01** | Vault Trap Sequence | Cross trigger threshold into vault | Emergency red siren lights activate, steam vents discharge, entrance door unseals, and Titan awakens. | [x] PASS |
| **BOS-02** | Phase 1 Armor | Shoot Titan with standard bullets | Bullets ricochet with metallic pings and spark bursts ($55\%$ damage reduction). | [x] PASS |
| **BOS-03** | Explosive Stagger | Shoot volatile gas canisters near Titan | High-explosive damage ($>40\text{ HP}$) bypasses armor, staggering the Titan for 3.5 seconds. | [x] PASS |
| **BOS-04** | Phase 2 Armor Fracture | Reduce Titan HP below 50% | Outer plating fractures with massive steam blast; damage multiplier rises to $1.35\times$; searchlight turns red. | [x] PASS |
| **BOS-05** | Ground Slam Shockwave | Stand within 5.5m of enraged Titan | Titan stomps ground; radial physics impulse knocks player back and inflicts 18 damage with seismic rumble. | [x] PASS |
| **BOS-06** | Boss Defeat & Loot | Deplete Titan HP to 0 | Slow-motion cinematic time dilation triggers; Master Prison Cell Key and Appa's Map spawn at wreckage. | [x] PASS |

---

### 2.5 Secret Laboratory & 4-Color Console Puzzle
| ID | Test Case | Action / Steps | Expected Result | Status |
| :--- | :--- | :--- | :--- | :--- |
| **LAB-01** | Blast Door Access | Interact with Lab Terminal using Master Key | Blast doors slide open with hydraulic audio, granting access to the Secret Laboratory. | [x] PASS |
| **LAB-02** | Clue Stations | Inspect 4 stations (Terminal, Document, Wall, Log) | Displays lore hints; HUD increments tracker (`LAB CLUES: 0/4` to `4/4`). | [x] PASS |
| **LAB-03** | Incorrect Sequence | Activate switches out of order | Plays alarm SFX, flashes red warning banner and lights, resets all switches to inactive. | [x] PASS |
| **LAB-04** | Correct Sequence | Activate Blue ➔ Yellow ➔ Green ➔ Red | Plays success chime, all switches glow steady green, and prison energy barrier deactivates! | [x] PASS |

---

### 2.6 Evacuation & Mission End State
| ID | Test Case | Action / Steps | Expected Result | Status |
| :--- | :--- | :--- | :--- | :--- |
| **ESC-01** | Evacuation Timer | Speak to Father after puzzle is solved | Triggers 60-second emergency facility collapse countdown timer with live progress bar. | [x] PASS |
| **ESC-02** | Escape Point Trigger | Reach evacuation jeep in factory yard | Displays Victory Screen with stats, story outro, and clickable Play Again / Main Menu buttons. | [x] PASS |
| **ESC-03** | Collapse Timeout | Allow timer to expire before reaching jeep | Displays Mission Failed Screen with red flash, prompt, and clickable Retry / Menu buttons. | [x] PASS |
