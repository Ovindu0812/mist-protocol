"""Renders a lineup preview of every exported FBX (for the report / pitch slides)."""
import bpy, os, math
ROOT = os.path.dirname(os.path.abspath(__file__))
FBX = os.path.join(ROOT, "..", "..", "Assets", "_Project", "Art", "Models")
bpy.ops.wm.read_factory_settings(use_empty=True)
x = 0.0
SPACING = 1.4
for f in ["SentinelBot", "HunterDrone", "SaboteurBot", "MistRifle", "TeaCrate", "GasCanister", "KillSwitchConsole"]:
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(FBX, f + ".fbx"))
    new = [o for o in bpy.data.objects if o not in before and o.parent is None]
    for o in new:
        o.location.x += x
        if f == "HunterDrone": o.location.z += 1.2
    x += 1.4
img = bpy.data.images.load(os.path.join(ROOT, "..", "..", "Assets", "_Project", "Art", "Textures", "MistPalette.png"))
for m in bpy.data.materials:
    m.use_nodes = True
    nt = m.node_tree
    for n in nt.nodes:
        if n.type == "TEX_IMAGE": n.image = img; n.interpolation = "Closest"
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); bpy.context.scene.collection.objects.link(cam)
cam.location = (4.0, 12.5, 2.0); cam.rotation_euler = (math.radians(80), 0, math.radians(180))
bpy.context.scene.camera = cam
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.rotation_euler = (0.8, 0.2, 0.6)
bpy.context.scene.collection.objects.link(sun)
s = bpy.context.scene
s.render.engine = "BLENDER_WORKBENCH"
s.display.shading.color_type = "TEXTURE"
s.display.shading.light = "STUDIO"
s.display.shading.show_cavity = True
s.render.resolution_x, s.render.resolution_y = 1400, 600
s.render.filepath = os.path.join(ROOT, "preview.png")
bpy.ops.render.render(write_still=True)
