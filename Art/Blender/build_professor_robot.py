"""
Mist Protocol - Professor Robot (Father Appa) Model Builder.
Builds the distinguished android professor / chief automaton engineer character:
- Scholarly humanoid automaton with Victorian/Dieselpunk professor aesthetics.
- Head: Intellectual cranium, swept cooling fins (scholar hair), ear acoustic dials,
        mechanical mustache & goatee, and brass round dual spectacles with cyan glowing oculars.
- Torso: Victorian blue waistcoat with brass buttons, pocket watch chain,
         draped white professor lab coat with lapels and long tailcoat flaps,
         chest supervisor ID badge, and central glowing vacuum-tube heart core.
- Left Arm: Holding an illuminated holographic research datapad / blueprint tablet.
- Right Arm: Dignified scholar stance holding an ornamental brass-handled diagnostic cane.
- Legs: Tailored mechanical trouser plating, brass knee guards, gentleman scholar boots.
"""
import bpy
import bmesh
import math
import os
from mathutils import Vector, Matrix

if "__file__" in globals():
    ROOT = os.path.dirname(os.path.abspath(__file__))
else:
    ROOT = r"D:\mist-protocol\Art\Blender"
PROJECT = os.path.abspath(os.path.join(ROOT, "..", ".."))
FBX_DIR = os.path.join(PROJECT, "Assets", "_Project", "Art", "Models")
TEX_DIR = os.path.join(PROJECT, "Assets", "_Project", "Art", "Textures")
BLEND_DIR = ROOT
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
    "white":       (0.90, 0.89, 0.86),
    "rubber":      (0.05, 0.05, 0.06),
    "concrete":    (0.47, 0.47, 0.45),
    "blue":        (0.16, 0.26, 0.42),
    "brass":       (0.74, 0.58, 0.24),
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
        bsdf.inputs["Roughness"].default_value = 0.50
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
    fbx_out = os.path.join(FBX_DIR, filename)
    bpy.ops.export_scene.fbx(
        filepath=fbx_out,
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
    print(f"Exported FBX: {fbx_out}")

# ==============================================================================
# PROFESSOR ROBOT (APPA) BUILDER
# ==============================================================================
def build_professor_robot():
    reset_scene()
    mat = build_material()

    FWD = 1.0  # +Y is forward in Blender

    # -------------------------------------------------------------------------
    # ROOT: Pelvis & Belt Chassis (at Z = 0.96m)
    # -------------------------------------------------------------------------
    root = part("ProfessorRobot_Appa", merge(
        # Main pelvis mechanical box
        box((0.32, 0.22, 0.16), (0, 0, 0.96), bevel=0.02),
        # Lateral hip socket pivot drums
        cyl(0.08, 0.36, 16, (0, 0, 0.94), rot=("Y", 90)),
        # Industrial utility belt
        box((0.34, 0.24, 0.05), (0, 0, 1.03)),
    ), "metal_dark")

    # Brass belt buckle
    part("Belt_Buckle", box((0.08, 0.04, 0.07), (0, FWD * 0.125, 1.03), bevel=0.01), "brass", root)

    # Pocket watch chain drape on pelvis
    chain_parts = []
    for step in range(5):
        t = step / 4.0
        cx = -0.04 - t * 0.08
        cy = FWD * (0.128 - math.sin(t * math.pi) * 0.02)
        cz = 1.02 - math.sin(t * math.pi) * 0.06
        chain_parts.append(sphere(0.012, subdiv=1, offset=(cx, cy, cz)))
    part("PocketWatch_Chain", merge(*chain_parts), "brass", root)

    # Professor Scholarly Frock Coat / Lab Coat (Rear tails & Hip Flaps)
    part("Coat_Tails_Rear", merge(
        # Left tail flap draping back down to knees
        box((0.14, 0.03, 0.44), (0.08, -FWD * 0.12, 0.72), bevel=0.01),
        # Right tail flap draping back down to knees
        box((0.14, 0.03, 0.44), (-0.08, -FWD * 0.12, 0.72), bevel=0.01),
        # Left side coat panel
        box((0.03, 0.20, 0.36), (0.17, 0, 0.78), bevel=0.01),
        # Right side coat panel
        box((0.03, 0.20, 0.36), (-0.17, 0, 0.78), bevel=0.01),
    ), "white", root)

    # -------------------------------------------------------------------------
    # TORSO: Core Chassis, Victorian Waistcoat, Lapels & Vacuum Core
    # -------------------------------------------------------------------------
    torso_main = merge(
        # Main upper chassis block
        box((0.36, 0.24, 0.42), (0, 0, 1.28), bevel=0.03),
        # Shoulder socket mounts
        cyl(0.07, 0.48, 16, (0, 0, 1.44), rot=("Y", 90)),
        # Back spine heat vent ribs
        box((0.18, 0.06, 0.36), (0, -FWD * 0.13, 1.30)),
    )
    torso = part("Torso", torso_main, "metal_dark", root)

    # Victorian Scholar Waistcoat (Navy Blue Tweed Vest)
    part("Waistcoat_Blue", merge(
        # Vest front plate
        box((0.32, 0.10, 0.36), (0, FWD * 0.08, 1.25), bevel=0.02),
    ), "blue", torso)

    # Vest Buttons (Brass)
    buttons = []
    for bz in [1.13, 1.20, 1.27, 1.34]:
        buttons.append(cyl(0.014, 0.03, 8, (0, FWD * 0.135, bz), rot=("X", 90)))
    part("Vest_Buttons", merge(*buttons), "brass", torso)

    # Professor Lab Coat Outer Panels & Broad Lapels (White)
    part("Coat_Lapels_White", merge(
        # Left coat lapel
        box((0.08, 0.06, 0.38), (0.15, FWD * 0.10, 1.29), bevel=0.015),
        # Right coat lapel
        box((0.08, 0.06, 0.38), (-0.15, FWD * 0.10, 1.29), bevel=0.015),
        # Left shoulder pauldron / coat shoulder cap
        box((0.12, 0.26, 0.06), (0.22, 0, 1.48), bevel=0.02),
        # Right shoulder pauldron / coat shoulder cap
        box((0.12, 0.26, 0.06), (-0.22, 0, 1.48), bevel=0.02),
        # Rear coat mantle across back shoulders
        box((0.38, 0.05, 0.38), (0, -FWD * 0.14, 1.31), bevel=0.02),
    ), "white", torso)

    # Breast Pocket with Pens / Stylus (Left Chest)
    pens_and_pocket = merge(
        # Pocket flap
        box((0.07, 0.02, 0.02), (0.13, FWD * 0.135, 1.35)),
        # Golden stylus 1
        cyl(0.007, 0.07, 8, (0.115, FWD * 0.14, 1.38)),
        # Silver stylus 2
        cyl(0.006, 0.06, 8, (0.145, FWD * 0.14, 1.37)),
    )
    part("Chest_Pocket_Pens", pens_and_pocket, "brass", torso)

    # Glenmist Chief Engineer Supervisor Golden Badge
    badge_mesh = merge(
        # Star / shield plaque
        box((0.07, 0.02, 0.08), (0.13, FWD * 0.135, 1.43), bevel=0.01),
        # Pin clasp
        cyl(0.01, 0.03, 8, (0.13, FWD * 0.14, 1.45), rot=("X", 90)),
    )
    part("Supervisor_Badge", badge_mesh, "brass", torso)

    # Supervisor Badge LED indicator
    part("Badge_LED", box((0.02, 0.01, 0.02), (0.13, FWD * 0.146, 1.42)), "glow_cyan", torso)

    # Exposed Heart Core / Vacuum Tube Chamber (The robot's gentle warmth / core)
    core_housing = merge(
        # Outer brass ring bezel
        cyl(0.055, 0.04, 16, (0, FWD * 0.12, 1.25), rot=("X", 90)),
    )
    part("Heart_Bezel", core_housing, "brass", torso)

    # Glowing Vacuum Tube Core
    part("Heart_Vacuum_Core", cyl(0.04, 0.05, 16, (0, FWD * 0.125, 1.25), rot=("X", 90)), "glow_cyan", torso)

    # High Scholar Victorian Neck Collar
    part("Scholar_Collar", merge(
        # Left upturned collar wing
        box((0.04, 0.16, 0.09), (0.07, FWD * 0.02, 1.54), bevel=0.01),
        # Right upturned collar wing
        box((0.04, 0.16, 0.09), (-0.07, FWD * 0.02, 1.54), bevel=0.01),
    ), "white", torso)

    # Scholarly Bowtie / Cravat (Brass)
    bowtie_mesh = merge(
        # Center knot
        box((0.03, 0.03, 0.03), (0, FWD * 0.13, 1.51)),
        # Left butterfly flare
        box((0.05, 0.02, 0.045), (0.045, FWD * 0.125, 1.51), bevel=0.008),
        # Right butterfly flare
        box((0.05, 0.02, 0.045), (-0.045, FWD * 0.125, 1.51), bevel=0.008),
    )
    part("Bowtie", bowtie_mesh, "brass", torso)

    # -------------------------------------------------------------------------
    # HEAD & SCHOLAR VISAGE
    # -------------------------------------------------------------------------
    head_loc = (0, 0, 1.54)
    head_mesh = merge(
        # Neck mechanical vertebrae cylinder
        cyl(0.05, 0.10, 12, (0, 0, 0.05)),
        # Cranium lower jaw & skull base
        box((0.20, 0.22, 0.22), (0, FWD * 0.02, 0.20), bevel=0.03),
        # Cranium intellectual dome curve
        sphere(0.12, subdiv=2, scale=(0.95, 1.05, 0.70), offset=(0, FWD * 0.01, 0.28)),
    )
    head = part("Head", head_mesh, "metal_dark", torso, loc=head_loc)

    # Side Cooling Heatsink Fins (Stylized as distinguished swept scholar hair)
    hair_fins = []
    for side in [1, -1]:
        for fi in range(4):
            fz = 0.20 + fi * 0.025
            fy = -FWD * 0.02 - fi * 0.015
            hair_fins.append(box((0.025, 0.14, 0.014), (side * 0.115, fy, fz), bevel=0.003))
        # Rear occipital plates
        hair_fins.append(box((0.14, 0.025, 0.12), (0, -FWD * 0.10, 0.22), bevel=0.01))
    part("Scholar_Hair_Fins", merge(*hair_fins), "metal_light", head)

    # Side Ear Rotary Dials / Acoustic Sensors (Steampunk acoustic horns)
    ear_dials = merge(
        cyl(0.035, 0.04, 12, (0.115, 0, 0.20), rot=("Y", 90)),
        cyl(0.035, 0.04, 12, (-0.115, 0, 0.20), rot=("Y", 90)),
    )
    part("Ear_Acoustic_Dials", ear_dials, "brass", head)

    # Distinguished Mechanical Mustache & Goatee
    mustache_parts = [
        # Left sweeping handlebar mustache segment
        cyl(0.016, 0.08, 8, (0.045, FWD * 0.138, 0.14), rot=("Y", 70)),
        sphere(0.018, subdiv=1, offset=(0.085, FWD * 0.135, 0.145)),
        # Right sweeping handlebar mustache segment
        cyl(0.016, 0.08, 8, (-0.045, FWD * 0.138, 0.14), rot=("Y", -70)),
        sphere(0.018, subdiv=1, offset=(-0.085, FWD * 0.135, 0.145)),
        # Central philtrum mustache bracket
        box((0.04, 0.03, 0.025), (0, FWD * 0.135, 0.15)),
        # Trimmed chin goatee plate
        box((0.05, 0.04, 0.04), (0, FWD * 0.12, 0.08), bevel=0.01),
    ]
    part("Mechanical_Mustache", merge(*mustache_parts), "brass", head)

    # Professor Round Spectacles Frame & Ocular Lenses
    glasses_frames = merge(
        # Left circular rim
        cyl(0.045, 0.025, 16, (0.052, FWD * 0.135, 0.215), rot=("X", 90)),
        # Right circular rim
        cyl(0.045, 0.025, 16, (-0.052, FWD * 0.135, 0.215), rot=("X", 90)),
        # Bridge over nose connecting frames
        box((0.035, 0.015, 0.012), (0, FWD * 0.14, 0.215)),
        # Left wire temple arm running back to ear dial
        cyl(0.005, 0.14, 8, (0.10, FWD * 0.07, 0.21), rot=("X", 90)),
        # Right wire temple arm running back to ear dial
        cyl(0.005, 0.14, 8, (-0.10, FWD * 0.07, 0.21), rot=("X", 90)),
    )
    part("Spectacles_Frame", glasses_frames, "brass", head)

    # Glowing Cyan Ocular Lenses (Professor's eyes)
    lenses = merge(
        cyl(0.038, 0.028, 16, (0.052, FWD * 0.138, 0.215), rot=("X", 90)),
        cyl(0.038, 0.028, 16, (-0.052, FWD * 0.138, 0.215), rot=("X", 90)),
    )
    part("Spectacles_Lenses_Glow", lenses, "glow_cyan", head)

    # -------------------------------------------------------------------------
    # LEFT ARM: Holding Holographic Research Datapad
    # -------------------------------------------------------------------------
    # Shoulder joint (Left)
    part("Shoulder_L", sphere(0.065, subdiv=2, offset=(0.24, 0, 1.44)), "brass", torso)

    # Upper Arm Left (White coat sleeve)
    part("Arm_L", merge(
        cyl(0.062, 0.24, 12, (0.26, FWD * 0.03, 1.30), rot=("X", 10)),
    ), "white", torso)

    # Elbow Joint Left
    part("Elbow_L", cyl(0.042, 0.09, 12, (0.27, FWD * 0.07, 1.17), rot=("Y", 90)), "brass", torso)

    # Forearm Left (Angled forward to support the datapad)
    forearm_l = merge(
        cyl(0.052, 0.22, 12, (0.24, FWD * 0.17, 1.11), rot=("X", -50)),
        # Wrist cuff
        box((0.08, 0.08, 0.04), (0.21, FWD * 0.25, 1.05)),
    )
    part("Forearm_L", forearm_l, "white", torso)

    # Robot Hand Left
    hand_l = merge(
        # Palm
        box((0.06, 0.08, 0.03), (0.19, FWD * 0.27, 1.03), bevel=0.008),
        # Articulated fingers wrapped onto tablet edge
        cyl(0.012, 0.06, 8, (0.22, FWD * 0.30, 1.06), rot=("X", 60)),
        cyl(0.012, 0.06, 8, (0.19, FWD * 0.30, 1.06), rot=("X", 60)),
        cyl(0.012, 0.06, 8, (0.16, FWD * 0.30, 1.06), rot=("X", 60)),
    )
    part("Hand_L", hand_l, "metal_light", torso)

    # Holographic Research Datapad / Blueprint Tablet (Held by Left Hand)
    datapad_body = merge(
        # Outer casing tablet plate
        box((0.26, 0.20, 0.024), (0.15, FWD * 0.29, 1.08), bevel=0.01),
        # Brass edge protector corners
        box((0.27, 0.04, 0.03), (0.15, FWD * 0.39, 1.08)),
        box((0.27, 0.04, 0.03), (0.15, FWD * 0.19, 1.08)),
        # Antenna stylus clip
        cyl(0.006, 0.12, 8, (0.01, FWD * 0.29, 1.08), rot=("Y", 90)),
    )
    part("Datapad_Chassis", datapad_body, "metal_dark", torso)

    # Datapad Glowing Blueprint Screen
    part("Datapad_Screen_Glow", box((0.23, 0.17, 0.008), (0.15, FWD * 0.29, 1.096)), "glow_cyan", torso)

    # -------------------------------------------------------------------------
    # RIGHT ARM: Scholar Cane / Diagnostic Probe Stance
    # -------------------------------------------------------------------------
    # Shoulder joint (Right)
    part("Shoulder_R", sphere(0.065, subdiv=2, offset=(-0.24, 0, 1.44)), "brass", torso)

    # Upper Arm Right (White coat sleeve, hanging comfortably down)
    part("Arm_R", merge(
        cyl(0.062, 0.24, 12, (-0.26, 0, 1.30)),
    ), "white", torso)

    # Elbow Joint Right
    part("Elbow_R", cyl(0.042, 0.09, 12, (-0.26, FWD * 0.02, 1.16), rot=("Y", 90)), "brass", torso)

    # Forearm Right (Extending down and slightly forward to hold cane handle)
    forearm_r = merge(
        cyl(0.052, 0.22, 12, (-0.25, FWD * 0.08, 1.04), rot=("X", -25)),
        # Wrist cuff
        box((0.07, 0.07, 0.04), (-0.24, FWD * 0.14, 0.94)),
    )
    part("Forearm_R", forearm_r, "white", torso)

    # Robot Hand Right (Clasped over cane head)
    hand_r = merge(
        box((0.06, 0.06, 0.04), (-0.24, FWD * 0.15, 0.93), bevel=0.008),
        # Curled gripping fingers
        cyl(0.012, 0.05, 8, (-0.26, FWD * 0.18, 0.91), rot=("X", 90)),
        cyl(0.012, 0.05, 8, (-0.24, FWD * 0.18, 0.91), rot=("X", 90)),
        cyl(0.012, 0.05, 8, (-0.22, FWD * 0.18, 0.91), rot=("X", 90)),
    )
    part("Hand_R", hand_r, "metal_light", torso)

    # Professor's Gentleman Cane / Diagnostic Probe
    cane_handle = merge(
        # Brass spherical ornamental knob
        sphere(0.038, subdiv=2, offset=(-0.24, FWD * 0.15, 0.95)),
        # Brass neck collar & ring
        cyl(0.018, 0.04, 12, (-0.24, FWD * 0.15, 0.91)),
        # Brass ferrule foot at the floor
        cyl(0.014, 0.06, 8, (-0.24, FWD * 0.15, 0.03)),
    )
    part("Cane_Brass_Accents", cane_handle, "brass", torso)

    # Cane Shaft (Dark lacquered steel)
    part("Cane_Shaft", cyl(0.012, 0.88, 8, (-0.24, FWD * 0.15, 0.48)), "metal_dark", torso)

    # -------------------------------------------------------------------------
    # LEGS: Trousers, Brass Knee Guards, Gentleman Scholar Boots
    # -------------------------------------------------------------------------
    for side, prefix in [(1, "L"), (-1, "R")]:
        lx = side * 0.11

        # Upper Thigh / Trouser Leg
        thigh_mesh = merge(
            # Hip ball
            sphere(0.06, subdiv=2, offset=(lx, 0, 0.91)),
            # Trouser upper leg cylinder
            cyl(0.075, 0.36, 12, (lx, 0, 0.72)),
        )
        part(f"Leg_Upper_{prefix}", thigh_mesh, "metal_dark", root)

        # Knee Joint & Brass Knee Guard
        knee_joint = merge(
            cyl(0.05, 0.12, 12, (lx, 0, 0.52), rot=("Y", 90)),
            # Brass oval knee cap protector
            box((0.11, 0.05, 0.11), (lx, FWD * 0.06, 0.52), bevel=0.015),
        )
        part(f"Knee_{prefix}", knee_joint, "brass", root)

        # Lower Leg / Shin
        shin_mesh = merge(
            # Main shin
            cyl(0.065, 0.36, 12, (lx, 0, 0.32)),
            # Rear mechanical piston shock strut
            cyl(0.02, 0.28, 8, (lx, -FWD * 0.055, 0.32)),
        )
        part(f"Leg_Lower_{prefix}", shin_mesh, "metal_dark", root)

        # Scholar Gentleman Mechanical Boot
        boot_mesh = merge(
            # Boot body / leather upper
            box((0.11, 0.22, 0.10), (lx, FWD * 0.03, 0.08), bevel=0.015),
            # Rounded toe cap
            cyl(0.055, 0.10, 12, (lx, FWD * 0.11, 0.08)),
            # Sole tread
            box((0.12, 0.24, 0.03), (lx, FWD * 0.03, 0.015)),
        )
        part(f"Foot_{prefix}", boot_mesh, "rubber", root)

        # Brass Boot Buckles
        boot_buckles = merge(
            box((0.02, 0.035, 0.035), (lx + side * 0.06, FWD * 0.02, 0.09)),
            box((0.02, 0.035, 0.035), (lx + side * 0.06, FWD * 0.07, 0.09)),
        )
        part(f"Boot_Buckles_{prefix}", boot_buckles, "brass", root)

    # -------------------------------------------------------------------------
    # UV UNWRAP & EXPORT
    # -------------------------------------------------------------------------
    unwrap_all(mat)

    # Save .blend file
    blend_path = os.path.join(BLEND_DIR, "ProfessorRobot_Appa.blend")
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"Saved Blend file: {blend_path}")

    # Export FBX to Assets/_Project/Art/Models/ProfessorRobot_Appa.fbx
    export("ProfessorRobot_Appa", "ProfessorRobot_Appa.fbx")
    print("Successfully built ProfessorRobot_Appa!")

if __name__ == "__main__":
    build_professor_robot()
