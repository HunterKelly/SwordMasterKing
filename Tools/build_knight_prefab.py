"""Build the authored knight hierarchy using Unity's built-in primitive meshes.
Run from the repository root. Preserves prefab GUID and PlayerRig fileID for existing scenes.
No external packages required. Edits to the generated prefab should normally be made in Unity.
"""
from pathlib import Path
import re
import math
from itertools import product

root = Path('SwordKing/Assets/_SwordKing')
folder = root / 'Prefabs/Player'
path = folder / 'ArmoredKnight.prefab'
old = folder / 'Swordsman.prefab'
source = (path if path.exists() else old).read_text()
sections = re.split(r'(?=--- !u!)', source)
def template(kind):
    return next(s for s in sections if s.startswith('--- !u!' + str(kind) + ' '))
renderer = template(23)
controller = template(143)
rig_id = 7072597504312420402
controller_id = 3648755183412612744
animation_id = 900000
animation_guid = '85d7be92fb5b4dff914c0dca52e12bd4'
mat = [re.search(r'guid: (\w+)', (root / f'Art/Materials/Player_{i}.mat.meta').read_text())[1] for i in range(5)]
# A shared handmade bevel mesh softens the armor silhouette without external assets.
model_folder = root / 'Art/Models'
model_folder.mkdir(parents=True, exist_ok=True)
bevel_guid = '0948d9c6d25b4ee78a9202cd825a5ea4'
vertices = []
for axis in range(3):
    for signs in product((-1, 1), repeat=3):
        vertices.append(tuple(signs[k] * (.5 if k == axis else .40) for k in range(3)))
planes = []
for axis in range(3):
    for sign in (-1, 1):
        normal = tuple(sign if k == axis else 0 for k in range(3))
        planes.append((normal, .5))
for axis in range(3):
    for signs in product((-1, 1), repeat=2):
        normal = [0, 0, 0]
        other = [k for k in range(3) if k != axis]
        for k, sign in zip(other, signs): normal[k] = sign
        planes.append((tuple(normal), .90))
for signs in product((-1, 1), repeat=3): planes.append((signs, 1.30))
def cross(a,b): return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def unit(a): return tuple(x / math.sqrt(dot(a,a)) for x in a)
obj = ['o ArmorBevel\n'] + ['v '+' '.join(map(str,v))+'\n' for v in vertices]
faces = []
for normal, distance in planes:
    indices = [i for i,v in enumerate(vertices) if abs(dot(normal,v)-distance)<.00001]
    n = unit(normal)
    center = tuple(sum(vertices[i][k] for i in indices)/len(indices) for k in range(3))
    u = unit(cross(n, (0,1,0) if abs(n[1])<.9 else (1,0,0)))
    v = cross(n,u)
    def angle(i):
        delta = tuple(vertices[i][k]-center[k] for k in range(3))
        return math.atan2(dot(delta,v), dot(delta,u))
    indices.sort(key=angle)
    obj.append('vn '+' '.join(map(str,n))+'\n')
    faces.append(indices)
for ni, indices in enumerate(faces,1):
    for i in range(1,len(indices)-1):
        obj.append('f '+' '.join(f'{k+1}//{ni}' for k in (indices[0],indices[i],indices[i+1]))+'\n')
(model_folder/'ArmorBevel.obj').write_text(''.join(obj))
(model_folder/'ArmorBevel.obj.meta').write_text(f'''fileFormatVersion: 2
guid: {bevel_guid}
ModelImporter:
  serializedVersion: 22200
  internalIDToNameTable:
  - first:
      43: 4300000
    second: ArmorBevel
  externalObjects: {{}}
  materials:
    materialImportMode: 0
  meshes:
    globalScale: 1
    useFileUnits: 0
    importBlendShapes: 0
    addColliders: 0
    isReadable: 0
  tangentSpace:
    normalSmoothAngle: 0
    normalImportMode: 0
    tangentImportMode: 2
  importAnimation: 0
  fileIdsGeneration: 1
  userData:
  assetBundleName:
  assetBundleVariant:
''')
nodes = []
def joint(name, parent=None, pos=(0,0,0)):
    n = dict(name=name, parent=parent, pos=pos, scale=(1,1,1), children=[], id=1000+len(nodes)*10)
    if parent is not None: parent['children'].append(n)
    nodes.append(n)
    return n

def shape(name, parent, pos, scale, material=2, mesh=10202):
    n = joint(name, parent, pos)
    n.update(scale=scale, material=material, mesh=mesh)
    return n

player = joint('Armored Knight')
visual = joint('Visuals - roll and spin pivot', player, (0,.95,0))
hips = joint('Hips', visual, (0,.02,0))
shape('Armored waist', hips, (0,.02,0), (.46,.22,.30), 1)
shape('Gold belt', hips, (0,.08,.015), (.51,.065,.34), 3)
shape('Belt buckle', hips, (0,.08,.197), (.105,.09,.04), 3)
chest = joint('Chest', visual, (0,.17,0))
shape('Breastplate', chest, (0,.16,0), (.60,.44,.37))
shape('Raised breastplate', chest, (0,.19,.19), (.40,.30,.065))
shape('Tabard chest teal', chest, (0,.15,.231), (.22,.35,.025), 4)
shape('Gold crest vertical', chest, (0,.17,.251), (.028,.18,.016), 3)
shape('Gold crest horizontal', chest, (0,.20,.253), (.12,.025,.016), 3)
shape('Lower armor trim', chest, (0,-.065,0), (.62,.045,.39), 3)
shape('Gorget', chest, (0,.405,0), (.31,.10,.30), 1,10206)
head = joint('Head', chest, (0,.43,0))
shape('Helmet shell', head, (0,.19,0), (.39,.43,.36), 2,10207)
shape('Faceplate', head, (0,.155,.158), (.335,.28,.09))
shape('Black visor slit', head, (0,.218,.209), (.29,.035,.015), 0)
shape('Helmet brow', head, (0,.26,.20), (.36,.035,.05), 3)
shape('Nose guard', head, (0,.14,.22), (.035,.19,.035), 3)
for x in (-.09,.09):
    for y in (.095,.14):
        shape('Visor vent', head, (x,y,.208), (.052,.016,.013), 0)
shape('Helmet ridge', head, (0,.38,-.015), (.045,.075,.30), 3)
shape('Teal plume', head, (0,.425,-.06), (.065,.11,.29), 4)
shape('Plume tail', head, (0,.36,-.23), (.065,.19,.09), 4)

left_arm = joint('Left shoulder', visual, (-.40,.43,0))
shape('Left pauldron', left_arm, (0,-.015,0), (.30,.24,.36), 2,10207)
shape('Left pauldron trim', left_arm, (0,-.095,.015), (.31,.045,.35), 3)
shape('Left upper arm mail', left_arm, (0,-.21,0), (.17,.27,.19), 1)
left_elbow = joint('Left elbow', left_arm, (0,-.32,0))
shape('Left elbow plate', left_elbow, (0,0,.015), (.21,.16,.23),2,10207)
shape('Left vambrace', left_elbow, (0,-.14,0), (.18,.26,.20))
shape('Left wrist trim', left_elbow, (0,-.27,0), (.19,.04,.21),3)
shape('Left gauntlet', left_elbow, (0,-.335,.025), (.19,.15,.22),1)

# Keep the original sword pivot position and blade reach; only graphics change.
sword = joint('Sword shoulder - combat pivot', visual, (.40,.25,.05))
shape('Right pauldron', sword, (0,.12,-.025), (.30,.24,.36),2,10207)
shape('Right pauldron trim', sword, (0,.04,-.025), (.31,.045,.35),3)
shape('Right upper arm mail', sword, (0,0,.13), (.18,.19,.28),1)
right_elbow = joint('Right elbow', sword, (0,0,.29))
shape('Right elbow plate', right_elbow, (0,0,0), (.22,.22,.17),2,10207)
shape('Right vambrace', right_elbow, (0,0,.105), (.19,.20,.24))
shape('Right wrist trim', right_elbow, (0,0,.23), (.20,.21,.04),3)
shape('Right gauntlet', right_elbow, (0,0,.285), (.20,.19,.16),1)
weapon = joint('Sword - editable mesh pieces', sword)
shape('Leather grip', weapon, (0,0,.61), (.075,.085,.29),1)
for z in (.51,.56,.61,.66,.71):
    shape('Grip winding', weapon, (0,0,z), (.085,.095,.012),3)
shape('Pommel', weapon, (0,0,.435), (.13,.12,.09),3,10207)
shape('Crossguard', weapon, (0,0,.79), (.46,.095,.08),3)
shape('Blade', weapon, (0,0,1.31), (.14,.045,1.0))
shape('Blade fuller', weapon, (0,.025,1.31), (.035,.008,.88),1)
# A tapered tip is a custom mesh asset later if desired; this narrow point keeps built-in meshes editable.
shape('Blade point', weapon, (0,0,1.84), (.075,.04,.12))

legs=[]; shins=[]
for x, side in ((-.18,'Left'),(.18,'Right')):
    thigh = joint(side+' hip', visual, (x,-.01,0)); legs.append(thigh)
    shape(side+' upper leg mail', thigh, (0,-.18,0), (.22,.34,.24),1)
    shape(side+' thigh plate', thigh, (0,-.15,.11), (.23,.27,.08))
    shape(side+' thigh trim', thigh, (0,-.27,.11), (.24,.035,.095),3)
    shin = joint(side+' knee', thigh, (0,-.38,0)); shins.append(shin)
    shape(side+' knee plate', shin, (0,0,.09), (.25,.20,.20),2,10207)
    shape(side+' greave', shin, (0,-.22,0), (.215,.36,.25))
    shape(side+' greave ridge', shin, (0,-.21,.135), (.035,.29,.025),3)
    shape(side+' sabaton', shin, (0,-.445,.065), (.25,.14,.43),1)
    shape(side+' toe plate', shin, (0,-.42,.19), (.25,.085,.18))
front = joint('Tabard front hinge', visual, (0,.025,.20))
back = joint('Tabard back hinge', visual, (0,.025,-.20))
for pivot in (front,back):
    for x in (-.085,.085):
        shape('Split teal cloth',pivot,(x,-.255,0),(.16,.51,.027),4)
        shape('Gold hem',pivot,(x,-.50,0),(.16,.027,.032),3)


def vec(v):
    return '{'+', '.join(f'{k}: {x:g}' for k,x in zip('xyz',v))+'}'
def ref(i): return '{fileID: '+str(i)+'}'
def mono(i,guid,fields,klass):
    return f'''--- !u!114 &{i}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {ref(player['id'])}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: SwordKing.Runtime::SwordKing.{klass}
'''+''.join(f'  {k}: {v}\n' for k,v in fields.items())

out=['%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n']
for n in nodes:
    i=n['id']; components=[i+1]
    if 'mesh' in n: components += [i+2,i+3]
    if n is player: components += [controller_id,rig_id,animation_id]
    out.append(f'''--- !u!1 &{i}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
'''+''.join(f'  - component: {ref(c)}\n' for c in components)+f'''  m_Layer: 0
  m_Name: {n['name']}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{i+1}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {ref(i)}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {vec(n['pos'])}
  m_LocalScale: {vec(n['scale'])}
  m_ConstrainProportionsScale: 0
  m_Children:'''+ ('\n'+''.join(f'  - {ref(c["id"]+1)}\n' for c in n['children']) if n['children'] else ' []\n')+f'''  m_Father: {ref(n['parent']['id']+1 if n['parent'] else 0)}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
''')
    if 'mesh' in n:
        out.append(f'''--- !u!33 &{i+2}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {ref(i)}
  m_Mesh: {('{fileID: 4300000, guid: '+bevel_guid+', type: 3}') if n['mesh'] == 10202 and n['material'] in (1,2,3) else ('{fileID: '+str(n['mesh'])+', guid: 0000000000000000e000000000000000, type: 0}')}
''')
        r=re.sub(r'&\d+',f'&{i+3}',renderer,count=1)
        r=re.sub(r'm_GameObject: \{fileID: \d+\}',f'm_GameObject: {ref(i)}',r)
        r=re.sub(r'(m_Materials:\n  - )[^\n]+',r'\g<1>{fileID: 2100000, guid: '+mat[n['material']]+', type: 2}',r)
        out.append(r)
c=re.sub(r'&\d+',f'&{controller_id}',controller,count=1)
c=re.sub(r'm_GameObject: \{fileID: \d+\}',f'm_GameObject: {ref(player["id"])}',c)
out.append(c)
out.append(mono(rig_id,'baee79b2435c56c6993e0e128123f778',dict(controller=ref(controller_id),visualRoot=ref(visual['id']+1),swordPivot=ref(sword['id']+1),leftLeg=ref(legs[0]['id']+1),rightLeg=ref(legs[1]['id']+1)),'PlayerRig'))
refs=dict(chest=chest,head=head,leftArm=left_arm,leftForearm=left_elbow,rightForearm=right_elbow,leftShin=shins[0],rightShin=shins[1],frontTabard=front,backTabard=back)
fields={k:ref(v['id']+1) for k,v in refs.items()}
fields.update(movementAmount='1',attackAmount='1',poseResponse='18')
out.append(mono(animation_id,animation_guid,fields,'KnightAnimation'))
path.write_text(''.join(out))
if old.exists():
    old.with_suffix('.prefab.meta').rename(path.with_suffix('.prefab.meta'))
    old.unlink()
(root/'Scripts/Player/KnightAnimation.cs.meta').write_text(f'''fileFormatVersion: 2
guid: {animation_guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData:
  assetBundleName:
  assetBundleVariant:
''')
print(f'Built {path}: {len(nodes)} editable objects; {sum("mesh" in n for n in nodes)} mesh pieces.')
