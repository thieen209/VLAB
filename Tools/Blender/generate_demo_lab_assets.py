"""Original metric VLAB demo assets. Run through Blender MCP; preserves the user's scene.
Objects use mechanical origins, applied mesh transforms, simple shared materials.
"""
import bpy
import math
import os
from mathutils import Vector

ROOT = r'D:\VLAB\Web demo\VLAB'
OUT = os.path.join(ROOT, 'Assets', 'VLAB', 'DemoLabs', 'Art', 'Models')
SOURCE = os.path.join(ROOT, 'SourceAssets', 'DemoLabs')
os.makedirs(OUT, exist_ok=True)
os.makedirs(SOURCE, exist_ok=True)
original_scene = bpy.context.window.scene
previous_work = bpy.data.scenes.get('VLAB_Demo_Asset_Workshop')
if previous_work is not None and previous_work != original_scene:
    # Only the generated workshop from this tool is replaced; the user's scene is untouched.
    for generated_object in list(previous_work.objects):
        bpy.data.objects.remove(generated_object, do_unlink=True)
    bpy.data.scenes.remove(previous_work)
work = bpy.data.scenes.new('VLAB_Demo_Asset_Workshop')
bpy.context.window.scene = work
work.unit_settings.system = 'METRIC'
work.unit_settings.scale_length = 1
assets = []

def vec(p): return (p[0], -p[2], p[1])
def mat(name, rgb, metal=0):
    m = bpy.data.materials.new('Demo_' + name)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    n = m.node_tree.nodes.get('Principled BSDF')
    n.inputs['Base Color'].default_value = (*rgb, 1)
    n.inputs['Metallic'].default_value = metal
    n.inputs['Roughness'].default_value = .32 if metal else .46
    return m

ivory = mat('Ivory', (.82,.85,.82))
navy = mat('Navy', (.035,.075,.10))
metal = mat('Metal', (.48,.54,.58), .75)
black = mat('Graphite', (.035,.043,.052))
teal = mat('Teal', (.055,.43,.37))
glass = mat('Glass', (.5,.73,.76), .15)
green = mat('Lens', (.10,.65,.22))
red = mat('Red', (.66,.075,.045))
gold = mat('Gold', (.72,.49,.17), .4)
brown = mat('Brown', (.25,.10,.035))

def root(name):
    obj = bpy.data.objects.new(name, None)
    work.collection.objects.link(obj)
    assets.append(obj)
    return obj

def finish(obj, name, parent, material, bevel=0):
    obj.name = name
    obj.data.materials.append(material)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    if bevel:
        mod = obj.modifiers.new('Manufactured edge', 'BEVEL')
        mod.width = bevel
        mod.segments = 3
        bpy.ops.object.modifier_apply(modifier=mod.name)
    for p in obj.data.polygons: p.use_smooth = len(p.vertices) <= 4 and obj.type == 'MESH'
    mod = obj.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
    bpy.ops.object.modifier_apply(modifier=mod.name)
    if parent:
        world = obj.matrix_world.copy()
        obj.parent = parent
        obj.matrix_world = world
    return obj

def box(name, pos, size, material, parent, bevel=.006):
    bpy.ops.mesh.primitive_cube_add(size=1, location=vec(pos))
    obj = bpy.context.object
    obj.dimensions = (size[0], size[2], size[1])
    return finish(obj, name, parent, material, bevel)

def cyl(name, pos, radius, depth, material, parent, axis=(0,1,0), vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=vec(pos))
    obj = bpy.context.object
    obj.rotation_euler = Vector((0,0,1)).rotation_difference(Vector(vec(axis))).to_euler()
    return finish(obj, name, parent, material, .002)

def rod(name, a, b, radius, material, parent):
    delta = Vector(b) - Vector(a)
    return cyl(name, (Vector(a)+Vector(b))/2, radius, delta.length, material, parent, delta.normalized(), 20)

try:
    mic = root('BIO_Microscope')
    box('Base', (0,.04,0), (.43,.08,.52), ivory, mic, .035)
    for x in [-.15,.15]:
        for z in [-.17,.17]: cyl('RubberFoot', (x,.008,z), .035,.016,black,mic)
    box('ArmFoot',(0,.13,.16),(.17,.18,.18),ivory,mic,.026)
    rod('Arm',(0,.18,.17),(0,.58,.12),.065,ivory,mic)
    rod('HeadSupport',(0,.58,.12),(0,.61,-.05),.055,ivory,mic)
    stage=box('Stage',(0,.31,-.04),(.34,.023,.29),black,mic,.008)
    cyl('Condenser',(0,.265,-.08),.063,.065,metal,mic)
    cyl('Illuminator',(0,.09,-.08),.058,.035,black,mic)
    cyl('LightGlass',(0,.111,-.08),.043,.006,glass,mic)
    for x, suffix in [(-.125,'L'),(.125,'R')]:
        clip=box('StageClip_'+suffix,(x,.340,-.018),(.023,.011,.19),metal,mic,.004)
        # Pivot at the fastening pin, not at the middle of the spring clip.
        pivot=Vector(vec((x,.340,.072)))
        clip.data.transform(__import__('mathutils').Matrix.Translation(clip.location-pivot))
        clip.location=pivot
        cyl('ClipPin_'+suffix,(x,.340,.072),.018,.022,metal,mic)
    nose=cyl('Nosepiece',(0,.53,-.06),.085,.027,black,mic)
    cyl('Objective_10x',(0,.463,-.12),.021,.102,metal,nose)
    cyl('ObjectiveRing10',(0,.438,-.12),.022,.013,gold,nose)
    cyl('Objective_40x',(0,.443,0),.024,.142,metal,nose)
    cyl('ObjectiveRing40',(0,.421,0),.025,.013,teal,nose)
    rod('Head',(0,.595,-.045),(0,.76,-.15),.047,ivory,mic)
    rod('Eyepiece',(0,.755,-.147),(0,.835,-.202),.031,black,mic)
    rod('EyepieceGlass',(0,.833,-.2),(0,.837,-.203),.024,glass,mic)
    cyl('CoarseFocusKnob',(.13,.38,.15),.05,.065,black,mic,(1,0,0))
    cyl('FineFocusKnob',(.172,.38,.15),.027,.035,metal,mic,(1,0,0))
    for i in range(20):
        angle=i*math.tau/20
        box('CoarseGrip',(.135,.38+math.sin(angle)*.049,.15+math.cos(angle)*.049),(.057,.005,.005),navy,mic,.001)
    cyl('LightControl',(.20,.066,-.055),.022,.024,teal,mic,(1,0,0))
    anchor=bpy.data.objects.new('SlideAnchor',None)
    work.collection.objects.link(anchor)
    anchor.parent=mic
    anchor.location=vec((0,.322,-.12))

    board=root('ENG_Breadboard')
    box('Backing',(0,.022,0),(.94,.044,.52),navy,board,.018)
    box('ContactDeck',(0,.05,0),(.9,.022,.47),ivory,board,.01)
    box('Channel',(0,.063,0),(.76,.003,.025),black,board,.001)
    # Geometry only: no physics or GameObjects per electrical hole after joining.
    holes=[]
    for x in range(24):
        for z in [-.16,-.12,-.08,.08,.12,.16]:
            holes.append(box('ContactHole',(-.368+x*.032,.0625,z),(.012,.0015,.012),black,board,.001))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in holes: obj.select_set(True)
    bpy.context.view_layer.objects.active=holes[0]
    bpy.ops.object.join()
    bpy.context.object.name='ContactPattern'
    for z,m in [(-.215,red),(.215,navy)]: box('RailStripe',(0,.063,z),(.82,.002,.008),m,board,.001)

    psu=root('ENG_PowerSupply')
    box('Housing',(0,.155,0),(.47,.31,.28),ivory,psu,.026)
    box('Face',(0,.155,-.147),(.43,.265,.013),navy,psu,.012)
    box('Display',(0,.205,-.158),(.31,.10,.008),black,psu,.005)
    for x,m in [(-.13,red),(.13,black)]:
        cyl('Terminal', (x,.086,-.177), .024,.035,m,psu,(0,0,1))
        cyl('SocketInsert',(x,.086,-.198),.01,.008,metal,psu,(0,0,1))
    cyl('PowerSwitch',(0,.085,-.175),.033,.034,teal,psu,(0,0,1))
    for i in range(8): box('Vent',(.12,.317,-.085+i*.023),(.13,.002,.008),black,psu,.002)

    for value,bands in [(100,[brown,black,brown]),(220,[red,red,brown]),(1000,[brown,black,red])]:
        resistor=root('ENG_Resistor'+str(value))
        cyl('Body',(0,.025,0),.027,.16,ivory,resistor,(1,0,0))
        rod('LeadA',(-.16,.018,0),(-.08,.025,0),.005,metal,resistor)
        rod('LeadB',(.08,.025,0),(.16,.018,0),.005,metal,resistor)
        for i,m in enumerate(bands): cyl('Band'+str(i),(-.047+i*.033,.025,0),.0278,.012,m,resistor,(1,0,0))
        cyl('Tolerance',(.061,.025,0),.0278,.009,gold,resistor,(1,0,0))
    led=root('ENG_LED')
    cyl('Lens',(0,.066,0),.039,.075,green,led)
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,radius=.039,location=vec((0,.103,0)))
    finish(bpy.context.object,'Dome',led,green)
    cyl('Rim',(0,.032,0),.044,.012,green,led)
    rod('Anode',(-.018,.034,0),(-.018,-.006,0),.005,metal,led)
    rod('Cathode',(.018,.034,0),(.018,.003,0),.005,metal,led)

    connector=root('ENG_WireConnector')
    cyl('InsulatedGrip',(0,.014,0),.031,.080,teal,connector,(0,0,1),20)
    cyl('StrainRelief',(0,.014,.056),.019,.035,black,connector,(0,0,1),20)
    cyl('ContactPin',(0,.014,-.060),.009,.048,metal,connector,(0,0,1),16)
    for z in [-.022,0,.022]: cyl('GripRing',(0,.014,z),.034,.006,teal,connector,(0,0,1),20)

    socket=root('ENG_ConnectionSocket')
    cyl('Insulator',(0,.015,0),.035,.030,navy,socket,vertices=24)
    cyl('MetalRim',(0,.034,0),.026,.012,metal,socket,vertices=24)
    cyl('Receptacle',(0,.041,0),.013,.003,black,socket,vertices=20)

    slide=root('BIO_Slide')
    box('Slide',(0,.008,0),(.29,.009,.105),glass,slide,.003)
    box('Label',(-.104,.014,0),(.066,.001,.095),ivory,slide,.001)
    cover=root('BIO_Coverslip')
    box('Coverslip',(0,.007,0),(.097,.006,.085),glass,cover,.002)
    drop=root('BIO_Dropper')
    rod('Pipette',(-.10,.025,0),(.08,.025,0),.012,glass,drop)
    rod('Tip',(-.14,.025,0),(-.10,.025,0),.005,glass,drop)
    cyl('Bulb',(.11,.025,0),.024,.065,teal,drop,(1,0,0))
    tray=root('Lab_Tray')
    box('TrayBase',(0,.018,0),(.65,.025,.43),navy,tray,.012)
    for x in [-.32,.32]: box('TrayRim',(x,.04,0),(.018,.05,.43),ivory,tray)
    for z in [-.208,.208]: box('TrayRim',(0,.04,z),(.65,.05,.018),ivory,tray)

    def children(obj):
        yield obj
        for c in obj.children: yield from children(c)
    report=[]
    for asset in assets:
        bpy.ops.object.select_all(action='DESELECT')
        for obj in children(asset): obj.select_set(True)
        bpy.context.view_layer.objects.active=asset
        path=os.path.join(OUT,asset.name+'.fbx')
        bpy.ops.export_scene.fbx(filepath=path,use_selection=True,apply_unit_scale=True,
            object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',bake_anim=False,
            add_leaf_bones=False,use_mesh_modifiers=True)
        report.append({'name':asset.name,'meshes':sum(o.type=='MESH' for o in children(asset))})
    bpy.data.libraries.write(os.path.join(SOURCE,'VLAB_DemoAssets.blend'),{work},fake_user=True)
    result={'exports':report,'source':os.path.join(SOURCE,'VLAB_DemoAssets.blend')}
finally:
    bpy.context.window.scene=original_scene
