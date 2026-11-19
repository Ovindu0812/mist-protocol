"""
Mist Protocol - Titan Boss Mech Builder (Core Boss Automaton).
Builds the giant, terrifying industrial juggernaut 'Titan' in Blender.
- 3.2m tall colossal bipedal walking tank.
- Reinforced blast-proof chest chassis with central glowing reactor core.
- Massive hunched silhouette with dual rear steam exhaust chimneys.
- Sunken tactical armored head with glowing crimson cyclops Searchlight.
- Right Arm: Quad-barrel heavy rotary autocannon with ammo drum.
- Left Arm: Hydraulic industrial wrecking claw / crushing pincer.
- Legs: Heavy shock-absorber dual-piston legs with magnetic stomper boots.
- Fully compatible with WalkerAnimator (Leg_L, Leg_R, Arm_L, Arm_R, Torso, Head, Gun).
"""
import bpy
import bmesh
import math
import os
from mathutils import Vector, Matrix

ROOT = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(ROOT, "..", ".."))
FBX_DIR = os.path.join(PROJECT, "Assets", "_Project", "Art", "Models")
TEX_DIR = os.path.join(PROJECT, "Assets", "_Project", "Art", "Textures")
os.makedirs(FBX_DIR, exist_ok=True)
os.makedirs(TEX_DIR, exist_ok=True)

GRID = 4
SIZE = 256
CELL = SIZE // GRID
MARGIN = 6 / SIZE

PALETTE = {
    "metal_dark":  (0.10, 0.11, 0.14),
    "metal":       (0.35, 0.38, 0.42),
    "metal_light": (0.65, 0.67, 0.70),
    "hazard":      (0.95, 0.68, 0.08),
    "rust":        (0.55, 0.22, 0.08),
    "tea":         (0.24, 0.44, 0.19),
    "tea_dark":    (0.11, 0.23, 0.09),
    "wood":        (0.52, 0.34, 0.18),
    "wood_dark":   (0.28, 0.17, 0.09),
    "red":         (0.92, 0.08, 0.05),
    "glow_cyan":   (0.25, 0.92, 1.00),
    "white":       (0.88, 0.88, 0.85),
    "rubber":      (0.04, 0.04, 0.05),
    "concrete":    (0.47, 0.47, 0.45),
    "blue":        (0.14, 0.22, 0.36),
    "brass":       (0.72, 0.56, 0.24),
}
NAMES = list(PALETTE.keys())

def swatch_rect(color):
    if color not in NAMES:
        color = "metal"
    i = NAMES.index(color)
    cx, cy = i % GRID, i // GRID
    u0, v0 = cx / GRID, cy / GRID
    return u0 + MARGIN, v0 + MARGIN, u0 + 1 / GRID - MARGIN, v0 + 1 / GRID - MARGIN

def build_material():
    img = bpy.data.images.get("MistPalette")
    if img is None:
        tex_path = os.path.join(TEX_DIR, "MistPalette.png")
        if os.path.exists(tex_path):
            img = bpy.data.images.load(tex_path)
    mat = bpy.data.materials.get("M_MistPalette")
    if mat is None:
        mat = bpy.data.materials.new("M_MistPalette")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = next((n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf and img:
        tex = next((n for n in nt.nodes if n.type == "TEX_IMAGE"), None)
        if not tex:
            tex = nt.nodes.new("ShaderNodeTexImage")
            nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        tex.image = img
        tex.interpolation = "Closest"
        bsdf.inputs["Roughness"].default_value = 0.55
    return mat

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def new_obj(name, bm, parent=None, loc=(0, 0, 0)):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    if parent is not None:
        ob.parent = parent
    ob.location = loc
    return ob

def box(size, offset=(0, 0, 0), bevel=0.0):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(size), verts=bm.verts)
    if bevel > 0:
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=bevel, segments=2, affect="EDGES")
    bmesh.ops.translate(bm, vec=Vector(offset), verts=bm.verts)
    return bm

def cyl(radius, depth, segments=16, offset=(0, 0, 0), rot=None, radius2=None):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=segments,
                          radius1=radius, radius2=radius if radius2 is None else radius2, depth=depth)
    if rot is not None:
        bmesh.ops.rotate(bm, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(rot[1]), 3, rot[0]), verts=bm.verts)
    bmesh.ops.translate(bm, vec=Vector(offset), verts=bm.verts)
    return bm

def sphere(radius, subdiv=2, scale=(1, 1, 1), offset=(0, 0, 0)):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=radius)
    bmesh.ops.scale(bm, vec=Vector(scale), verts=bm.verts)
    bmesh.ops.translate(bm, vec=Vector(offset), verts=bm.verts)
    return bm

def merge(*bms):
    out = bmesh.new()
    for b in bms:
        me = bpy.data.meshes.new("_tmp")
        b.to_mesh(me)
        b.free()
        out.from_mesh(me)
        bpy.data.meshes.remove(me)
    return out

PART_COLORS = {}

def part(name, bm, color, parent=None, loc=(0, 0, 0)):
    ob = new_obj(name, bm, parent, loc)
    PART_COLORS[ob.name] = color
    return ob

def unwrap_all(mat):
    for ob in [o for o in bpy.context.scene.objects if o.type == "MESH"]:
        bpy.ops.object.select_all(action="DESELECT")
        ob.select_set(True)
        bpy.context.view_layer.objects.active = ob
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02)
        bpy.ops.object.mode_set(mode="OBJECT")

        u0, v0, u1, v1 = swatch_rect(PART_COLORS.get(ob.name, "metal_dark"))
        uv = ob.data.uv_layers.active.data
        for d in uv:
            d.uv = (u0 + d.uv[0] * (u1 - u0), v0 + d.uv[1] * (v1 - v0))
        ob.data.materials.clear()
        ob.data.materials.append(mat)
        for p in ob.data.polygons:
            p.use_smooth = False

def export(root_name, filename):
    bpy.ops.object.select_all(action="DESELECT")
    root = bpy.data.objects[root_name]
    root.select_set(True)
    for c in root.children_recursive:
        c.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(FBX_DIR, filename),
        use_selection=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y",
        bake_space_transform=False,
        object_types={"MESH", "EMPTY"},
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="STRIP",
    )

FWD = 1

# ==============================================================================
# TITAN BOSS MECH BUILDER
# ==============================================================================
def build_titan():
    # -------------------------------------------------------------------------
    # ROOT: Pelvis & Hip Chassis (at Z=1.35m)
    # -------------------------------------------------------------------------
    root = part("TitanBoss", merge(
        # Heavy cast-steel central pelvis chassis block
        box((0.72, 0.54, 0.36), (0, 0, 1.35), bevel=0.06),
        # Lateral heavy rotary hip actuator drums (massive cylinders)
        cyl(0.22, 0.88, 16, (0, 0, 1.35), rot=("Y", 90)),
        # Front armored groin blast wedge
        box((0.36, 0.32, 0.30), (0, FWD * 0.22, 1.25), bevel=0.04),
        # Rear hydraulic stabilizer housing
        box((0.44, 0.24, 0.28), (0, -FWD * 0.22, 1.30), bevel=0.03),
    ), "metal_dark")

    # Pelvis hazard warning trim
    part("Pelvis_Hazard", box((0.74, 0.56, 0.06), (0, 0, 1.22)), "hazard", root)

    # -------------------------------------------------------------------------
    # TORSO: Hunched Heavy Juggernaut Blast Chassis (Parented to root, pivot at Z=1.45m)
    # -------------------------------------------------------------------------
    torso = part("Torso", merge(
        # Main reinforced blast chassis (sloped forward)
        box((1.15, 0.82, 0.95), (0, FWD * 0.08, 0.52), bevel=0.08),
        # Pectoral heavy armored blast slabs (left & right)
        box((0.48, 0.16, 0.52), (-0.28, FWD * 0.44, 0.58), bevel=0.04),
        box((0.48, 0.16, 0.52), ( 0.28, FWD * 0.44, 0.58), bevel=0.04),
        # Upper armored neck cowl ring (collar shielding the head)
        cyl(0.38, 0.22, 16, (0, FWD * 0.12, 1.05)),
        # Massive shoulder pauldron mounts (bulkheads)
        box((0.28, 0.48, 0.38), (-0.68, FWD * 0.02, 0.72), bevel=0.04),
        box((0.28, 0.48, 0.38), ( 0.68, FWD * 0.02, 0.72), bevel=0.04),
    ), "blue", root, (0, 0, 1.45))

    # Chest Hazard Stripes
    part("Torso_Hazard", merge(
        box((0.42, 0.04, 0.14), (-0.28, FWD * 0.53, 0.72)),
        box((0.42, 0.04, 0.14), ( 0.28, FWD * 0.53, 0.72)),
    ), "hazard", torso)

    # Central glowing coolant / power reactor core (weak spot)
    part("Core_Glow", merge(
        cyl(0.16, 0.08, 16, (0, FWD * 0.46, 0.48), rot=("X", 90)),
        cyl(0.12, 0.10, 16, (0, FWD * 0.48, 0.48), rot=("X", 90)),
    ), "glow_cyan", torso)

    # Rear Steam Boiler Exhaust Chimneys (twin smoke stacks on upper back)
    part("Exhaust_Stacks", merge(
        # Left steam chimney with ventilation cap
        cyl(0.11, 0.65, 14, (-0.32, -FWD * 0.38, 1.05), rot=("X", -12)),
        cyl(0.14, 0.12, 14, (-0.32, -FWD * 0.45, 1.35), rot=("X", -12)),
        # Right steam chimney with ventilation cap
        cyl(0.11, 0.65, 14, ( 0.32, -FWD * 0.38, 1.05), rot=("X", -12)),
        cyl(0.14, 0.12, 14, ( 0.32, -FWD * 0.45, 1.35), rot=("X", -12)),
        # Heavy boiler radiator block
        box((0.84, 0.32, 0.62), (0, -FWD * 0.32, 0.56), bevel=0.04),
    ), "rust", torso)

    # -------------------------------------------------------------------------
    # HEAD: Sunken Armored Cockpit / Sensor Turret (Parented to Torso at Z=0.98m)
    # -------------------------------------------------------------------------
    head = part("Head", merge(
        # Heavy faceted blast helmet sunk deep into the torso cowl
        box((0.44, 0.42, 0.34), (0, FWD * 0.14, 0.15), bevel=0.05),
        # Brow heavy strike plate
        box((0.46, 0.18, 0.12), (0, FWD * 0.28, 0.26), bevel=0.02),
        # Chin reinforced air intake filter grille
        box((0.28, 0.14, 0.12), (0, FWD * 0.28, 0.02), bevel=0.02),
        # Dual acoustic detection horns on helmet sides
        cyl(0.04, 0.14, 8, (-0.24, FWD * 0.10, 0.22), rot=("Y", -45)),
        cyl(0.04, 0.14, 8, ( 0.24, FWD * 0.10, 0.22), rot=("Y", 45)),
    ), "metal_dark", torso, (0, 0, 0.98))

    # Giant Cyclops Crimson Searchlight Lens (The Titan's eye)
    part("Searchlight", merge(
        # Cylindrical searchlight housing bezel
        cyl(0.15, 0.09, 16, (0, FWD * 0.33, 0.16), rot=("X", 90)),
        # Convex glowing red lens
        sphere(0.13, 2, (1, 0.6, 1), (0, FWD * 0.36, 0.16)),
    ), "red", head)

    # Secondary tactical scanning visor strip
    part("Visor", box((0.36, 0.05, 0.04), (0, FWD * 0.33, 0.22)), "red", head)

    # -------------------------------------------------------------------------
    # ARM RIGHT (Arm_R): The Devastating Autocannon System
    # -------------------------------------------------------------------------
    arm_r = part("Arm_R", merge(
        # Shoulder heavy ball joint
        sphere(0.20, 2, (1, 1, 1), (0, 0, 0)),
        # Massive reinforced curved shoulder pauldron
        box((0.38, 0.42, 0.26), (0.12, 0, 0.08), bevel=0.05),
        # Upper arm twin hydraulic ram shafts
        cyl(0.065, 0.52, 12, ( 0.06, 0, -0.28)),
        cyl(0.065, 0.52, 12, (-0.06, 0, -0.28)),
        # Elbow heavy rotary actuator
        cyl(0.16, 0.28, 14, (0, 0, -0.55), rot=("Y", 90)),
        # Armored forearm housing
        box((0.32, 0.38, 0.48), (0, FWD * 0.06, -0.80), bevel=0.04),
    ), "metal", torso, (0.82, 0, 0.70))

    # Right Hand Autocannon (Gun): 4-barrel heavy rotary gatling
    gun = part("Gun", merge(
        # Cannon receiver & housing block
        box((0.26, 0.78, 0.30), (0, FWD * 0.36, 0), bevel=0.03),
        # Large cylindrical ammo drum on top of receiver
        cyl(0.20, 0.26, 16, (0, FWD * 0.28, 0.22), rot=("Y", 90)),
        # Quad-barrel rotary assembly (4 heavy steel barrels)
        cyl(0.042, 0.72, 12, (-0.06, FWD * 0.90, -0.06), rot=("X", 90)),
        cyl(0.042, 0.72, 12, ( 0.06, FWD * 0.90, -0.06), rot=("X", 90)),
        cyl(0.042, 0.72, 12, (-0.06, FWD * 0.90,  0.06), rot=("X", 90)),
        cyl(0.042, 0.72, 12, ( 0.06, FWD * 0.90,  0.06), rot=("X", 90)),
        # Barrel cluster stabilization rings
        cyl(0.14, 0.05, 16, (0, FWD * 0.75, 0), rot=("X", 90)),
        cyl(0.14, 0.05, 16, (0, FWD * 1.15, 0), rot=("X", 90)),
        # Heavy muzzle flash brake
        cyl(0.15, 0.08, 16, (0, FWD * 1.25, 0), rot=("X", 90)),
    ), "metal_dark", arm_r, (0, 0, -1.02))

    # Autocannon Muzzle marker
    part("Muzzle", cyl(0.05, 0.02, 10, (0, FWD * 1.30, 0), rot=("X", 90)), "brass", gun)

    # -------------------------------------------------------------------------
    # ARM LEFT (Arm_L): The Hydraulic Wrecking Claw / Crushing Pincer
    # -------------------------------------------------------------------------
    arm_l = part("Arm_L", merge(
        # Shoulder heavy ball joint
        sphere(0.20, 2, (1, 1, 1), (0, 0, 0)),
        # Massive reinforced curved shoulder pauldron
        box((0.38, 0.42, 0.26), (-0.12, 0, 0.08), bevel=0.05),
        # Upper arm twin hydraulic ram shafts
        cyl(0.065, 0.52, 12, (-0.06, 0, -0.28)),
        cyl(0.065, 0.52, 12, ( 0.06, 0, -0.28)),
        # Elbow heavy rotary actuator
        cyl(0.16, 0.28, 14, (0, 0, -0.55), rot=("Y", 90)),
        # Armored forearm housing
        box((0.34, 0.38, 0.52), (0, FWD * 0.06, -0.80), bevel=0.04),
    ), "metal", torso, (-0.82, 0, 0.70))

    # Hand_L: The Industrial Hydraulic Crushing Claw
    part("Hand_L", merge(
        # Heavy wrist gimbal block
        box((0.28, 0.28, 0.22), (0, 0, -1.08), bevel=0.03),
        # 3 Serrated steel hydraulic pincer claws
        # Claw finger 1 (Left pincer jaw)
        box((0.08, 0.38, 0.12), (-0.14, FWD * 0.18, -1.22), bevel=0.02),
        box((0.07, 0.14, 0.09), (-0.10, FWD * 0.34, -1.28), bevel=0.01),
        # Claw finger 2 (Right pincer jaw)
        box((0.08, 0.38, 0.12), ( 0.14, FWD * 0.18, -1.22), bevel=0.02),
        box((0.07, 0.14, 0.09), ( 0.10, FWD * 0.34, -1.28), bevel=0.01),
        # Claw finger 3 (Opposing lower thumb claw)
        box((0.14, 0.34, 0.09), ( 0.0, FWD * 0.16, -1.36), bevel=0.02),
    ), "rust", arm_l)

    # Claw Hazard Stripes
    part("Claw_Hazard", merge(
        box((0.09, 0.22, 0.13), (-0.14, FWD * 0.16, -1.20)),
        box((0.09, 0.22, 0.13), ( 0.14, FWD * 0.16, -1.20)),
    ), "hazard", arm_l)

    # -------------------------------------------------------------------------
    # LEGS (Leg_L & Leg_R): Heavy Stomper Bipedal Walking Mechanism
    # -------------------------------------------------------------------------
    for side, sx in (("L", -1), ("R", 1)):
        leg = part(f"Leg_{side}", merge(
            # Upper thigh heavy armored casing
            box((0.34, 0.38, 0.58), (0, 0, -0.32), bevel=0.05),
            # Rear twin exposed hydraulic shock absorber piston shafts
            cyl(0.045, 0.48, 10, (-0.08, -FWD * 0.16, -0.32)),
            cyl(0.045, 0.48, 10, ( 0.08, -FWD * 0.16, -0.32)),
            # Knee rotary actuator knuckle
            cyl(0.15, 0.34, 14, (0, 0, -0.62), rot=("X", 90)),
            # Heavy shin armor column
            box((0.30, 0.34, 0.62), (0, FWD * 0.02, -0.96), bevel=0.04),
            # Dual forward shin reinforcement strut bars
            cyl(0.035, 0.58, 8, (-0.09, FWD * 0.17, -0.96)),
            cyl(0.035, 0.58, 8, ( 0.09, FWD * 0.17, -0.96)),
        ), "metal", root, (sx * 0.36, 0, 1.35))

        # Forward armored knee ram strike plate
        part(f"Knee_{side}", merge(
            box((0.32, 0.16, 0.18), (0, FWD * 0.22, -0.62), bevel=0.03),
            box((0.26, 0.10, 0.10), (0, FWD * 0.28, -0.62), bevel=0.02),
        ), "hazard", leg)

        # Colossal magnetic stomper boot with floor dampening spurs
        part(f"Foot_{side}", merge(
            # Main boot sole block
            box((0.38, 0.58, 0.16), (0, FWD * 0.06, -1.28), bevel=0.03),
            # Forward magnetic toe claw
            box((0.34, 0.18, 0.12), (0, FWD * 0.32, -1.30), bevel=0.02),
            # Rear stabilizing heel spur
            box((0.32, 0.20, 0.14), (0, -FWD * 0.22, -1.28), bevel=0.02),
            # Side hydraulic ankle dampers
            cyl(0.05, 0.18, 8, ( 0.18, 0, -1.20), rot=("Y", 90)),
            cyl(0.05, 0.18, 8, (-0.18, 0, -1.20), rot=("Y", 90)),
        ), "rubber", leg)

    return "TitanBoss"


def main():
    reset_scene()
    PART_COLORS.clear()
    mat = build_material()
    root = build_titan()
    unwrap_all(mat)
    export(root, "TitanBoss.fbx")
    blend_path = os.path.join(ROOT, "TitanBoss.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) 
               for o in [bpy.data.objects[root]] + list(bpy.data.objects[root].children_recursive) 
               if o.type == "MESH")
    print(f"TITAN_BUILD_SUCCESS: {tris} tris saved to {blend_path} and TitanBoss.fbx")

if __name__ == "__main__":
    main()
