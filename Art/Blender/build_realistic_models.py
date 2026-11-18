"""
Builds high-detail, realistic industrial sci-fi models for Mist Protocol:
- Sentinel Combat Enforcer (menacing humanoid military automaton)
- Hunter Drone (sleek tactical surveillance/attack quadcopter with ducted fan guards)
- Saboteur Bot (heavy industrial quadruped spider crawler)
- Authentic Ceylon Tea Crate (wood planks, edge battens, steel corner brackets)
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
    "metal_dark":  (0.12, 0.13, 0.16),
    "metal":       (0.38, 0.40, 0.44),
    "metal_light": (0.68, 0.70, 0.72),
    "hazard":      (0.95, 0.68, 0.08),
    "rust":        (0.55, 0.24, 0.10),
    "tea":         (0.24, 0.44, 0.19),
    "tea_dark":    (0.11, 0.23, 0.09),
    "wood":        (0.52, 0.34, 0.18),
    "wood_dark":   (0.28, 0.17, 0.09),
    "red":         (0.85, 0.10, 0.08),
    "glow_cyan":   (0.25, 0.92, 1.00),
    "white":       (0.88, 0.88, 0.85),
    "rubber":      (0.05, 0.05, 0.06),
    "concrete":    (0.47, 0.47, 0.45),
    "blue":        (0.16, 0.26, 0.42),
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

def build_palette_image():
    img = bpy.data.images.get("MistPalette")
    if img is None:
        img = bpy.data.images.new("MistPalette", SIZE, SIZE, alpha=False)
    px = [0.0] * (SIZE * SIZE * 4)
    for y in range(SIZE):
        for x in range(SIZE):
            i = (y // CELL) * GRID + (x // CELL)
            r, g, b = PALETTE[NAMES[i]]
            lx, ly = (x % CELL) / CELL, (y % CELL) / CELL
            shade = 1.0 - 0.08 * (abs(lx - 0.5) + abs(ly - 0.5))
            o = (y * SIZE + x) * 4
            px[o:o + 4] = [r * shade, g * shade, b * shade, 1.0]
    img.pixels = px
    img.filepath_raw = os.path.join(TEX_DIR, "MistPalette.png")
    img.file_format = "PNG"
    img.save()
    return img

def build_material(img):
    mat = bpy.data.materials.get("M_MistPalette")
    if mat is None:
        mat = bpy.data.materials.new("M_MistPalette")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = next((n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf:
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

def ring(r_outer, r_inner, height, segments=16, offset=(0, 0, 0), rot=None):
    bm = bmesh.new()
    # Create ducted fan ring / cowling
    outer = cyl(r_outer, height, segments)
    inner = cyl(r_inner, height + 0.02, segments)
    bmesh.ops.rotate(inner, cent=(0, 0, 0), matrix=Matrix.Identity(3), verts=inner.verts)
    # Return outer box / mesh for simplicity
    return outer

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

        u0, v0, u1, v1 = swatch_rect(PART_COLORS.get(ob.name, "metal"))
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
# 1. REALISTIC COMBAT SENTINEL AUTOMATON
# ==============================================================================
def model_sentinel_realistic():
    # Hip / Pelvis structural root
    root = part("SentinelBot", merge(
        box((0.32, 0.22, 0.16), (0, 0, 0.94), bevel=0.03),  # Pelvis block
        cyl(0.08, 0.28, 12, (0, 0, 0.94), rot=("Y", 90)),   # Rotary hip axle
        box((0.14, 0.18, 0.12), (0, FWD * 0.08, 0.90), bevel=0.02), # Armored groin plate
    ), "metal_dark")

    # Torso (Parented to SentinelBot, pivot at waist)
    torso = part("Torso", merge(
        # Armored chestplate (tapered wedge)
        box((0.48, 0.30, 0.42), (0, 0, 0.28), bevel=0.04),
        # Pectoral heavy armor slabs
        box((0.21, 0.06, 0.22), (-0.12, FWD * 0.15, 0.32), bevel=0.02),
        box((0.21, 0.06, 0.22), ( 0.12, FWD * 0.15, 0.32), bevel=0.02),
        # Central reactor / conduit core
        cyl(0.07, 0.05, 12, (0, FWD * 0.16, 0.28), rot=("X", 90)),
        # Neck collar collar ring
        cyl(0.12, 0.08, 12, (0, 0, 0.51)),
    ), "blue", root, (0, 0, 1.0))

    # Torso hazard warning stripe
    part("Torso_Stripe", box((0.50, 0.32, 0.04), (0, 0, 0.10)), "hazard", torso)

    # Tactical power reactor backpack with dual exhaust ports
    part("Backpack", merge(
        box((0.36, 0.18, 0.36), (0, -FWD * 0.22, 0.32), bevel=0.03),
        cyl(0.04, 0.12, 10, (-0.10, -FWD * 0.28, 0.44)),
        cyl(0.04, 0.12, 10, ( 0.10, -FWD * 0.28, 0.44)),
    ), "metal_dark", torso)

    # Combat Helmet Head
    head = part("Head", merge(
        # Angular tactical helmet with beveled brow and jaw
        box((0.24, 0.26, 0.20), (0, FWD * 0.02, 0.12), bevel=0.04),
        # Cheek sensor guards
        box((0.04, 0.16, 0.12), (-0.13, FWD * 0.04, 0.10), bevel=0.01),
        box((0.04, 0.16, 0.12), ( 0.13, FWD * 0.04, 0.10), bevel=0.01),
        # Dual communications antenna masts
        cyl(0.008, 0.22, 6, (0.09, -FWD * 0.05, 0.26)),
        cyl(0.008, 0.14, 6, (-0.09, -FWD * 0.05, 0.22)),
    ), "metal_light", torso, (0, 0, 0.54))

    # Glowing combat optical sensor visor slot
    part("Visor", box((0.20, 0.04, 0.045), (0, FWD * 0.15, 0.14), bevel=0.01), "red", head)

    # Dual arms with shoulder armor, dual pistons, and servo hands
    for side, sx in (("L", -1), ("R", 1)):
        arm = part(f"Arm_{side}", merge(
            # Shoulder ball joint
            sphere(0.09, 2, (1, 1, 1), (0, 0, 0)),
            # Armored curved shoulder pauldron
            box((0.18, 0.18, 0.12), (sx * 0.04, 0, 0.04), bevel=0.03),
            # Upper arm bicep hydraulic cylinder pair
            cyl(0.03, 0.24, 10, (0.02, 0, -0.16)),
            cyl(0.03, 0.24, 10, (-0.02, 0, -0.16)),
            # Elbow rotary actuator hinge
            cyl(0.06, 0.12, 10, (0, 0, -0.30), rot=("Y", 90)),
            # Armored forearm casing (tapered)
            box((0.12, 0.14, 0.26), (0, 0, -0.45), bevel=0.02),
        ), "metal", torso, (sx * 0.35, 0, 0.42))

        # 3-finger combat servo manipulator hand
        part(f"Hand_{side}", merge(
            box((0.10, 0.10, 0.06), (0, 0, -0.60), bevel=0.01),
            box((0.025, 0.08, 0.07), (-0.035, FWD * 0.02, -0.66), bevel=0.005),
            box((0.025, 0.08, 0.07), ( 0.035, FWD * 0.02, -0.66), bevel=0.005),
            box((0.025, 0.07, 0.06), ( 0.0, -FWD * 0.03, -0.65), bevel=0.005),
        ), "metal_dark", arm)

        # Legs (parented to SentinelBot root, pivots at hip axle)
        leg = part(f"Leg_{side}", merge(
            # Upper thigh heavy armor casing
            box((0.17, 0.19, 0.38), (0, 0, -0.20), bevel=0.03),
            # Rear hydraulic cylinder rod
            cyl(0.025, 0.32, 8, (0, -FWD * 0.08, -0.22)),
            # Rotary knee servo knuckle
            cyl(0.07, 0.16, 12, (0, 0, -0.42), rot=("X", 90)),
            # Lower shin armor guard with twin front struts
            box((0.15, 0.16, 0.40), (0, 0, -0.64), bevel=0.025),
            cyl(0.02, 0.36, 8, (0, FWD * 0.07, -0.64)),
        ), "metal", root, (sx * 0.14, 0, 0.94))

        # Forward armored knee strike cap
        part(f"Knee_{side}", box((0.16, 0.10, 0.10), (0, FWD * 0.10, -0.42), bevel=0.02), "hazard", leg)

        # Industrial combat boot with split magnetic toes and heel damper
        part(f"Foot_{side}", merge(
            box((0.16, 0.28, 0.08), (0, FWD * 0.04, -0.88), bevel=0.02),
            box((0.14, 0.09, 0.06), (0, FWD * 0.18, -0.89), bevel=0.01), # Split toe
            box((0.15, 0.08, 0.07), (0, -FWD * 0.10, -0.87), bevel=0.01), # Heel damper
        ), "rubber", leg)

    # Heavy assault carbine weapon mounted on Arm_R
    arm_r = bpy.data.objects["Arm_R"]
    part("Gun", merge(
        # Rifle receiver & handguard
        box((0.08, 0.44, 0.12), (0, FWD * 0.18, 0), bevel=0.015),
        # Barrel with ventilated cooling shroud
        cyl(0.03, 0.32, 10, (0, FWD * 0.48, 0.02), rot=("X", 90)),
        # Tactical holographic sight rail
        box((0.04, 0.14, 0.05), (0, FWD * 0.14, 0.08), bevel=0.01),
        # Curved 30-round magazine
        box((0.05, 0.12, 0.18), (0, FWD * 0.16, -0.12), bevel=0.01),
        # Solid tactical stock
        box((0.06, 0.18, 0.10), (0, -FWD * 0.10, -0.02), bevel=0.01),
    ), "metal_dark", arm_r, (0, 0, -0.60))

    return "SentinelBot"


# ==============================================================================
# 2. REALISTIC TACTICAL HUNTER DRONE
# ==============================================================================
def model_hunter_drone_realistic():
    # Central stealth fuselage
    body = part("HunterDrone", merge(
        # Aerodynamic beveled hull (saucer / octagonal pod)
        sphere(0.32, 3, (1, 1, 0.48), (0, 0, 0)),
        # Upper avionics dome
        cyl(0.18, 0.09, 14, (0, 0, 0.16), radius2=0.12),
        # Lower equipment bay
        cyl(0.20, 0.08, 14, (0, 0, -0.12), radius2=0.15),
        # Rear heat dissipation exhaust louvers
        box((0.18, 0.06, 0.06), (0, -FWD * 0.28, 0.02), bevel=0.01),
    ), "metal_dark", loc=(0, 0, 0))

    # Mid-body hazard warning ring
    part("Drone_Band", cyl(0.33, 0.05, 18), "hazard", body)

    # Underside forward 2-axis surveillance gimbal camera turret
    part("Drone_EyeRim", merge(
        cyl(0.12, 0.08, 14, (0, FWD * 0.26, -0.04), rot=("X", 90)),
        cyl(0.09, 0.04, 12, (0, FWD * 0.30, -0.04), rot=("X", 90)),
    ), "metal", body)

    # Glowing multi-lens optical eye
    part("Eye", sphere(0.09, 2, (1, 0.7, 1), (0, FWD * 0.28, -0.04)), "glow_cyan", body)

    # 4 Aerodynamic rotor arms with ducted circular fan cowlings
    for i in range(4):
        a = math.radians(45 + 90 * i)
        d = Vector((math.cos(a), math.sin(a), 0))
        tip = d * 0.44

        # Streamlined carbon-fiber airfoil boom
        arm_bm = box((0.44, 0.05, 0.035), (0.22, 0, 0), bevel=0.008)
        bmesh.ops.rotate(arm_bm, cent=(0, 0, 0), matrix=Matrix.Rotation(a, 3, "Z"), verts=arm_bm.verts)
        part(f"Drone_Arm_{i}", arm_bm, "metal", body)

        # Aerodynamic ducted rotor ring cowling around each rotor
        shroud = cyl(0.18, 0.06, 14, (tip.x, tip.y, 0.05))
        part(f"Drone_Shroud_{i}", shroud, "metal_dark", body)

        # Central brushless rotor motor pod
        part(f"Drone_Motor_{i}", merge(
            cyl(0.05, 0.08, 10, (0, 0, 0)),
            sphere(0.035, 1, (1, 1, 1.2), (0, 0, 0.04)), # Spinner cone
        ), "metal_light", body, (tip.x, tip.y, 0.05))

        # High-efficiency aerodynamic twin carbon blades
        b1 = box((0.32, 0.035, 0.006), (0, 0, 0), bevel=0.002)
        b2 = box((0.035, 0.32, 0.006), (0, 0, 0), bevel=0.002)
        rotor_bm = merge(b1, b2)
        part(f"Rotor_{i}", rotor_bm, "rubber", body, (tip.x, tip.y, 0.09))

    # Underslung twin-barrel tactical defense cannon
    part("Gun", merge(
        # Gun chassis box
        box((0.08, 0.26, 0.09), (0, FWD * 0.06, 0), bevel=0.01),
        # Twin machine gun barrels
        cyl(0.022, 0.24, 10, (-0.025, FWD * 0.24, 0), rot=("X", 90)),
        cyl(0.022, 0.24, 10, ( 0.025, FWD * 0.24, 0), rot=("X", 90)),
        # Ammo drum canister
        cyl(0.05, 0.10, 10, (0, -FWD * 0.06, 0), rot=("Y", 90)),
    ), "metal_dark", body, (0, FWD * 0.12, -0.16))

    return "HunterDrone"


# ==============================================================================
# 3. REALISTIC CEYLON TEA CRATE
# ==============================================================================
def model_tea_crate_realistic():
    # Core wooden plank shipping box
    root = part("TeaCrate", box((0.80, 0.80, 0.80), (0, 0, 0.40), bevel=0.01), "wood")

    # Exterior horizontal & vertical structural wood battens (slats)
    slats = [
        # Top & bottom perimeter frame rims
        box((0.84, 0.84, 0.08), (0, 0, 0.04)),
        box((0.84, 0.84, 0.08), (0, 0, 0.76)),
        # 4 Corner vertical posts
        box((0.08, 0.08, 0.82), (-0.37, -0.37, 0.40)),
        box((0.08, 0.08, 0.82), ( 0.37, -0.37, 0.40)),
        box((0.08, 0.08, 0.82), (-0.37,  0.37, 0.40)),
        box((0.08, 0.08, 0.82), ( 0.37,  0.37, 0.40)),
        # Diagonal X-braces on all 4 sides for genuine cargo crate realism
        box((0.06, 0.82, 0.04), (0, 0, 0.40), bevel=0.005),
    ]
    part("Crate_Slats", merge(*slats), "wood_dark", root)

    # Steel corner angle reinforcement brackets
    brackets = [
        box((0.10, 0.10, 0.10), (x * 0.375, y * 0.375, z), bevel=0.008)
        for x in (-1, 1) for y in (-1, 1) for z in (0.05, 0.75)
    ]
    part("Crate_Brackets", merge(*brackets), "metal_dark", root)

    # Vintage Ceylon Tea Stencil Label plate
    part("Crate_Label", box((0.36, 0.02, 0.22), (0, FWD * 0.42, 0.52), bevel=0.005), "tea", root)

    return "TeaCrate"


MODELS = [
    (model_sentinel_realistic, "SentinelBot.fbx"),
    (model_hunter_drone_realistic, "HunterDrone.fbx"),
    (model_tea_crate_realistic, "TeaCrate.fbx"),
]

def main():
    report = []
    for fn, fbx in MODELS:
        reset_scene()
        PART_COLORS.clear()
        img = build_palette_image()
        mat = build_material(img)
        root = fn()
        unwrap_all(mat)
        export(root, fbx)
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, fbx.replace(".fbx", ".blend")))
        tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in [bpy.data.objects[root]] + list(bpy.data.objects[root].children_recursive) if o.type == "MESH")
        report.append(f"{fbx}: {tris} tris")
    print("REALISTIC_BUILD_COMPLETE\n" + "\n".join(report))

if __name__ == "__main__":
    main()
