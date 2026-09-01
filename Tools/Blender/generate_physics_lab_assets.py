"""Generate the VLAB Physics Lab production asset library in Blender.

Run inside Blender. The scene is rebuilt intentionally and saved as a master working
file. Each top-level asset collection is also exported as a metric Unity-ready FBX.
"""

import math
import os

import bpy
from mathutils import Matrix, Vector


ASSET_ROOT = r"D:\VLAB\files 3d lý"
WORKING_BLEND = os.path.join(ASSET_ROOT, "01_Blender_Working", "PhysicsLabAssetLibrary.blend")
EXPORT_ROOT = os.path.join(ASSET_ROOT, "02_Exported_Models")
UNITY_READY = os.path.join(ASSET_ROOT, "04_Unity_Ready")
SOURCE_STAND = os.path.join(
    ASSET_ROOT,
    "00_Source_Original",
    "iron_stand_with_base_and_clamp_arrangement.glb",
)


def ensure_directories():
    for directory in (
        os.path.dirname(WORKING_BLEND),
        EXPORT_ROOT,
        UNITY_READY,
    ):
        os.makedirs(directory, exist_ok=True)
    for category in ("Measurement", "Mechanics", "Oscillation", "AirTrack", "Projectile", "Common"):
        os.makedirs(os.path.join(EXPORT_ROOT, category), exist_ok=True)


def material(name, color, metallic=0.0, roughness=0.45, alpha=1.0):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, alpha)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color, alpha)
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Alpha"].default_value = alpha
    if alpha < 1.0:
        mat.surface_render_method = "DITHERED"
    return mat


def move_to_collection(obj, collection):
    for current in list(obj.users_collection):
        current.objects.unlink(obj)
    collection.objects.link(obj)


def parent_keep_world(obj, root):
    world = obj.matrix_world.copy()
    obj.parent = root
    obj.matrix_world = world


def apply_rotation_scale(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj.select_set(False)


def bevel(obj, width=0.002, segments=2):
    if width <= 0:
        return
    modifier = obj.modifiers.new("EdgeBevel", "BEVEL")
    modifier.width = width
    modifier.segments = segments


def add_box(collection, root, name, dimensions, location, mat, bevel_width=0.002):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.dimensions = dimensions
    apply_rotation_scale(obj)
    bevel(obj, min(bevel_width, min(dimensions) * 0.2), 2)
    obj.data.materials.append(mat)
    move_to_collection(obj, collection)
    parent_keep_world(obj, root)
    return obj


def add_cylinder(collection, root, name, radius, depth, location, mat, rotation=(0, 0, 0), vertices=32, bevel_width=0.001):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    apply_rotation_scale(obj)
    bevel(obj, min(bevel_width, radius * 0.15, depth * 0.15), 2)
    obj.data.materials.append(mat)
    move_to_collection(obj, collection)
    parent_keep_world(obj, root)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return obj


def add_sphere(collection, root, name, radius, location, mat, segments=32, rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=radius, location=location)
    obj = bpy.context.active_object
    obj.name = name
    apply_rotation_scale(obj)
    obj.data.materials.append(mat)
    move_to_collection(obj, collection)
    parent_keep_world(obj, root)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return obj


def add_torus(collection, root, name, major_radius, minor_radius, location, mat, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius,
        minor_radius=minor_radius,
        major_segments=32,
        minor_segments=10,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.active_object
    obj.name = name
    apply_rotation_scale(obj)
    obj.data.materials.append(mat)
    move_to_collection(obj, collection)
    parent_keep_world(obj, root)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return obj


def add_rod_between(collection, root, name, p0, p1, radius, mat, vertices=24):
    start, end = Vector(p0), Vector(p1)
    direction = end - start
    midpoint = (start + end) * 0.5
    rotation = direction.to_track_quat("Z", "Y").to_euler()
    return add_cylinder(collection, root, name, radius, direction.length, midpoint, mat, rotation, vertices)


def add_text(collection, root, name, text, location, size, mat, rotation=(math.radians(90), 0, 0), align="CENTER"):
    bpy.ops.object.text_add(location=location, rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    obj.data.body = text
    obj.data.align_x = align
    obj.data.align_y = "CENTER"
    obj.data.size = size
    obj.data.extrude = 0.00035
    obj.data.bevel_depth = 0.00015
    obj.data.materials.append(mat)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target="MESH")
    apply_rotation_scale(obj)
    move_to_collection(obj, collection)
    parent_keep_world(obj, root)
    return obj


def add_coil(collection, root, name, radius, wire_radius, length, turns, location, mat):
    curve = bpy.data.curves.new(name + "_Curve", "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 1
    curve.bevel_depth = wire_radius
    curve.bevel_resolution = 2
    spline = curve.splines.new("POLY")
    points_per_turn = 18
    count = turns * points_per_turn + 1
    spline.points.add(count - 1)
    base = Vector(location)
    for index in range(count):
        ratio = index / (count - 1)
        angle = ratio * turns * math.tau
        x = base.x + radius * math.cos(angle)
        y = base.y + radius * math.sin(angle)
        z = base.z + length * ratio
        spline.points[index].co = (x, y, z, 1.0)
    obj = bpy.data.objects.new(name, curve)
    collection.objects.link(obj)
    curve.materials.append(mat)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    obj.select_set(False)
    parent_keep_world(obj, root)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return obj


def new_asset(name, category):
    collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(collection)
    root = bpy.data.objects.new(name, None)
    root.empty_display_type = "PLAIN_AXES"
    root["vlab_asset_name"] = name
    root["vlab_category"] = category
    collection.objects.link(root)
    ASSETS[name] = (collection, root, category)
    return collection, root


def repair_retort_stand():
    collection, root = new_asset("Laboratory_Retort_Stand", "Common")
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=SOURCE_STAND)
    imported = [obj for obj in bpy.data.objects if obj not in before]
    meshes = []
    for obj in imported:
        if obj.type != "MESH":
            continue
        source_materials = {mat.name for mat in obj.data.materials if mat}
        is_baked_string = any(name.startswith("Thread") for name in source_materials)
        is_hanging_pan = obj.name in {"pCylinder9_Metal_0", "pCylinder10_Black_0"}
        if is_baked_string or is_hanging_pan:
            bpy.data.objects.remove(obj, do_unlink=True)
            continue
        meshes.append(obj)
    scale_matrix = Matrix.Scale(10.0, 4)
    for index, obj in enumerate(meshes, 1):
        world = scale_matrix @ obj.matrix_world
        obj.parent = None
        obj.matrix_world = world
        obj.name = f"Stand_Part_{index:02d}"
        apply_rotation_scale(obj)
        move_to_collection(obj, collection)
    bpy.context.view_layer.update()
    if meshes:
        corners = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
        minimum = Vector((min(p.x for p in corners), min(p.y for p in corners), min(p.z for p in corners)))
        maximum = Vector((max(p.x for p in corners), max(p.y for p in corners), max(p.z for p in corners)))
        offset = Vector((-(minimum.x + maximum.x) * 0.5, -(minimum.y + maximum.y) * 0.5, -minimum.z))
        for obj in meshes:
            obj.location += offset
            parent_keep_world(obj, root)
    for obj in [candidate for candidate in list(bpy.data.objects) if candidate not in before and candidate.type != "MESH"]:
        bpy.data.objects.remove(obj, do_unlink=True)
    root["source"] = "Repaired supplied GLB"
    root["height_m"] = 0.72


def build_adjustable_clamp():
    c, r = new_asset("Adjustable_Clamp", "Common")
    add_box(c, r, "BossHead", (0.055, 0.045, 0.05), (0, 0, 0.08), MAT_DARK, 0.006)
    add_cylinder(c, r, "StandSocket", 0.012, 0.06, (0, 0, 0.08), MAT_BLACK, (math.radians(90), 0, 0), 24)
    add_rod_between(c, r, "ClampArm", (0.025, 0, 0.08), (0.16, 0, 0.08), 0.006, MAT_STEEL)
    add_box(c, r, "ClampJawFixed", (0.012, 0.038, 0.055), (0.165, 0, 0.08), MAT_STEEL, 0.003)
    add_box(c, r, "ClampJawMoving", (0.012, 0.038, 0.055), (0.195, 0, 0.08), MAT_STEEL, 0.003)
    add_rod_between(c, r, "ClampScrew", (0.18, 0, 0.08), (0.225, 0, 0.08), 0.004, MAT_DARK)
    add_cylinder(c, r, "ClampKnob", 0.014, 0.01, (0.23, 0, 0.08), MAT_BLUE, (0, math.radians(90), 0), 24)


def build_table():
    c, r = new_asset("Experiment_Table", "Common")
    add_box(c, r, "TableTop", (1.5, 0.75, 0.055), (0, 0, 0.78), MAT_WOOD, 0.015)
    for x in (-0.66, 0.66):
        for y in (-0.29, 0.29):
            add_box(c, r, f"Leg_{'L' if x < 0 else 'R'}_{'F' if y < 0 else 'B'}", (0.065, 0.065, 0.75), (x, y, 0.375), MAT_DARK, 0.008)
    add_box(c, r, "FrontBrace", (1.32, 0.035, 0.07), (0, -0.30, 0.32), MAT_DARK, 0.006)
    add_box(c, r, "RearBrace", (1.32, 0.035, 0.07), (0, 0.30, 0.32), MAT_DARK, 0.006)


def build_ruler():
    c, r = new_asset("Laboratory_Meter_Ruler", "Measurement")
    add_box(c, r, "RulerBody", (1.02, 0.008, 0.04), (0.5, 0, 0.02), MAT_RULER, 0.002)
    ticks = []
    for index in range(101):
        x = index * 0.01
        height = 0.018 if index % 10 == 0 else (0.012 if index % 5 == 0 else 0.007)
        ticks.append(add_box(c, r, f"Tick_{index:03d}", (0.0008, 0.0015, height), (x, -0.0048, 0.04 - height * 0.5), MAT_BLACK, 0))
    bpy.ops.object.select_all(action="DESELECT")
    for tick in ticks:
        tick.select_set(True)
    bpy.context.view_layer.objects.active = ticks[0]
    bpy.ops.object.join()
    ticks[0].name = "RulerTicks"
    ticks[0].select_set(False)
    add_text(c, r, "RulerLabel", "1 m", (0.5, -0.005, 0.017), 0.022, MAT_BLACK)


def build_timer():
    c, r = new_asset("Digital_Timer_MC964", "Measurement")
    add_box(c, r, "TimerBody", (0.32, 0.12, 0.19), (0, 0, 0.105), MAT_IVORY, 0.015)
    add_box(c, r, "FrontPanel", (0.292, 0.012, 0.155), (0, -0.062, 0.112), MAT_DARK, 0.006)
    add_box(c, r, "DisplayWindow", (0.17, 0.004, 0.052), (-0.035, -0.069, 0.145), MAT_DISPLAY, 0.004)
    add_text(c, r, "ModelLabel", "MC-964", (-0.074, -0.072, 0.184), 0.018, MAT_LIGHT)
    add_text(c, r, "DisplayDigits", "0.000", (-0.035, -0.072, 0.145), 0.034, MAT_LED)
    for name, x, color in (("Port_A", -0.105, MAT_RED), ("Port_B", 0.0, MAT_YELLOW), ("Port_C", 0.105, MAT_BLUE)):
        add_cylinder(c, r, name, 0.014, 0.012, (x, -0.067, 0.064), color, (math.radians(90), 0, 0), 24)
        add_text(c, r, name + "_Label", name[-1], (x, -0.075, 0.09), 0.016, MAT_LIGHT)
    for name, x, label in (("Button_Mode", 0.082, "MODE"), ("Button_Reset", 0.125, "RESET"), ("Button_Resolution", 0.082, "RES")):
        z = 0.145 if name != "Button_Resolution" else 0.108
        add_box(c, r, name, (0.035, 0.012, 0.024), (x, -0.071, z), MAT_BLUE if name != "Button_Reset" else MAT_RED, 0.005)
        add_text(c, r, name + "_Label", label, (x, -0.078, z), 0.008, MAT_LIGHT)


def build_photogate():
    c, r = new_asset("Physics_Photogate", "Measurement")
    add_box(c, r, "PhotogateBase", (0.19, 0.08, 0.025), (0, 0, 0.0125), MAT_DARK, 0.008)
    add_box(c, r, "LeftPost", (0.035, 0.04, 0.22), (-0.0775, 0, 0.13), MAT_BLUE, 0.008)
    add_box(c, r, "RightPost", (0.035, 0.04, 0.22), (0.0775, 0, 0.13), MAT_BLUE, 0.008)
    add_box(c, r, "TopBridge", (0.19, 0.04, 0.04), (0, 0, 0.24), MAT_BLUE, 0.008)
    add_cylinder(c, r, "BeamEmitter", 0.009, 0.006, (-0.058, -0.021, 0.14), MAT_RED, (math.radians(90), 0, 0), 24)
    add_cylinder(c, r, "BeamReceiver", 0.009, 0.006, (0.058, -0.021, 0.14), MAT_BLACK, (math.radians(90), 0, 0), 24)
    add_text(c, r, "PhotogateLabel", "VLAB GATE", (0, -0.022, 0.242), 0.014, MAT_LIGHT)


def build_flag():
    c, r = new_asset("Photogate_Flag", "Measurement")
    add_box(c, r, "FlagBlade", (0.05, 0.003, 0.08), (0, 0, 0.09), MAT_BLACK, 0.001)
    add_box(c, r, "FlagStem", (0.008, 0.008, 0.09), (0, 0, 0.045), MAT_STEEL, 0.002)
    r["flag_width_m"] = 0.05


def build_force_meter():
    c, r = new_asset("Spring_Force_Meter", "Measurement")
    add_box(c, r, "ForceMeterBody", (0.055, 0.028, 0.30), (0, 0, 0.17), MAT_BLUE, 0.008)
    add_box(c, r, "ScaleWindow", (0.038, 0.004, 0.22), (0, -0.016, 0.175), MAT_LIGHT, 0.002)
    for index in range(11):
        z = 0.075 + index * 0.018
        add_box(c, r, f"ScaleTick_{index:02d}", (0.018 if index % 5 else 0.028, 0.0015, 0.001), (0.006, -0.019, z), MAT_BLACK, 0)
    add_box(c, r, "ForceIndicator", (0.032, 0.004, 0.004), (0, -0.020, 0.17), MAT_RED, 0.001)
    add_torus(c, r, "TopHook", 0.022, 0.0035, (0, 0, 0.345), MAT_STEEL, (math.radians(90), 0, 0))
    add_rod_between(c, r, "BottomHookStem", (0, 0, 0.02), (0, 0, -0.025), 0.0035, MAT_STEEL)
    add_torus(c, r, "BottomHook", 0.018, 0.0035, (0, 0, -0.045), MAT_STEEL, (math.radians(90), 0, 0))
    add_text(c, r, "ForceMeterLabel", "0-10 N", (0, -0.019, 0.292), 0.012, MAT_LIGHT)


def build_pendulum_bob():
    c, r = new_asset("Pendulum_Bob", "Oscillation")
    add_sphere(c, r, "BobSphere", 0.035, (0, 0, 0.035), MAT_BRASS)
    add_torus(c, r, "BobEyelet", 0.008, 0.0025, (0, 0, 0.075), MAT_STEEL, (math.radians(90), 0, 0))
    r["default_mass_kg"] = 0.20


def build_string_anchor():
    c, r = new_asset("Pendulum_String_Anchor", "Oscillation")
    add_box(c, r, "AnchorBody", (0.055, 0.045, 0.035), (0, 0, 0.03), MAT_DARK, 0.006)
    add_torus(c, r, "StringEye", 0.012, 0.003, (0, 0, 0.002), MAT_STEEL, (math.radians(90), 0, 0))
    add_cylinder(c, r, "AnchorKnob", 0.013, 0.012, (0.032, 0, 0.03), MAT_BLUE, (0, math.radians(90), 0), 24)


def build_spring():
    c, r = new_asset("Physics_Coil_Spring", "Oscillation")
    add_coil(c, r, "SpringCoil", 0.027, 0.0026, 0.32, 22, (0, 0, 0.04), MAT_STEEL)
    add_torus(c, r, "TopEye", 0.015, 0.0028, (0, 0, 0.385), MAT_STEEL, (math.radians(90), 0, 0))
    add_torus(c, r, "BottomEye", 0.015, 0.0028, (0, 0, 0.015), MAT_STEEL, (math.radians(90), 0, 0))
    r["natural_length_m"] = 0.37
    r["default_spring_constant_n_per_m"] = 18.0


def build_mass_set():
    c, r = new_asset("Mass_Set", "Oscillation")
    add_rod_between(c, r, "MassHangerStem", (-0.20, 0, 0.02), (-0.20, 0, 0.20), 0.003, MAT_STEEL)
    add_torus(c, r, "MassHangerHook", 0.014, 0.003, (-0.20, 0, 0.215), MAT_STEEL, (math.radians(90), 0, 0))
    add_cylinder(c, r, "MassHangerTray", 0.045, 0.006, (-0.20, 0, 0.02), MAT_STEEL, vertices=32)
    masses = (("Mass_50g", -0.09, 0.022), ("Mass_100g", 0.02, 0.030), ("Mass_200g", 0.14, 0.038), ("Mass_500g", 0.29, 0.050))
    for name, x, radius in masses:
        add_cylinder(c, r, name, radius, 0.018, (x, 0, 0.012), MAT_BRASS, vertices=32, bevel_width=0.002)
        add_cylinder(c, r, name + "_Slot", 0.006, 0.022, (x, 0, 0.012), MAT_DARK, vertices=20, bevel_width=0)


def build_launcher():
    c, r = new_asset("Physics_Projectile_Launcher", "Projectile")
    add_box(c, r, "LauncherBase", (0.34, 0.22, 0.035), (0, 0, 0.018), MAT_DARK, 0.015)
    add_box(c, r, "LauncherSupport", (0.055, 0.12, 0.18), (-0.105, 0, 0.12), MAT_BLUE, 0.01)
    add_cylinder(c, r, "LauncherAnglePivot", 0.045, 0.14, (-0.105, 0, 0.185), MAT_STEEL, (math.radians(90), 0, 0), 32)
    add_cylinder(c, r, "LauncherBarrel", 0.027, 0.31, (0.035, 0, 0.22), MAT_BLUE, (0, math.radians(90), 0), 32, 0.003)
    add_cylinder(c, r, "BarrelBore", 0.019, 0.012, (0.195, 0, 0.22), MAT_BLACK, (0, math.radians(90), 0), 32, 0)
    add_cylinder(c, r, "ProjectileSocket", 0.024, 0.025, (-0.126, 0, 0.22), MAT_DARK, (0, math.radians(90), 0), 32)
    add_box(c, r, "LauncherTrigger", (0.018, 0.055, 0.045), (-0.075, -0.055, 0.095), MAT_RED, 0.006)
    add_text(c, r, "LauncherLabel", "VLAB", (0.04, -0.029, 0.22), 0.018, MAT_LIGHT)


def build_projectile():
    c, r = new_asset("Projectile_Steel_Ball", "Projectile")
    add_sphere(c, r, "SteelBall", 0.02, (0, 0, 0.02), MAT_STEEL, 32, 16)
    r["default_mass_kg"] = 0.065


def build_friction_block():
    c, r = new_asset("Physics_Friction_Block", "Mechanics")
    add_box(c, r, "FrictionBlock", (0.16, 0.10, 0.075), (0, 0, 0.0375), MAT_WOOD, 0.008)
    add_box(c, r, "MassTray", (0.11, 0.075, 0.008), (0, 0, 0.079), MAT_STEEL, 0.003)
    add_torus(c, r, "ForceAttachmentEye", 0.014, 0.0035, (0.095, 0, 0.042), MAT_STEEL, (0, math.radians(90), 0))
    add_text(c, r, "MassLabel", "0.50 kg", (0, -0.052, 0.038), 0.016, MAT_DARK)
    r["default_mass_kg"] = 0.5


def build_air_track():
    c, r = new_asset("Physics_Air_Track", "AirTrack")
    add_box(c, r, "TrackSpine", (2.0, 0.15, 0.06), (0, 0, 0.14), MAT_ALUMINUM, 0.012)
    add_box(c, r, "GuideRailLeft", (2.0, 0.018, 0.035), (0, -0.066, 0.185), MAT_STEEL, 0.004)
    add_box(c, r, "GuideRailRight", (2.0, 0.018, 0.035), (0, 0.066, 0.185), MAT_STEEL, 0.004)
    for x in (-0.82, 0.82):
        add_box(c, r, f"TrackFoot_{'L' if x < 0 else 'R'}", (0.18, 0.36, 0.045), (x, 0, 0.0225), MAT_DARK, 0.012)
        add_rod_between(c, r, f"LevelingPost_{'L' if x < 0 else 'R'}", (x, -0.13, 0.045), (x, -0.13, 0.12), 0.008, MAT_STEEL)
    for index, x in enumerate((-0.65, -0.25, 0.25, 0.65), 1):
        add_box(c, r, f"PhotogateMount_{index}", (0.045, 0.19, 0.012), (x, 0, 0.195), MAT_BLUE, 0.003)
    r["movement_axis"] = "local X"


def build_glider(name, color_mat):
    c, r = new_asset(name, "AirTrack")
    add_box(c, r, "GliderBody", (0.18, 0.13, 0.055), (0, 0, 0.065), color_mat, 0.012)
    add_box(c, r, "TrackShoeLeft", (0.15, 0.018, 0.025), (0, -0.065, 0.032), MAT_DARK, 0.004)
    add_box(c, r, "TrackShoeRight", (0.15, 0.018, 0.025), (0, 0.065, 0.032), MAT_DARK, 0.004)
    add_box(c, r, "PhotogateFlag", (0.05, 0.004, 0.09), (0, 0, 0.137), MAT_BLACK, 0.001)
    add_cylinder(c, r, "FrontAttachment", 0.018, 0.02, (0.10, 0, 0.065), MAT_STEEL, (0, math.radians(90), 0), 24)
    add_cylinder(c, r, "RearAttachment", 0.018, 0.02, (-0.10, 0, 0.065), MAT_STEEL, (0, math.radians(90), 0), 24)
    r["default_mass_kg"] = 0.25


def build_collision_bumper():
    c, r = new_asset("Collision_Bumper", "AirTrack")
    add_cylinder(c, r, "BumperStem", 0.008, 0.035, (-0.018, 0, 0.03), MAT_STEEL, (0, math.radians(90), 0), 24)
    add_cylinder(c, r, "ElasticBumper", 0.028, 0.025, (0.012, 0, 0.03), MAT_RUBBER, (0, math.radians(90), 0), 32, 0.004)
    r["collision_mode"] = "elastic"


def build_inelastic_attachment():
    c, r = new_asset("Inelastic_Collision_Attachment", "AirTrack")
    add_box(c, r, "AttachmentPlate", (0.028, 0.065, 0.065), (0, 0, 0.04), MAT_DARK, 0.006)
    add_box(c, r, "HookPad", (0.012, 0.052, 0.052), (0.02, 0, 0.04), MAT_RED, 0.003)
    for y in (-0.017, 0, 0.017):
        add_rod_between(c, r, f"Latch_{y:+.3f}", (0.025, y, 0.025), (0.055, y, 0.055), 0.0025, MAT_STEEL, 16)
    r["collision_mode"] = "inelastic"


def export_assets():
    exported = []
    for asset_name, (collection, root, category) in ASSETS.items():
        bpy.ops.object.select_all(action="DESELECT")
        for obj in collection.objects:
            obj.select_set(True)
        output = os.path.join(EXPORT_ROOT, category, asset_name + ".fbx")
        bpy.ops.export_scene.fbx(
            filepath=output,
            use_selection=True,
            object_types={"EMPTY", "MESH"},
            axis_forward="-Z",
            axis_up="Y",
            apply_unit_scale=True,
            apply_scale_options="FBX_SCALE_ALL",
            use_mesh_modifiers=True,
            add_leaf_bones=False,
            bake_anim=False,
            path_mode="COPY",
            embed_textures=True,
        )
        unity_copy = os.path.join(UNITY_READY, asset_name + ".fbx")
        with open(output, "rb") as source, open(unity_copy, "wb") as destination:
            destination.write(source.read())
        exported.append({"name": asset_name, "category": category, "fbx": output})
    bpy.ops.object.select_all(action="DESELECT")
    return exported


ensure_directories()
for existing_object in list(bpy.data.objects):
    bpy.data.objects.remove(existing_object, do_unlink=True)
for existing_collection in list(bpy.data.collections):
    bpy.data.collections.remove(existing_collection)
scene = bpy.context.scene
scene.unit_settings.system = "METRIC"
scene.unit_settings.length_unit = "METERS"
scene.unit_settings.scale_length = 1.0
scene.render.engine = "BLENDER_EEVEE"

MAT_DARK = material("Lab_DarkPowderCoat", (0.035, 0.045, 0.055), 0.25, 0.30)
MAT_BLACK = material("Lab_BlackRubber", (0.012, 0.014, 0.016), 0.0, 0.65)
MAT_STEEL = material("Lab_BrushedSteel", (0.42, 0.46, 0.50), 0.85, 0.22)
MAT_ALUMINUM = material("Lab_AnodizedAluminum", (0.62, 0.66, 0.70), 0.75, 0.30)
MAT_BLUE = material("Lab_InstrumentBlue", (0.035, 0.18, 0.42), 0.25, 0.28)
MAT_RED = material("Lab_SafetyRed", (0.62, 0.025, 0.02), 0.15, 0.32)
MAT_YELLOW = material("Lab_SignalYellow", (0.92, 0.57, 0.02), 0.1, 0.35)
MAT_LIGHT = material("Lab_LabelWhite", (0.88, 0.90, 0.88), 0.0, 0.48)
MAT_IVORY = material("Lab_TimerIvory", (0.68, 0.70, 0.67), 0.05, 0.42)
MAT_DISPLAY = material("Lab_DisplayGlass", (0.012, 0.035, 0.028), 0.15, 0.18)
MAT_LED = material("Lab_LEDGreen", (0.10, 1.0, 0.25), 0.0, 0.22)
MAT_WOOD = material("Lab_SealedWood", (0.34, 0.16, 0.055), 0.0, 0.42)
MAT_RULER = material("Lab_RulerYellow", (0.85, 0.64, 0.12), 0.2, 0.36)
MAT_BRASS = material("Lab_Brass", (0.56, 0.34, 0.07), 0.8, 0.23)
MAT_RUBBER = material("Lab_ElasticRubber", (0.08, 0.085, 0.09), 0.0, 0.8)
MAT_GREEN = material("Lab_GliderGreen", (0.04, 0.38, 0.18), 0.18, 0.30)

ASSETS = {}
repair_retort_stand()
build_adjustable_clamp()
build_table()
build_ruler()
build_timer()
build_photogate()
build_flag()
build_force_meter()
build_pendulum_bob()
build_string_anchor()
build_spring()
build_mass_set()
build_launcher()
build_projectile()
build_friction_block()
build_air_track()
build_glider("Air_Track_Glider_A", MAT_BLUE)
build_glider("Air_Track_Glider_B", MAT_GREEN)
build_collision_bumper()
build_inelastic_attachment()

bpy.ops.wm.save_as_mainfile(filepath=WORKING_BLEND)
EXPORTED = export_assets()

result = {
    "working_blend": WORKING_BLEND,
    "asset_count": len(ASSETS),
    "assets": EXPORTED,
}
