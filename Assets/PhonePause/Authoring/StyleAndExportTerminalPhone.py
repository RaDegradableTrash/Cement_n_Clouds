import bpy
from mathutils import Vector
from pathlib import Path


PROJECT = Path(__file__).resolve().parents[3]
SOURCE_OUT = PROJECT / "Assets" / "PhonePause" / "Source" / "TerminalPhone_Styled.blend"
FBX_OUT = PROJECT / "Assets" / "Resources" / "PhonePause" / "TerminalPhone.fbx"


def color(hex_value):
    value = hex_value.lstrip("#")
    rgb = tuple(int(value[i:i + 2], 16) / 255.0 for i in (0, 2, 4))
    return (rgb[0], rgb[1], rgb[2], 1.0)


def make_material(name, base_hex, metallic=0.0, roughness=0.45, alpha=1.0, emission_hex=None, emission_strength=0.0):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color(base_hex)[:3], alpha)
    mat.use_nodes = True
    mat.surface_render_method = 'DITHERED' if alpha < 1.0 else 'DITHERED'
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color(base_hex)[:3], alpha)
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Alpha"].default_value = alpha
        if emission_hex:
            bsdf.inputs["Emission Color"].default_value = color(emission_hex)
            bsdf.inputs["Emission Strength"].default_value = emission_strength
    return mat


def assign(obj_name, material):
    obj = bpy.data.objects.get(obj_name)
    if not obj or obj.type != 'MESH':
        return
    obj.data.materials.clear()
    obj.data.materials.append(material)
    for polygon in obj.data.polygons:
        polygon.material_index = 0


mat_shell = make_material("VOYAGE | Deep-Sea Titanium", "182833", metallic=0.72, roughness=0.31)
mat_bezel = make_material("VOYAGE | Brushed Alloy", "38505A", metallic=0.82, roughness=0.25)
mat_frame = make_material("VOYAGE | Graphite Ceramic", "0D151C", metallic=0.35, roughness=0.28)
mat_grip = make_material("VOYAGE | Slate Polymer", "243640", metallic=0.18, roughness=0.58)
mat_button = make_material("VOYAGE | Signal Coral", "EE8B63", metallic=0.38, roughness=0.24)
mat_button_alt = make_material("VOYAGE | Sea Glass", "50D2BC", metallic=0.32, roughness=0.22)
mat_screen = make_material("VOYAGE | OLED Black", "080B10", metallic=0.08, roughness=0.16, emission_hex="05090D", emission_strength=0.12)
mat_clear = make_material("VOYAGE | Transparent UI Surface", "FFFFFF", metallic=0.0, roughness=1.0, alpha=0.0)

assign("Cube", mat_shell)
assign("Cube.001", mat_bezel)
assign("Cube.002", mat_frame)
assign("Cube.003", mat_grip)
assign("Cube.004", mat_button)
assign("Cube.005", mat_button_alt)
assign("Cube.006", mat_button)
assign("Plane", mat_screen)
assign("Plane.001", mat_clear)
assign("Plane.002", mat_clear)

scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 1200
scene.render.resolution_y = 1600
scene.render.resolution_percentage = 75
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
scene.render.image_settings.color_mode = 'RGBA'
scene.view_settings.view_transform = 'AgX'
scene.world.color = (0.008, 0.018, 0.028)

# Keep the viewport presentation legible when the saved project is opened.
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D':
            area.spaces.active.shading.type = 'MATERIAL'
            area.spaces.active.region_3d.view_distance = 4.3
            area.spaces.active.region_3d.view_location = (0.0, 0.0, 0.0)

# Keep the authored meshes centered and name the object groups clearly in Blender.
for name in ("Cube", "Cube.001", "Cube.002", "Cube.003", "Cube.004", "Cube.005", "Cube.006"):
    obj = bpy.data.objects.get(name)
    if obj:
        obj.color = (0.12, 0.23, 0.29, 1.0)
for name in ("Plane.001", "Plane.002"):
    obj = bpy.data.objects.get(name)
    if obj:
        obj["display_surface"] = "transparent_overlay"
        obj["unity_material"] = "Alpha 0 / UI overlay only"
if bpy.data.objects.get("Plane"):
    bpy.data.objects["Plane"]["display_surface"] = "main_oled"
    bpy.data.objects["Plane"]["unity_material"] = "Near-black default OLED"

SOURCE_OUT.parent.mkdir(parents=True, exist_ok=True)
FBX_OUT.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE_OUT))

mesh_objects = [o for o in scene.objects if o.type == 'MESH']
bpy.ops.object.select_all(action='DESELECT')
for obj in mesh_objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = bpy.data.objects.get("Cube")
bpy.ops.export_scene.fbx(
    filepath=str(FBX_OUT),
    use_selection=True,
    object_types={'MESH'},
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_UNITS',
    axis_forward='-Z',
    axis_up='Y',
    use_mesh_modifiers=True,
    mesh_smooth_type='OFF',
    use_mesh_edges=False,
    use_tspace=True,
    path_mode='AUTO',
    embed_textures=True,
    add_leaf_bones=False,
    bake_anim=False,
)
print("Wrote styled Blender source:", SOURCE_OUT)
print("Exported Unity FBX:", FBX_OUT)
