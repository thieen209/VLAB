"""Convert the supplied SketchUp-style GLB window to a clean Unity FBX.

The source is never modified. The imported hierarchy is flattened, normalized to
metres, centred at its sill, and exported as one static mesh for the shared lab.
"""

import sys
from pathlib import Path

import bpy
from mathutils import Vector


def argument_value(name: str) -> Path:
    arguments = sys.argv[sys.argv.index("--") + 1 :]
    index = arguments.index(name)
    return Path(arguments[index + 1]).resolve()


source_path = argument_value("--source")
output_path = argument_value("--output")
output_path.parent.mkdir(parents=True, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(source_path))

mesh_objects = [
    item
    for item in bpy.context.scene.objects
    if item.type == "MESH" and len(item.data.vertices) > 0
]
if not mesh_objects:
    raise RuntimeError(f"No mesh data found in {source_path}")


def world_dimensions(item):
    corners = [item.matrix_world @ Vector(corner) for corner in item.bound_box]
    return Vector(
        (
            max(point.x for point in corners) - min(point.x for point in corners),
            max(point.y for point in corners) - min(point.y for point in corners),
            max(point.z for point in corners) - min(point.z for point in corners),
        )
    )


# The GLB contains two very large, zero-thickness glass sheets. They would
# create overlapping transparent layers, so omit them and build one shared,
# inexpensive glass pane per installed window in Unity.
glass_sheets = []
for item in mesh_objects:
    ordered = sorted(world_dimensions(item))
    if ordered[0] < 0.01 and ordered[1] > 5.0 and ordered[2] > 10.0:
        glass_sheets.append(item)
for item in glass_sheets:
    bpy.data.objects.remove(item, do_unlink=True)
mesh_objects = [item for item in mesh_objects if item not in glass_sheets]

bpy.ops.object.select_all(action="DESELECT")
for item in mesh_objects:
    item.hide_set(False)
    item.hide_viewport = False
    item.select_set(True)

bpy.context.view_layer.objects.active = mesh_objects[0]
bpy.ops.object.convert(target="MESH")
bpy.ops.object.join()
window = bpy.context.active_object
window.name = "VLAB_Architectural_Window"

# The supplied SketchUp model uses X as thickness, Y as width and Z as height.
# Rotate it into the conventional Unity-ready X-width/Y-depth/Z-height frame.
window.rotation_euler[2] = -1.5707963267948966
bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)

world_corners = [window.matrix_world @ Vector(corner) for corner in window.bound_box]
minimum = Vector((min(point.x for point in world_corners), min(point.y for point in world_corners), min(point.z for point in world_corners)))
maximum = Vector((max(point.x for point in world_corners), max(point.y for point in world_corners), max(point.z for point in world_corners)))
dimensions = maximum - minimum
target = Vector((3.35, 0.14, 1.78))
window.scale = Vector((target.x / dimensions.x, target.y / dimensions.y, target.z / dimensions.z))
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

world_corners = [window.matrix_world @ Vector(corner) for corner in window.bound_box]
minimum = Vector((min(point.x for point in world_corners), min(point.y for point in world_corners), min(point.z for point in world_corners)))
maximum = Vector((max(point.x for point in world_corners), max(point.y for point in world_corners), max(point.z for point in world_corners)))
window.location -= Vector(((minimum.x + maximum.x) * 0.5, (minimum.y + maximum.y) * 0.5, minimum.z))
bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)

# Materials are deliberately replaced in Unity with the shared architectural
# metal and one inexpensive glass layer.
window.data.materials.clear()

bpy.ops.object.select_all(action="DESELECT")
window.select_set(True)
bpy.context.view_layer.objects.active = window
bpy.ops.export_scene.fbx(
    filepath=str(output_path),
    use_selection=True,
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_UNITS",
    axis_forward="-Z",
    axis_up="Y",
    add_leaf_bones=False,
    bake_anim=False,
    path_mode="AUTO",
)

print(f"VLAB_WINDOW_EXPORT={output_path}")
