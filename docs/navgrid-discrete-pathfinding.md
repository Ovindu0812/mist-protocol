# Discrete NavGrid and A* Navigation Architecture

## 1. Grid Specification
- **Origin:** $(-32.0, 0.0, -10.0)$
- **Dimensions:** $\text{Width} = 176\text{ cells}$, $\text{Height} = 96\text{ cells}$
- **Cell Size:** $0.5\text{ meters}$
- **Spatial Coverage:**
  - $X \in [-32.0\text{m}, +56.0\text{m}]$ (encompassing Yard, Zones A, B, C, D, and Sector E)
  - $Z \in [-10.0\text{m}, +38.0\text{m}]$

---

## 2. Walkability Sampling & Geometry Fallback
1. **Primary NavMesh Sampling:**
   - Samples native Unity NavMesh via `NavMesh.SamplePosition`.
2. **Physics Geometry Fallback:**
   - If pre-baked NavMesh instances are unavailable in memory, automatic downward vertical raycasting samples solid floor geometry and tests obstacle spheres to dynamically classify walkable cells.
3. **Dynamic Obstacle Overlays:**
   - Barricades and closed doors dynamically invalidate discrete grid cells, triggering local A* re-planning via `NavGrid.RefreshArea(bounds)`.
