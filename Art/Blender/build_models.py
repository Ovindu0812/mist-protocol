"""
Mist Protocol - custom model builder (Core Developer role).

Builds every custom low-poly model for the game from scratch in Blender,
UV-unwraps it onto a shared 256x256 colour-palette atlas and exports FBX
files straight into the Unity project.

Run headless:
    Blender -b -P Art/Blender/build_models.py

Design notes (for the viva):
  * One shared palette texture + one material for every model -> Unity can
    batch draw calls (SRP Batcher / GPU instancing) and the art style stays
    cohesive.
  * Every part is UV unwrapped with Smart UV Project, then each island set is
    packed inside the palette swatch of its colour, so texture bleeding at
    mip levels is avoided by a 4px swatch margin.
  * Moving parts (rotors, legs, arms, head, claw) are separate objects whose
    origin sits on the joint, so the Agent Controller can animate them
    procedurally in Unity without a skinned mesh (cheaper on the CPU).
  * Poly budgets: drone < 600 tris, robots < 1200 tris, props < 400 tris.
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

# ---------------------------------------------------------------- palette
GRID = 4                      # 4x4 swatches
SIZE = 256
CELL = SIZE // GRID
MARGIN = 6 / SIZE             # UV margin inside a swatch

PALETTE = {
    "metal_dark":  (0.10, 0.11, 0.13),
    "metal":       (0.33, 0.35, 0.38),
    "metal_light": (0.62, 0.64, 0.66),
    "hazard":      (0.95, 0.68, 0.08),
    "rust":        (0.62, 0.27, 0.10),
    "tea":         (0.24, 0.44, 0.19),
    "tea_dark":    (0.11, 0.23, 0.09),
    "wood":        (0.52, 0.34, 0.18),
    "wood_dark":   (0.30, 0.18, 0.09),
    "red":         (0.78, 0.09, 0.07),
    "glow_cyan":   (0.25, 0.92, 1.00),
    "white":       (0.88, 0.88, 0.85),
    "rubber":      (0.04, 0.04, 0.045),
    "concrete":    (0.47, 0.47, 0.45),
    "blue":        (0.14, 0.28, 0.52),
    "brass":       (0.72, 0.56, 0.24),
}
NAMES = list(PALETTE.keys())


def swatch_rect(color):
    i = NAMES.index(color)
    cx, cy = i % GRID, i // GRID
    u0, v0 = cx / GRID, cy / GRID
    return u0 + MARGIN, v0 + MARGIN, u0 + 1 / GRID - MARGIN, v0 + 1 / GRID - MARGIN


def build_palette_image():
    img = bpy.data.images.new("MistPalette", SIZE, SIZE, alpha=False)
    px = [0.0] * (SIZE * SIZE * 4)
    for y in range(SIZE):
        for x in range(SIZE):
            i = (y // CELL) * GRID + (x // CELL)
            r, g, b = PALETTE[NAMES[i]]
            # subtle edge darkening inside each swatch gives a hand-painted feel
            lx, ly = (x % CELL) / CELL, (y % CELL) / CELL
            shade = 1.0 - 0.08 * (abs(lx - 0.5) + abs(ly - 0.5))
            o = (y * SIZE + x) * 4
            px[o:o + 4] = [r * shade, g * shade, b * shade, 1.0]
    img.pixels = px
    img.filepath_raw = os.path.join(TEX_DIR, "MistPalette.png")
    img.file_format = "PNG"
    img.save()
    return img


# emissive mask: white where the swatch should glow (cyan + red)
def build_emission_image():
    img = bpy.data.images.new("MistPalette_Emission", SIZE, SIZE, alpha=False)
    px = [0.0] * (SIZE * SIZE * 4)
    glow = {"glow_cyan", "red"}
    for y in range(SIZE):
        for x in range(SIZE):
            name = NAMES[(y // CELL) * GRID + (x // CELL)]
            c = PALETTE[name] if name in glow else (0, 0, 0)
            o = (y * SIZE + x) * 4
            px[o:o + 4] = [c[0], c[1], c[2], 1.0]
    img.pixels = px
    img.filepath_raw = os.path.join(TEX_DIR, "MistPalette_Emission.png")
    img.file_format = "PNG"
    img.save()


def build_material(img):
    mat = bpy.data.materials.new("M_MistPalette")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    tex.interpolation = "Closest"
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.6
    return mat


# ---------------------------------------------------------------- helpers
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
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=bevel, segments=1, affect="EDGES")
    bmesh.ops.translate(bm, vec=Vector(offset), verts=bm.verts)
    return bm


def cyl(radius, depth, segments=12, offset=(0, 0, 0), rot=None, radius2=None):
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
    """Smart-UV-project each mesh, then pack its islands into its palette swatch."""
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


def tri_count(root_name):
    root = bpy.data.objects[root_name]
    objs = [root] + list(root.children_recursive)
    return sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in objs if o.type == "MESH")


# Models face Blender +Y: with Unity's bakeAxisConversion, Blender +Y becomes Unity +Z (forward).
FWD = 1

# ---------------------------------------------------------------- models
def model_hunter_drone():
    body = part("HunterDrone", merge(
        sphere(0.30, 2, (1, 1, 0.55), (0, 0, 0)),
        cyl(0.18, 0.08, 10, (0, 0, 0.17)),
    ), "metal_dark", loc=(0, 0, 0))
    part("Drone_Band", cyl(0.315, 0.07, 16), "hazard", body)
    part("Eye", sphere(0.085, 1, (1, 0.6, 1), (0, FWD * 0.27, 0.0)), "glow_cyan", body)
    part("Drone_EyeRim", cyl(0.11, 0.06, 12, (0, FWD * 0.25, 0), rot=("X", 90)), "metal", body)
    for i in range(4):
        a = math.radians(45 + 90 * i)
        d = Vector((math.cos(a), math.sin(a), 0))
        arm = box((0.42, 0.06, 0.04), (0.2, 0, 0))
        bmesh.ops.rotate(arm, cent=(0, 0, 0), matrix=Matrix.Rotation(a, 3, "Z"), verts=arm.verts)
        part(f"Drone_Arm_{i}", arm, "metal", body)
        tip = d * 0.42
        part(f"Drone_Motor_{i}", cyl(0.045, 0.07, 8), "metal_light", body, (tip.x, tip.y, 0.03))
        rotor = merge(box((0.30, 0.035, 0.008)), box((0.035, 0.30, 0.008)))
        part(f"Rotor_{i}", rotor, "rubber", body, (tip.x, tip.y, 0.075))
    part("Gun", cyl(0.025, 0.22, 8, (0, FWD * 0.11, 0), rot=("X", 90)), "rubber", body, (0, FWD * 0.12, -0.15))
    return "HunterDrone"


def model_sentinel():
    root = part("SentinelBot", box((0.36, 0.24, 0.16), (0, 0, 0.95), 0.02), "metal")
    part("Torso", merge(
        box((0.52, 0.32, 0.46), (0, 0, 0.28), 0.03),
        box((0.30, 0.06, 0.20), (0, FWD * 0.18, 0.32), 0.01),
    ), "blue", root, (0, 0, 1.0))
    torso = bpy.data.objects["Torso"]
    part("Torso_Stripe", box((0.54, 0.34, 0.05), (0, 0, 0.10)), "hazard", torso)
    part("Backpack", box((0.36, 0.16, 0.34), (0, -FWD * 0.22, 0.30), 0.02), "metal_dark", torso)
    head = part("Head", merge(
        box((0.26, 0.26, 0.22), (0, 0, 0.12), 0.03),
        cyl(0.012, 0.18, 6, (0.09, 0.05, 0.30)),
    ), "metal_light", torso, (0, 0, 0.53))
    part("Visor", box((0.22, 0.04, 0.06), (0, FWD * 0.13, 0.13)), "red", head)
    for side, sx in (("L", -1), ("R", 1)):
        arm = part(f"Arm_{side}", merge(
            sphere(0.09, 1, (1, 1, 1)),
            box((0.12, 0.12, 0.30), (0, 0, -0.18), 0.015),
            box((0.10, 0.10, 0.26), (0, 0, -0.45), 0.015),
        ), "metal", torso, (sx * 0.34, 0, 0.42))
        part(f"Hand_{side}", box((0.12, 0.12, 0.10), (0, 0, -0.62), 0.02), "metal_dark", arm)
        leg = part(f"Leg_{side}", merge(
            box((0.15, 0.17, 0.44), (0, 0, -0.22), 0.02),
            box((0.13, 0.15, 0.42), (0, 0, -0.66), 0.02),
        ), "metal", root, (sx * 0.13, 0, 0.92))
        part(f"Knee_{side}", box((0.17, 0.19, 0.08), (0, 0, -0.44), 0.01), "hazard", leg)
        part(f"Foot_{side}", box((0.17, 0.30, 0.08), (0, FWD * 0.05, -0.88), 0.02), "rubber", leg)
    arm_r = bpy.data.objects["Arm_R"]
    part("Gun", merge(
        box((0.10, 0.50, 0.12), (0, FWD * 0.15, 0), 0.01),
        cyl(0.03, 0.25, 8, (0, FWD * 0.50, 0.02), rot=("X", 90)),
    ), "metal_dark", arm_r, (0, 0, -0.60))
    return "SentinelBot"


def model_saboteur():
    root = part("SaboteurBot", merge(
        box((0.62, 0.70, 0.26), (0, 0, 0.48), 0.04),
        box((0.40, 0.40, 0.12), (0, -FWD * 0.05, 0.66), 0.02),
    ), "rust")
    part("Sab_Stripe", box((0.64, 0.10, 0.28), (0, FWD * 0.22, 0.48)), "hazard", root)
    part("Eye", box((0.30, 0.04, 0.05), (0, FWD * 0.36, 0.52)), "glow_cyan", root)
    part("Antenna", merge(cyl(0.01, 0.40, 6, (0, 0, 0.2)), sphere(0.03, 1, offset=(0, 0, 0.4))),
         "metal_light", root, (0.18, -FWD * 0.15, 0.72))
    for name, x, y in (("FL", -1, -1), ("FR", 1, -1), ("BL", -1, 1), ("BR", 1, 1)):
        leg = part(f"Leg_{name}", merge(
            box((0.30, 0.07, 0.07), (x * 0.15, 0, 0)),
            box((0.07, 0.07, 0.44), (x * 0.30, 0, -0.20)),
        ), "metal_dark", root, (x * 0.30, y * 0.28, 0.44))
        part(f"Toe_{name}", sphere(0.05, 1, offset=(x * 0.30, 0, -0.42)), "rubber", leg)
    claw = part("Claw", merge(
        box((0.08, 0.40, 0.08), (0, FWD * 0.20, 0)),
        box((0.20, 0.06, 0.06), (0, FWD * 0.40, 0)),
    ), "metal_light", root, (0, FWD * 0.35, 0.58))
    part("Claw_FingerL", box((0.04, 0.16, 0.04), (0, FWD * 0.08, 0)), "hazard", claw, (-0.09, FWD * 0.42, 0))
    part("Claw_FingerR", box((0.04, 0.16, 0.04), (0, FWD * 0.08, 0)), "hazard", claw, (0.09, FWD * 0.42, 0))
    return "SaboteurBot"


def model_rifle():
    root = part("MistRifle", merge(
        box((0.06, 0.38, 0.10), (0, 0, 0), 0.008),
        box((0.05, 0.20, 0.07), (0, -FWD * 0.27, -0.02), 0.008),
    ), "metal_dark")
    part("Rifle_Barrel", cyl(0.018, 0.36, 8, (0, FWD * 0.36, 0.015), rot=("X", 90)), "metal", root)
    part("Rifle_Shroud", cyl(0.032, 0.16, 8, (0, FWD * 0.24, 0.015), rot=("X", 90)), "tea", root)
    part("Rifle_Mag", box((0.04, 0.07, 0.16), (0, FWD * 0.06, -0.11), 0.006), "brass", root)
    part("Rifle_Grip", box((0.04, 0.06, 0.12), (0, -FWD * 0.10, -0.10), 0.006), "rubber", root)
    part("Rifle_Sight", merge(box((0.03, 0.10, 0.04), (0, 0, 0.07)),
                              box((0.02, 0.02, 0.02), (0, FWD * 0.04, 0.10))), "metal_light", root)
    part("Rifle_Light", box((0.02, 0.01, 0.02), (0, FWD * 0.05, 0.11)), "glow_cyan", root)
    part("Muzzle", box((0.01, 0.01, 0.01), (0, 0, 0)), "metal", root, (0, FWD * 0.55, 0.015))
    return "MistRifle"


def model_shotgun():
    root = part("MistShotgun", merge(
        box((0.055, 0.32, 0.09), (0, 0, 0), 0.008),
        box((0.048, 0.22, 0.10), (0, -FWD * 0.22, -0.02), 0.01),
    ), "metal_dark")
    part("Shotgun_Barrel_L", cyl(0.019, 0.44, 8, (-0.016, FWD * 0.38, 0.02), rot=("X", 90)), "metal", root)
    part("Shotgun_Barrel_R", cyl(0.019, 0.44, 8, (0.016, FWD * 0.38, 0.02), rot=("X", 90)), "metal", root)
    part("Shotgun_Tube", cyl(0.018, 0.36, 8, (0, FWD * 0.34, -0.018), rot=("X", 90)), "metal_dark", root)
    part("Shotgun_Pump", box((0.065, 0.15, 0.065), (0, FWD * 0.28, -0.018), 0.008), "wood", root)
    part("Shotgun_Grip", box((0.045, 0.07, 0.12), (0, -FWD * 0.09, -0.10), 0.006), "rubber", root)
    part("Shotgun_Guard", box((0.062, 0.14, 0.03), (0, FWD * 0.22, 0.036)), "tea", root)
    part("Shotgun_Sight", box((0.012, 0.02, 0.02), (0, FWD * 0.58, 0.045)), "brass", root)
    part("Shotgun_Light", box((0.018, 0.02, 0.018), (0, FWD * 0.06, 0.06)), "glow_cyan", root)
    part("Muzzle", box((0.01, 0.01, 0.01), (0, 0, 0)), "metal", root, (0, FWD * 0.62, 0.02))
    return "MistShotgun"


def model_revolver():
    root = part("MistRevolver", merge(
        box((0.042, 0.14, 0.07), (0, 0, 0.01), 0.006),
        box((0.038, 0.06, 0.08), (0, -FWD * 0.08, 0.02), 0.006),
    ), "metal_dark")
    part("Revolver_Cylinder", cyl(0.034, 0.09, 6, (0, FWD * 0.02, 0.01), rot=("X", 90)), "metal_light", root)
    part("Revolver_Barrel", merge(
        cyl(0.017, 0.26, 8, (0, FWD * 0.20, 0.025), rot=("X", 90)),
        box((0.036, 0.24, 0.03), (0, FWD * 0.19, 0.04)),
        box((0.032, 0.16, 0.03), (0, FWD * 0.15, -0.01))
    ), "metal", root)
    part("Revolver_Grip", box((0.038, 0.07, 0.13), (0, -FWD * 0.06, -0.08), 0.01), "wood_dark", root)
    part("Revolver_TriggerGuard", box((0.024, 0.07, 0.04), (0, FWD * 0.01, -0.03)), "brass", root)
    part("Revolver_Sight", box((0.008, 0.02, 0.02), (0, FWD * 0.32, 0.065)), "red", root)
    part("Revolver_Light", box((0.016, 0.016, 0.016), (0, -FWD * 0.06, 0.05)), "glow_cyan", root)
    part("Muzzle", box((0.01, 0.01, 0.01), (0, 0, 0)), "metal", root, (0, FWD * 0.35, 0.025))
    return "MistRevolver"


def model_smg():
    root = part("MistSMG", merge(
        box((0.046, 0.26, 0.08), (0, 0, 0.01), 0.006),
        box((0.042, 0.18, 0.06), (0, -FWD * 0.03, -0.04), 0.006),
    ), "metal_dark")
    part("SMG_Barrel", cyl(0.015, 0.18, 8, (0, FWD * 0.22, 0.02), rot=("X", 90)), "metal", root)
    part("SMG_Shroud", cyl(0.028, 0.12, 8, (0, FWD * 0.18, 0.02), rot=("X", 90)), "metal_light", root)
    part("SMG_Mag", box((0.032, 0.05, 0.18), (0, FWD * 0.04, -0.13), 0.006), "tea_dark", root)
    part("SMG_Grip", box((0.038, 0.055, 0.11), (0, -FWD * 0.08, -0.09), 0.006), "rubber", root)
    part("SMG_Foregrip", box((0.034, 0.04, 0.09), (0, FWD * 0.12, -0.06), 0.006), "rubber", root)
    part("SMG_Sight", merge(
        box((0.03, 0.18, 0.02), (0, 0, 0.06)),
        box((0.026, 0.05, 0.035), (0, FWD * 0.02, 0.085))
    ), "metal", root)
    part("SMG_PowerCell", box((0.015, 0.08, 0.03), (0.026, -FWD * 0.02, 0.02)), "glow_cyan", root)
    part("SMG_WireStock", box((0.036, 0.14, 0.04), (0, -FWD * 0.20, 0.01), 0.004), "metal", root)
    part("Muzzle", box((0.01, 0.01, 0.01), (0, 0, 0)), "metal", root, (0, FWD * 0.33, 0.02))
    return "MistSMG"


def model_glenmist_jeep():
    """Low-poly hill-country 4x4 used by the opening arrival."""
    root = part("GlenmistJeep", merge(
        box((1.72, 3.65, 0.48), (0, 0, 0.72), 0.08),
        box((1.55, 1.55, 0.72), (0, -0.38, 1.24), 0.06),
        box((1.80, 0.22, 0.36), (0, FWD * 1.90, 0.72), 0.03),
    ), "tea_dark")
    part("Jeep_Hood", box((1.58, 1.10, 0.24), (0, FWD * 0.90, 1.06), 0.04), "tea", root)
    part("Jeep_Windscreen", box((1.40, 0.06, 0.55), (0, FWD * 0.28, 1.62)), "metal_light", root)
    part("Jeep_Roof", box((1.60, 1.48, 0.12), (0, -FWD * 0.45, 1.76), 0.04), "metal_dark", root)
    part("Jeep_Bumper", box((1.86, 0.16, 0.18), (0, FWD * 2.02, 0.49), 0.02), "metal", root)
    part("Jeep_Headlight_L", box((0.27, 0.06, 0.20), (-0.56, FWD * 2.06, 0.82)), "hazard", root)
    part("Jeep_Headlight_R", box((0.27, 0.06, 0.20), (0.56, FWD * 2.06, 0.82)), "hazard", root)
    for name, x, y in (("FL", -0.91, 1.22), ("FR", 0.91, 1.22), ("RL", -0.91, -1.25), ("RR", 0.91, -1.25)):
        wheel = part("Jeep_Wheel_" + name, cyl(0.42, 0.28, 10, (0, 0, 0), rot=("Y", 90)), "rubber", root, (x, y, 0.43))
        part("Jeep_Hub_" + name, cyl(0.19, 0.30, 10, (0, 0, 0), rot=("Y", 90)), "metal", wheel)
    return "GlenmistJeep"


def model_tea_crate():
    slats = [box((0.82, 0.82, 0.10), (0, 0, z)) for z in (0.05, 0.40, 0.75)]
    part("TeaCrate", box((0.76, 0.76, 0.80), (0, 0, 0.40)), "wood", None)
    part("Crate_Slats", merge(*slats,
                              box((0.10, 0.84, 0.84), (-0.36, 0, 0.40)),
                              box((0.10, 0.84, 0.84), (0.36, 0, 0.40))),
         "wood_dark", bpy.data.objects["TeaCrate"])
    part("Crate_Label", box((0.30, 0.02, 0.18), (0, FWD * 0.42, 0.55)), "tea", bpy.data.objects["TeaCrate"])
    return "TeaCrate"


def model_gas_canister():
    root = part("GasCanister", cyl(0.17, 0.62, 12, (0, 0, 0.31)), "red")
    part("Can_Bands", merge(cyl(0.175, 0.04, 12, (0, 0, 0.12)), cyl(0.175, 0.04, 12, (0, 0, 0.50))),
         "hazard", root)
    part("Can_Valve", merge(cyl(0.05, 0.08, 8, (0, 0, 0.66)), box((0.16, 0.03, 0.03), (0, 0, 0.71))),
         "brass", root)
    return "GasCanister"


def model_rolling_machine():
    root = part("TeaRollingMachine", merge(
        box((1.8, 1.4, 0.12), (0, 0, 0.85), 0.02),
        *[box((0.12, 0.12, 0.85), (x, y, 0.42)) for x in (-0.8, 0.8) for y in (-0.6, 0.6)],
    ), "metal_dark")
    part("Roller_Table", cyl(0.62, 0.06, 16, (0, 0, 0.94)), "brass", root)
    part("Roller_Drum", merge(cyl(0.48, 0.55, 16, (0, 0, 1.25)),
                              cyl(0.52, 0.06, 16, (0, 0, 1.53))), "tea", root, (0, 0, 0))
    part("Roller_Arm", merge(box((1.6, 0.12, 0.12), (0, 0, 1.75)),
                             box((0.12, 0.12, 0.95), (-0.8, 0, 1.30)),
                             box((0.12, 0.12, 0.95), (0.8, 0, 1.30))), "metal", root)
    part("Roller_Hazard", box((1.82, 0.04, 0.10), (0, FWD * 0.70, 0.85)), "hazard", root)
    return "TeaRollingMachine"


def model_withering_trough():
    root = part("WitheringTrough", merge(
        box((4.0, 1.4, 0.10), (0, 0, 0.80)),
        box((4.0, 0.08, 0.35), (0, -0.70, 0.95)),
        box((4.0, 0.08, 0.35), (0, 0.70, 0.95)),
        *[box((0.10, 0.10, 0.80), (x, y, 0.40)) for x in (-1.9, 0, 1.9) for y in (-0.6, 0.6)],
    ), "metal")
    part("Trough_Leaves", box((3.9, 1.3, 0.12), (0, 0, 0.90)), "tea_dark", root)
    part("Trough_Fan", cyl(0.40, 0.12, 12, (0, 0, 0), rot=("Y", 90)), "metal_dark", root, (-2.06, 0, 0.55))
    return "WitheringTrough"


def model_control_console():
    root = part("KillSwitchConsole", merge(
        box((1.4, 0.7, 1.0), (0, 0, 0.50), 0.03),
        box((1.4, 0.5, 0.10), (0, FWD * 0.20, 1.05), 0.02),
    ), "blue")
    part("Console_Screen", box((1.0, 0.04, 0.45), (0, FWD * 0.36, 0.70)), "glow_cyan", root)
    part("KillSwitch", merge(cyl(0.10, 0.10, 12, (0, 0, 0)), box((0.30, 0.30, 0.04), (0, 0, -0.05))),
         "red", root, (0, FWD * 0.25, 1.15))
    part("Console_Hazard", box((1.42, 0.72, 0.08), (0, 0, 0.06)), "hazard", root)
    return "KillSwitchConsole"


MODELS = [
    (model_hunter_drone, "HunterDrone.fbx"),
    (model_sentinel, "SentinelBot.fbx"),
    (model_saboteur, "SaboteurBot.fbx"),
    (model_rifle, "MistRifle.fbx"),
    (model_shotgun, "MistShotgun.fbx"),
    (model_revolver, "MistRevolver.fbx"),
    (model_smg, "MistSMG.fbx"),
    (model_glenmist_jeep, "GlenmistJeep.fbx"),
    (model_tea_crate, "TeaCrate.fbx"),
    (model_gas_canister, "GasCanister.fbx"),
    (model_rolling_machine, "TeaRollingMachine.fbx"),
    (model_withering_trough, "WitheringTrough.fbx"),
    (model_control_console, "KillSwitchConsole.fbx"),
]


def main():
    report = []
    for fn, fbx in MODELS:
        reset_scene()
        PART_COLORS.clear()
        img = build_palette_image()
        build_emission_image()
        mat = build_material(img)
        root = fn()
        unwrap_all(mat)
        export(root, fbx)
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, fbx.replace(".fbx", ".blend")))
        report.append(f"{fbx}: {tri_count(root)} tris")
    print("MODEL_REPORT\n" + "\n".join(report))


main()
