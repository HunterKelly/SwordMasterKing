"""Author a plate-armored knight, joint hierarchy, and deterministic mesh source data.
Run from the repository root. Geometry uses a versioned binary format read directly
by KnightMeshSource: Unity model-import subasset IDs are never guessed.
"""
from pathlib import Path
import math, struct, hashlib, re
from collections import defaultdict

ROOT=Path('SwordKing/Assets/_SwordKing')
OUT=ROOT/'Art/Models/Knight'
PREFAB=ROOT/'Prefabs/Player/ArmoredKnight.prefab'
TAU=math.pi*2

def guid(name): return hashlib.md5(('SwordKing.Knight.v2.'+name).encode()).hexdigest()
def add(a,b): return tuple(x+y for x,y in zip(a,b))
def sub(a,b): return tuple(x-y for x,y in zip(a,b))
def mul(a,s): return tuple(x*s for x in a)
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def cross(a,b): return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def unit(a):
 d=math.sqrt(dot(a,a)); return mul(a,1/d) if d>1e-12 else (0,1,0)

class Mesh:
 def __init__(self): self.v=[]; self.f=[]; self.uv=[]
 def vertex(self,p,uv=(0,0)):
  self.v.append(p);self.uv.append(uv);return len(self.v)-1
 def tri(self,a,b,c):
  if dot(cross(sub(self.v[b],self.v[a]),sub(self.v[c],self.v[a])),cross(sub(self.v[b],self.v[a]),sub(self.v[c],self.v[a])))>1e-18: self.f.append((a,b,c))
 def quad(self,a,b,c,d): self.tri(a,b,c);self.tri(a,c,d)
 def normals(self):
  n=[[0.,0.,0.] for _ in self.v]
  for a,b,c in self.f:
   fn=cross(sub(self.v[b],self.v[a]),sub(self.v[c],self.v[a]))
   for i in (a,b,c):
    for k in range(3): n[i][k]+=fn[k]
  return [unit(x) for x in n]

NODES=[]; MESHES={}
def joint(name,parent=None,p=(0,0,0)):
 n=dict(name=name,parent=parent,pos=p,children=[],id=1000+10*len(NODES))
 if parent: parent['children'].append(n)
 NODES.append(n);return n

def mesh(bone,material):
 key=(bone['name'],material)
 if key not in MESHES: MESHES[key]=Mesh()
 return MESHES[key]

# Horizontal cross-sections: y, half-width, half-depth, front/back offset.
def loft(m,profiles,segments=40,theta=(0,TAU),center=(0,0,0),keel=0,caps=True):
 rings=[]
 for j,(y,rx,rz,zc) in enumerate(profiles):
  ring=[]
  for i in range(segments+1):
   t=theta[0]+(theta[1]-theta[0])*i/segments
   p=(math.sin(t)*rx,y,math.cos(t)*rz+zc+keel*max(0,math.cos(t))**16)
   ring.append(m.vertex(add(p,center),(i/segments,j/max(1,len(profiles)-1))))
  rings.append(ring)
 for a,b in zip(rings,rings[1:]):
  for i in range(segments): m.quad(a[i],a[i+1],b[i+1],b[i])
 if caps and abs(theta[1]-theta[0]-TAU)<.001:
  for r,j,up in ((rings[0],0,False),(rings[-1],-1,True)):
   c=m.vertex(add((0,profiles[j][0],profiles[j][3]),center))
   for i in range(segments):
    if up:m.tri(c,r[i],r[i+1])
    else:m.tri(c,r[i+1],r[i])
 return m

def ellipsoid(m,c,s,segments=24,rings=12):
 profiles=[]
 for j in range(rings+1):
  p=math.pi*j/rings
  profiles.append((-math.cos(p)*s[1],max(.0001,math.sin(p))*s[0],max(.0001,math.sin(p))*s[2],0))
 return loft(m,profiles,segments,center=c)

def tube(m,path,radius=.003,sides=8,closed=False):
 if closed:path=path+[path[0]]
 rings=[]
 for i,p in enumerate(path):
  tangent=unit(sub(path[min(i+1,len(path)-1)],path[max(0,i-1)]))
  u=unit(cross(tangent,(0,1,0) if abs(tangent[1])<.95 else (1,0,0)))
  v=cross(tangent,u)
  rings.append([m.vertex(add(p,mul(add(mul(u,math.cos(TAU*k/sides)),mul(v,math.sin(TAU*k/sides))),radius)),(k/sides,i/max(1,len(path)-1))) for k in range(sides+1)])
 for a,b in zip(rings,rings[1:]):
  for k in range(sides):m.quad(a[k],a[k+1],b[k+1],b[k])

def rim(m,y,rx,rz,zc=0,rad=.003,theta=(0,TAU),center=(0,0,0),keel=0):
 path=[]
 for k in range(49):
  a=theta[0]+(theta[1]-theta[0])*k/48
  path.append(add((math.sin(a)*rx,y,math.cos(a)*rz+zc+keel*max(0,math.cos(a))**16),center))
 tube(m,path,rad,8)

def rivet(m,p,r=.004):ellipsoid(m,p,(r,r,r*.7),8,4)

def ringmail(m,center,rx,rz,rows,height,cols=28):
 # Small linked rings placed on the garment surface, with alternating row offsets.
 for row in range(rows):
  y=center[1]+height*(row/max(1,rows-1)-.5)
  for col in range(cols):
   a=TAU*(col+.5*(row%2))/cols
   pos=add(center,(rx*math.sin(a),y-center[1],rz*math.cos(a)))
   tangent=(math.cos(a),0,-math.sin(a))
   path=[add(pos,add(mul(tangent,math.cos(TAU*k/8)*.006),(0,math.sin(TAU*k/8)*.008,0))) for k in range(9)]
   tube(m,path,.0012,4)

PALETTE={
 'Steel':((.63,.67,.72),.82,.72),
 'Edge':((.81,.84,.88),.85,.78),
 'DarkSteel':((.13,.16,.20),.72,.53),
 'Mail':((.21,.23,.26),.65,.38),
 'Gold':((.50,.34,.13),.78,.62),
 'Leather':((.055,.035,.025),0,.24),
 'Blue':((.025,.065,.16),0,.32),
 'Crimson':((.32,.008,.022),0,.36),
}

def author():
 NODES.clear();MESHES.clear()
 player=joint('Armored Knight')
 visual=joint('Visuals - roll and spin pivot',player,(0,.95,0))
 hips=joint('Pelvis',visual,(0,.025,0))
 chest=joint('Chest',visual,(0,.18,0))
 steel=mesh(chest,'Steel'); edge=mesh(chest,'Edge'); dark=mesh(chest,'DarkSteel'); gold=mesh(chest,'Gold')
 loft(dark,[(-.12,.16,.108,0),(.04,.20,.13,0),(.27,.27,.14,0),(.40,.17,.10,0)])
 loft(steel,[(-.12,.162,.12,0),(-.055,.18,.13,0),(.06,.225,.145,0),(.22,.28,.163,0),(.34,.255,.137,0),(.415,.155,.097,0)],keel=.026)
 rim(edge,-.115,.164,.123,rad=.004,keel=.026)
 rim(edge,.413,.156,.100,rad=.004,keel=.026)
 # Raised central keel and restrained embossed fluting across the breastplate.
 tube(edge,[(0,-.10,.149),(0,.02,.18),(0,.18,.193),(0,.30,.174),(0,.40,.128)],.003)
 for sign in (-1,1):
  for offset in (0,.035,.07):
   tube(edge,[(sign*(.035+offset),-.08,.144),(sign*(.08+offset),.04,.157),(sign*(.14+offset),.18,.151),(sign*(.16+offset*.7),.29,.128)],.0018,6)
  # Ornate but geometric collar chain and rosette fastenings.
  for i in range(12):
   t=i/11;x=sign*(.05+.17*t);y=.37-.05*math.sin(t*math.pi/2);z=.115+.035*math.sin(t*math.pi)
   ellipsoid(gold,(x,y,z),(.009,.013,.003),8,4)
  ellipsoid(gold,(sign*.22,.32,.105),(.027,.027,.007),16,6)
  for i in range(8):
   a=TAU*i/8;tube(edge,[(sign*.22,.32,.115),(sign*.22+.021*math.cos(a),.32+.021*math.sin(a),.117)],.002,6)
 # Overlapping articulated waist lames.
 for i in range(4):
  y=-.15-i*.039;rx=.17+i*.015;rz=.12+i*.008
  loft(steel,[(y-.035,rx+.009,rz+.006,0),(y+.015,rx,rz,0)],32)
  rim(edge,y-.034,rx+.01,rz+.007,rad=.0025)
 # Raised neck protection, not a long exposed cylinder.
 loft(steel,[(.39,.15,.102,0),(.44,.12,.096,0),(.49,.095,.082,0)],36)
 for y,rx,rz in ((.40,.145,.106),(.445,.118,.097),(.485,.098,.086)):rim(edge,y,rx,rz,rad=.0028)
 head=joint('Head',chest,(0,.49,0))
 h=mesh(head,'Steel');he=mesh(head,'Edge');hd=mesh(head,'DarkSteel');hg=mesh(head,'Gold')
 loft(hd,[(.018,.093,.092,.004),(.13,.106,.102,.004),(.24,.10,.095,0)],36)
 # Domed helmet and cheek shell, with real gaps between the visor bars.
 loft(h,[(.195,.119,.111,.006),(.245,.115,.106,0),(.29,.085,.079,-.008),(.315,.035,.032,-.01),(.32,.002,.002,-.01)],48)
 loft(h,[(.012,.088,.092,.005),(.04,.111,.112,.008),(.19,.119,.111,.006)],40,theta=(1.18,TAU-1.18),caps=False)
 loft(h,[(.005,.085,.094,.008),(.035,.111,.116,.012),(.045,.116,.12,.013)],40)
 rim(he,.195,.12,.113,.006,.004)
 rim(he,.035,.112,.118,.012,.003)
 for y in (.060,.092,.124,.156):
  loft(h,[(y-.006,.119,.125,.012),(y+.007,.12,.126,.012)],32,theta=(-1.19,1.19),caps=False,keel=.005)
  rim(he,y+.007,.12,.126,.012,.0018,theta=(-1.19,1.19),keel=.005)
 tube(he,[(0,.044,.143),(0,.08,.147),(0,.12,.147),(0,.167,.144)],.006,8)
 tube(he,[(0,.20,.12),(0,.265,.099),(0,.321,-.006),(0,.29,-.08)],.004)
 for sign in (-1,1):
  rivet(hg,(sign*.114,.181,.035),.008)
  for y in (.065,.10,.135):rivet(hg,(sign*.111,y,.056),.003)
 # Full plate arm assemblies. Right arm is fitted to the sword grip with visual IK.
 arms=[];elbows=[]
 for sign,side in ((-1,'Left'),(1,'Right')):
  arm=joint(side+' shoulder',chest,(sign*.29,.33,-.005));arms.append(arm)
  a=mesh(arm,'Steel');ae=mesh(arm,'Edge');ad=mesh(arm,'DarkSteel');ag=mesh(arm,'Gold')
  ellipsoid(ad,(sign*.015,-.08,0),(.091,.16,.089))
  # Four overlapping curved pauldron lames, each with rolled edges and rivets.
  for i in range(4):
   y=.055-i*.047;rx=.142-i*.010;rz=.143-i*.010
   profile=[(y-.057,rx,rz,0),(y-.012,rx+.009,rz+.009,0),(y+.027,rx*.76,rz*.80,0)]
   if i==0: profile += [(y+.063,rx*.45,rz*.47,0),(y+.076,.006,.006,0)]
   loft(a,profile,40)
   rim(ae,y-.055,rx,rz,rad=.0024)
   for zsign in (-1,1):rivet(ag,(sign*rx*.78,y-.025,zsign*rz*.60),.0035)
  ringmail(mesh(arm,'Mail'),(0,-.18,0),.078,.077,5,.12,22)
  loft(a,[(-.28,.064,.065,0),(-.21,.072,.075,0),(-.11,.079,.077,0)],32)
  rim(ae,-.274,.065,.067,rad=.003)
  elbow=joint(side+' elbow',arm,(0,-.285,0));elbows.append(elbow)
  f=mesh(elbow,'Steel');fe=mesh(elbow,'Edge');fd=mesh(elbow,'DarkSteel');fg=mesh(elbow,'Gold')
  ellipsoid(fd,(0,.015,0),(.067,.065,.065),20,10)
  ellipsoid(f,(0,.005,.035),(.078,.067,.066),24,10)
  # Winged couter on the outside of the elbow.
  ellipsoid(f,(sign*.068,0,-.005),(.052,.065,.025),20,10)
  loft(f,[(-.255,.046,.048,.006),(-.20,.056,.061,0),(-.08,.065,.067,0),(-.03,.067,.066,0)],32,keel=.006)
  rim(fe,-.249,.047,.05,.006,.0028)
  rim(fe,-.035,.067,.068,rad=.0028)
  for sx in (-1,1):
   tube(fe,[(sx*.024,-.24,.049),(sx*.034,-.17,.061),(sx*.041,-.075,.061)],.0015,6)
  for y in (-.22,-.075):rivet(fg,(sign*.057,y,.018),.003)
  # Metacarpal plates and individually curved armored fingers.
  ellipsoid(fd,(0,-.29,.008),(.049,.068,.037),20,10)
  ellipsoid(f,(0,-.28,.034),(.052,.047,.023),20,10)
  for k in range(4):
   x=(k-1.5)*.023
   path=[(x,-.30,.036),(x,-.33,.045),(x,-.35,.022),(x,-.345,-.003)]
   tube(f,path,.0105,8)
   for y,z in ((-.31,.045),(-.337,.039)):ellipsoid(fe,(x,y,z),(.011,.008,.008),8,4)
  tube(f,[(sign*.047,-.272,.022),(sign*.065,-.30,.035),(sign*.049,-.32,.041)],.013,10)
 # Blue arming coat visible only between the plate sections.
 loft(mesh(hips,'Blue'),[(-.19,.19,.12,0),(-.08,.184,.114,0),(.03,.166,.10,0)],36)
 belt=mesh(hips,'Leather');bg=mesh(hips,'Gold')
 loft(belt,[(.015,.172,.122,0),(.075,.173,.122,0)],36)
 for y in (.02,.07):rim(bg,y,.175,.124,rad=.0025)
 # Buckle is a rectangular loop with an actual open center.
 tube(bg,[(-.035,.015,.132),(.035,.015,.132),(.035,.077,.132),(-.035,.077,.132),(-.035,.015,.132)],.004,8)
 tube(bg,[(0,.017,.135),(0,.073,.135)],.0025)
 for side in (-1,1):
  for i in range(5):rivet(bg,(side*(.055+i*.021),.045,.12*math.sqrt(max(0,1-((.055+i*.021)/.18)**2))+.006),.003)
 # Tassets, cuisses, poleyns, greaves and pointed articulated sabatons.
 thighs=[];shins=[];feet=[]
 for sign,side in ((-1,'Left'),(1,'Right')):
  thigh=joint(side+' hip',visual,(sign*.13,.015,0));thighs.append(thigh)
  t=mesh(thigh,'Steel');te=mesh(thigh,'Edge');td=mesh(thigh,'DarkSteel');tg=mesh(thigh,'Gold')
  loft(td,[(-.40,.066,.066,0),(-.18,.086,.083,0),(-.02,.104,.094,0)],32)
  loft(t,[(-.36,.068,.073,.006),(-.20,.087,.084,.003),(-.07,.098,.094,0)],36,keel=.007)
  for i in range(4):
   y=-.07-i*.036
   loft(t,[(y-.045,.108,.114,.012),(y+.012,.105,.108,.01)],24,theta=(-1.25,1.25),caps=False)
   rim(te,y-.044,.109,.115,.012,.0022,theta=(-1.25,1.25))
   for sx in (-1,1):rivet(tg,(sx*.092,y-.024,.079),.003)
  tube(te,[(0,-.17,.10),(0,-.26,.091),(0,-.35,.082)],.002)
  shin=joint(side+' knee',thigh,(0,-.415,0));shins.append(shin)
  s=mesh(shin,'Steel');se=mesh(shin,'Edge');sd=mesh(shin,'DarkSteel');sg=mesh(shin,'Gold')
  ellipsoid(sd,(0,0,0),(.066,.066,.066),24,10)
  ellipsoid(s,(0,.005,.049),(.079,.067,.054),28,12)
  ellipsoid(s,(sign*.073,.005,.015),(.048,.068,.025),20,10)
  # Pointed fluted shin plates follow the calf rather than straight cylinders.
  loft(s,[(-.405,.045,.051,.006),(-.32,.052,.060,0),(-.19,.067,.079,-.009),(-.075,.065,.065,0)],36,keel=.010)
  for y,rx,rz,zc in ((-.40,.046,.053,.006),(-.077,.066,.067,0)):rim(se,y,rx,rz,zc,.003)
  for offset in (-.025,0,.025):
   tube(se,[(offset*.6,-.39,.067),(offset*.8,-.29,.078),(offset,-.18,.082),(offset*.9,-.09,.073)],.0018,6)
  rivet(sg,(sign*.065,-.09,.018),.003)
  # Foot armor: overlapping convex plates narrow toward the toe.
  foot=joint(side+' ankle',shin,(0,-.405,.015));feet.append(foot)
  boot=mesh(foot,'DarkSteel');bs=mesh(foot,'Steel');be=mesh(foot,'Edge')
  ellipsoid(boot,(0,-.038,.078),(.073,.055,.163),32,12)
  for i in range(6):
   z=-.025+i*.042;rx=.071-i*.004
   ellipsoid(bs,(0,-.015,z), (rx,.043-i*.003,.045),24,10)
   tube(be,[(-rx*.93,-.014,z+.014),(-rx*.65,.015-i*.002,z+.026),(0,.024-i*.003,z+.031),(rx*.65,.015-i*.002,z+.026),(rx*.93,-.014,z+.014)],.0017,6)
 # Mail skirt with readable linked rings in the gaps between the tassets.
 ringmail(mesh(hips,'Mail'),(0,-.20,0),.185,.115,7,.19,48)
 # Sword and handle. A real tapered diamond cross-section, not a cuboid.
 sword=joint('Sword shoulder - combat pivot',visual,(.28,.36,.04))
 grip=joint('Sword grip target',sword,(0,0,.50))
 sm=mesh(sword,'Steel');sed=mesh(sword,'Edge');sl=mesh(sword,'Leather');sg=mesh(sword,'Gold');sd=mesh(sword,'DarkSteel')
 # Blade is built face by face to keep its sharp longitudinal facets.
 sections=[(.72,.052,.012),(.90,.050,.014),(1.50,.036,.010),(1.72,.026,.007),(1.91,.0005,.0005)]
 for side in range(4):
  strip=[]
  for z,w,d in sections:
   points=[(-w,0,z),(0,d,z),(w,0,z),(0,-d,z)]
   strip.append((sed.vertex(points[side]),sed.vertex(points[(side+1)%4])))
  for a,b in zip(strip,strip[1:]):sed.quad(a[0],b[0],b[1],a[1])
 tube(sd,[(0,.014,.84),(0,.015,1.0),(0,.012,1.45)],.003,8)
 # Quillons flare slightly forward at the ends.
 tube(sg,[(-.245,0,.79),(-.21,0,.755),(-.11,0,.726),(0,0,.72),(.11,0,.726),(.21,0,.755),(.245,0,.79)],.014,12)
 ellipsoid(sg,(0,0,.72),(.051,.028,.038),20,10)
 # Rotate a loft to create a z-aligned grip.
 temp=Mesh();loft(temp,[(-.135,.022,.022,0),(.135,.022,.022,0)],20)
 for p in temp.v:sl.vertex((p[0],p[2],p[1]+.55))
 sl.f.extend(temp.f)
 for i in range(12):
  z=.425+i*.022
  tube(sd,[(.024*math.cos(TAU*k/24),.024*math.sin(TAU*k/24),z+.018*k/24) for k in range(25)],.0022,6)
 ellipsoid(sg,(0,0,.386),(.041,.027,.045),24,12)
 for z in (.41,.689):
  tube(sg,[(.026*math.cos(TAU*k/32),.026*math.sin(TAU*k/32),z) for k in range(33)],.003,8)
 # A long crimson cape made of three connected sections, with baked pleats.
 cape=[]
 parent=chest
 for section in range(3):
  p=(0,.40,-.16) if section==0 else (0,-.45,-.135)
  bone=joint(('Cape shoulder','Cape middle','Cape hem')[section],parent,p);cape.append(bone);parent=bone
  c=mesh(bone,'Crimson');trim=mesh(bone,'Gold')
  rows=[]
  for j in range(11):
   t=j/10;g=(section+t)/3;width=(.30+.29*g)*.93
   row=[]
   for i in range(33):
    u=i/32;x=(u*2-1)*width
    # Accumulated z positions match precisely across segment boundaries.
    z=-.135*t+.045*math.cos((u-.5)*10*math.pi)*(.3+g)+.08*(u*2-1)**2
    row.append(c.vertex((x,-.45*t,z),(u,g)))
   rows.append(row)
  for a,b in zip(rows,rows[1:]):
   for i in range(32):c.quad(a[i],a[i+1],b[i+1],b[i])
  if section==2:
   tube(trim,[c.v[i] for i in rows[-1]],.0025,6)
 # Cloak fastening at the shoulders, with short chains.
 for sign in (-1,1):
  ellipsoid(gold,(sign*.225,.34,.092),(.021,.023,.009),16,8)
  for i in range(9):
   t=i/8;x=sign*(.225-.225*t);y=.34-.08*math.sin(t*math.pi/2);z=.11+.02*t
   ellipsoid(gold,(x,y,z),(.009,.012,.003),8,4)
 for key in [key for key,value in MESHES.items() if not value.f]: del MESHES[key]
 return dict(player=player,visual=visual,pelvis=hips,leftFoot=feet[0],rightFoot=feet[1],chest=chest,head=head,leftArm=arms[0],rightArm=arms[1],leftForearm=elbows[0],rightForearm=elbows[1],leftLeg=thighs[0],rightLeg=thighs[1],leftShin=shins[0],rightShin=shins[1],sword=sword,weaponGrip=grip,capeTop=cape[0],capeMid=cape[1],capeHem=cape[2])

def build():
 refs=author();OUT.mkdir(parents=True,exist_ok=True)
 for stale in OUT.glob('*.bytes*'): stale.unlink()
 template=PREFAB.read_text();blocks=re.split(r'(?=--- !u!)',template)
 renderer=next(x for x in blocks if x.startswith('--- !u!23 '))
 controller=next(x for x in blocks if x.startswith('--- !u!143 '))
 materials={}
 mat_template=(ROOT/'Art/Materials/Player_2.mat').read_text()
 for name,(rgb,metal,smooth) in PALETTE.items():
  path=ROOT/f'Art/Materials/Knight_{name}.mat';mg=guid('mat/'+name);materials[name]=mg
  s=mat_template.replace('m_Name: Player_2',f'm_Name: Knight_{name}')
  for prop,value in (('_Metallic',metal),('_Smoothness',smooth),('_Cull',0 if name=='Crimson' else 2)):
   s=re.sub(r'(- '+prop+r': )[^\n]+',lambda m:m[1]+str(value),s)
  color='{r: %g, g: %g, b: %g, a: 1}'%rgb
  for prop in ('_BaseColor','_Color'):
   s=re.sub(r'(- '+prop+r': )[^\n]+',lambda m:m[1]+color,s)
  s='\n'.join(line.rstrip() for line in s.splitlines())+'\n'
  path.write_text(s);path.with_suffix('.mat.meta').write_text(f'fileFormatVersion: 2\nguid: {mg}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 2100000\n')
 # Store immutable source geometry. Meshes are decoded on enable in Editor and builds.
 for (bone,material),m in list(MESHES.items()):
  name=bone.replace(' ','_')+'_'+material
  p=OUT/(name+'.bytes'); data=bytearray(struct.pack('<III',0x4B4D5331,len(m.v),len(m.f)*3))
  normals=m.normals()
  for pos,n,uv in zip(m.v,normals,m.uv):data+=struct.pack('<8f',*pos,*n,*uv)
  for f in m.f:data+=struct.pack('<3I',*f)
  p.write_bytes(data);p.with_suffix('.bytes.meta').write_text(f'fileFormatVersion: 2\nguid: {guid(name)}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
  bone_node=next(n for n in NODES if n['name']==bone)
  n=joint('Mesh '+name,bone_node);n.update(mesh=m,source=guid(name),material=materials[material])
 def ref(i):return '{fileID: '+str(i)+'}'
 def vec(v):return '{'+', '.join(f'{k}: {x:.8g}' for k,x in zip('xyz',v))+'}'
 def mono(i,script,fields,owner,klass):
  return f'''--- !u!114 &{i}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {ref(owner)}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: SwordKing.Runtime::SwordKing.{klass}
'''+''.join(f'  {k}: {v}\n' for k,v in fields.items())
 out=['%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n']
 for n in NODES:
  i=n['id'];comps=[i+1]
  if 'mesh'in n:comps += [i+2,i+3,i+4]
  if n is refs['player']:comps += [3648755183412612744,7072597504312420402,900000]
  out.append(f'''--- !u!1 &{i}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
'''+''.join(f'  - component: {ref(c)}\n' for c in comps)+f'''  m_Layer: 0
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
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:'''+ ('\n'+''.join(f'  - {ref(c["id"]+1)}\n' for c in n['children']) if n['children'] else ' []\n')+f'''  m_Father: {ref(n['parent']['id']+1 if n['parent'] else 0)}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
''')
  if 'mesh'in n:
   out.append(f'''--- !u!33 &{i+2}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {ref(i)}
  m_Mesh: {{fileID: 0}}
''')
   r=re.sub(r'&\d+',f'&{i+3}',renderer,count=1)
   r=re.sub(r'm_GameObject: \{fileID: \d+\}',f'm_GameObject: {ref(i)}',r)
   r=re.sub(r'(m_Materials:\n  - )[^\n]+',lambda m:m[1]+'{fileID: 2100000, guid: '+n['material']+', type: 2}',r)
   out.append(r)
   out.append(mono(i+4,guid('KnightMeshSource.cs'),dict(source='{fileID: 4900000, guid: '+n['source']+', type: 3}'),i,'KnightMeshSource'))
 c=re.sub(r'm_GameObject: \{fileID: \d+\}',f'm_GameObject: {ref(refs["player"]["id"])}',controller);out.append(c)
 rig=dict(controller=ref(3648755183412612744),visualRoot=ref(refs['visual']['id']+1),swordPivot=ref(refs['sword']['id']+1),leftLeg=ref(refs['leftLeg']['id']+1),rightLeg=ref(refs['rightLeg']['id']+1))
 out.append(mono(7072597504312420402,'baee79b2435c56c6993e0e128123f778',rig,refs['player']['id'],'PlayerRig'))
 keys=['pelvis','leftFoot','rightFoot','chest','head','leftArm','rightArm','leftForearm','rightForearm','leftShin','rightShin','weaponGrip','capeTop','capeMid','capeHem']
 fields={k:ref(refs[k]['id']+1) for k in keys}
 fields.update(walkingStride='0.20',sprintingStride='0.29',walkingFootLift='0.07',sprintingFootLift='0.13',movementAmount='0.85',attackAmount='0.7',poseResponse='18',upperArmLength='0.285',forearmLength='0.29')
 out.append(mono(900000,'85d7be92fb5b4dff914c0dca52e12bd4',fields,refs['player']['id'],'KnightAnimation'))
 PREFAB.write_text(''.join(out))
 (ROOT/'Scripts/Player/KnightMeshSource.cs.meta').write_text(f'fileFormatVersion: 2\nguid: {guid("KnightMeshSource.cs")}\n')
 (OUT.with_suffix('.meta')).write_text(f'fileFormatVersion: 2\nguid: {guid("KnightFolder")}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n')
 print(f'Knight: {len(MESHES)} mesh sections, {sum(len(m.v) for m in MESHES.values()):,} vertices, {sum(len(m.f) for m in MESHES.values()):,} triangles')
 return refs

if __name__=='__main__':build()
