# 📦 Drying Chamber Tactical Layout & Cargo Container Level Design

This document details the spatial integration of industrial shipping containers within the **Drying Chamber (Zone 2)** of Mist Protocol.

---

## 1. Design Objectives

Prior to this integration, the Drying Chamber presented a large, wide $20\text{ m} \times 24\text{ m}$ open warehouse expanse between the two central ECP tea dryers. The introduction of standardized 20ft and 40ft cargo containers achieves three core gameplay objectives:

1. **Line-of-Sight Breaking:** Obstructs long-range Sentinel Bot line-of-sight upon breach from the Rolling Room keycard gate.
2. **Tactical Flanking Routes:** Creates narrow CQB alleyways along the western and eastern perimeter walls.
3. **Verticality & High Ground:** Features a stacked 20ft container atop the southern 40ft freight line ($Y = 2.59\text{ m}$) for dynamic elevation.

---

## 2. Container Placement Coordinates

| ID | Object Name | Container Spec | World Position $(X, Y, Z)$ | Rotation (Yaw) | Tactical Purpose |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **C1** | `Container_SW_Gate` | 20ft Teal | $(13.5, 0.0, 3.8)$ | $12^\circ$ | Immediate cover upon entering from Rolling Room gate |
| **C2** | `Container_S_Wall_40ft` | 40ft Red | $(22.0, 0.0, 1.8)$ | $0^\circ$ | Heavy freight barricade along southern boundary |
| **C3** | `Container_S_Stacked_Upper` | 20ft Teal | $(21.0, 2.59, 1.8)$ | $0^\circ$ | Stacked elevated cover atop C2 |
| **C4** | `Container_W_Flank` | 20ft Teal | $(11.6, 0.0, 13.5)$ | $90^\circ$ | Flanking corridor between Dryer 1 and West Wall |
| **C5** | `Container_E_VaultStaging` | 20ft Red | $(28.4, 0.0, 8.5)$ | $90^\circ$ | Breaching staging cover outside Sector E Vault Door |
| **C6** | `Container_N_ControlBarrier_40ft` | 40ft Red | $(17.5, 0.0, 21.5)$ | $-8^\circ$ | Checkpoint cover in front of Control Room door |

---

## 3. Collision & Navigation Specifications

- **Box Collider Extents:**
  - 20ft: $6.06\text{ m (L)} \times 2.59\text{ m (H)} \times 2.44\text{ m (W)}$
  - 40ft: $12.19\text{ m (L)} \times 2.59\text{ m (H)} \times 2.44\text{ m (W)}$
- **NavGrid Integration:** Static colliders are baked into the `NavGrid` cellular cost matrix, ensuring AI agents dynamically navigate around container corners and seek cover when pinned by player fire.
