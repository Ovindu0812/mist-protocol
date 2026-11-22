import bpy
import os
import math

ROOT = r"D:\mist-protocol\Art\Blender"
blend_file = os.path.join(ROOT, "ProfessorRobot_Appa.blend")
bpy.ops.wm.open_mainfile(filepath=blend_file)

# 1. Full body camera
cam_data = bpy.data.cameras.new("Cam_Full")
cam_data.lens = 40
cam_obj = bpy.data.objects.new("Cam_Full", cam_data)
bpy.context.scene.collection.objects.link(cam_obj)
cam_obj.location = (1.5, 3.4, 1.35)
cam_obj.rotation_euler = (math.radians(78), 0, math.radians(155))
bpy.context.scene.camera = cam_obj

# Add lights
sun1 = bpy.data.objects.new("Sun1", bpy.data.lights.new("Sun1", "SUN"))
sun1.rotation_euler = (math.radians(45), math.radians(25), math.radians(-30))
sun1.data.energy = 2.5
bpy.context.scene.collection.objects.link(sun1)

sun2 = bpy.data.objects.new("Sun2", bpy.data.lights.new("Sun2", "SUN"))
sun2.rotation_euler = (math.radians(60), math.radians(-40), math.radians(140))
sun2.data.energy = 1.2
bpy.context.scene.collection.objects.link(sun2)

s = bpy.context.scene
s.render.engine = "BLENDER_WORKBENCH"
s.display.shading.color_type = "TEXTURE"
s.display.shading.light = "STUDIO"
s.display.shading.show_cavity = True
s.render.resolution_x, s.render.resolution_y = 1024, 1024
preview_path = os.path.join(ROOT, "professor_preview.png")
s.render.filepath = preview_path
bpy.ops.render.render(write_still=True)
print(f"Rendered full preview to: {preview_path}")

# 2. Portrait / Close-up of Head & Chest
cam_close = bpy.data.objects.new("Cam_Close", bpy.data.cameras.new("Cam_Close"))
cam_close.data.lens = 65
cam_close.location = (0.6, 1.4, 1.62)
cam_close.rotation_euler = (math.radians(82), 0, math.radians(155))
bpy.context.scene.collection.objects.link(cam_close)
bpy.context.scene.camera = cam_close
close_path = os.path.join(ROOT, "professor_portrait.png")
s.render.filepath = close_path
bpy.ops.render.render(write_still=True)
print(f"Rendered portrait to: {close_path}")
