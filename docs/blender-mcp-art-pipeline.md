# Blender MCP Parametric 3D Art Pipeline: Industrial Shipping Containers

## 1. Pipeline Overview
The industrial shipping containers in **Mist Protocol** are procedurally and parametrically authored using the Blender Model Context Protocol (MCP) server bridge. This workflow allows direct Python scripting into the live Blender 5.2 scene, generating high-fidelity mechanical and structural assets that are immediately exported to FBX and ingested into Unity's Universal Render Pipeline (URP).

```
+-------------------+      MCP Socket Bridge      +-------------------+      FBX Exporter      +-------------------+
|  Antigravity AI   |  =======================>   |    Blender 5.2    |  ===================>  |    Unity 6 URP    |
|  Parametric Spec  |     (127.0.0.1:port)        |  Procedural Mesh  |    Scale 1.0 / Z-Up    |  PBR Materials &  |
+-------------------+                             +-------------------+                        |  NavMesh Obstacles|
                                                                                               +-------------------+
```

---

## 2. ISO Standard Dimensions & Geometry Specifications

The shipping containers adhere to standard **ISO 668 Series 1** freight container dimensions:

| Dimension / Spec | 20-Foot Standard Container (22G1) | 40-Foot Standard Container (42G1) |
|---|---|---|
| **Length (L)** | 6.058 m (scaled ~6.0 m) | 12.192 m (scaled ~12.0 m) |
| **Width (W)** | 2.438 m (scaled ~2.4 m) | 2.438 m (scaled ~2.4 m) |
| **Height (H)** | 2.591 m (scaled ~2.6 m) | 2.591 m (scaled ~2.6 m) |
| **Max Gross Mass** | 30,480 kg | 32,500 kg |
| **Tare Weight** | 2,173 kg | 3,700 kg |

---

## 3. Parametric Modeling Procedures

### 3.1 Structural Perimeter Framework & Corner Castings
1. **Corner Castings (ISO 1161):**
   - 8 cast-steel corner blocks placed at $( \pm \frac{W}{2}, \pm \frac{L}{2}, \pm \frac{H}{2} )$.
   - Dimension: $0.20 \times 0.20 \times 0.20\,\text{m}$.
   - Characteristic twistlock apertures boolean-cut or chamfered for industrial realism.
2. **Main Framing Rails:**
   - 4 longitudinal bottom/top side rails ($120 \times 60\,\text{mm}$ hollow structural sections).
   - 4 transverse front and rear cross-members.
   - 4 structural corner posts with 3mm plate thickness.

### 3.2 Corrugated Side and Roof Panels
- **Profile:** Standard trapezoidal fluting.
- **Pitch:** 12 to 14 corrugation ridges per 6-meter span.
- **Depth:** $36\,\text{mm}$ emboss depth, creating dynamic shadow lines under directional and volumetric spotlights.
- **Roof:** Transverse camber arch with anti-pooling gutter contours.

### 3.3 Rear End Doors & Locking Hardware
- **Twin Cargo Leaf Doors:** Recessed $50\,\text{mm}$ into the rear door frame.
- **Hinge Straps:** 4 high-tensile steel blade hinges per leaf, fixed to the corner posts.
- **Vertical Locking Rods:** Dual galvanized steel lock rods ($28\,\text{mm}$ diameter) per door leaf.
- **Cam Keepers & Handles:** Top and bottom latching cams engaging the door sill, complete with horizontal operating lever handles.

---

## 4. PBR Material Architecture

Containers feature dual production color schemes representing Glenmist industrial logistics:

### 4.1 Weathered Maritime Teal (`Container_MaritimeTeal`)
- **Base Color:** `#1C4E5E` / `RGB(0.11, 0.31, 0.37)`
- **Metallic:** `0.85`
- **Smoothness / Roughness:** Roughness `0.42`
- **Surface Wear:** Edge chipping exposing primer orange rust `#7A3618`.

### 4.2 Oxide Freight Crimson (`Container_OxideRed`)
- **Base Color:** `#6A221E` / `RGB(0.42, 0.13, 0.12)`
- **Metallic:** `0.80`
- **Smoothness / Roughness:** Roughness `0.48`
- **Surface Wear:** Deep scratches and oxidation along corrugation crests.

---

## 5. Live FBX Export Pipeline & Unity Settings

### 5.1 Export Settings (Blender)
```python
bpy.ops.export_scene.fbx(
    filepath="Assets/_Project/Art/Models/CargoContainer.fbx",
    check_existing=False,
    use_selection=True,
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL',
    bake_space_transform=True,
    object_types={'MESH'},
    mesh_smooth_type='FACE',
    add_leaf_bones=False
)
```

### 5.2 Unity Ingestion & Integration
- **Collision:** Automated BoxCollider setup configured in `DryingChamberContainers.cs` and `LevelBuilder.cs`.
- **NavMesh Obstacle:** Attached `NavMeshObstacle` components with `Carve = true` prevent AI robots from pathfinding through container barricades.
- **Tactical Cover Layer:** Configured to `LayerMask.NameToLayer("Default")` and tagged for bullet raycast collision.
