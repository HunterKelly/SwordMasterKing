"""Independent format/reference checks. Does not substitute for Unity tests."""
from pathlib import Path
import struct,math,re,hashlib,subprocess
root=Path('SwordKing/Assets/_SwordKing')
count=vertices=triangles=0
for p in (root/'Art/Models/Knight').glob('*.bytes'):
 b=p.read_bytes();magic,nv,ni=struct.unpack_from('<III',b)
 assert magic==0x4B4D5331 and nv>0 and ni>0 and ni%3==0,p
 assert len(b)==12+nv*32+ni*4,p
 for i in range(nv):
  v=struct.unpack_from('<8f',b,12+i*32)
  assert all(math.isfinite(x) for x in v),p
  assert abs(sum(x*x for x in v[3:6])-1)<.001,(p,i,'normal')
  assert max(abs(x) for x in v[:3])<5,(p,i,'bounds')
 ids=struct.unpack_from('<'+str(ni)+'I',b,12+nv*32)
 assert max(ids)<nv,p
 # Every face must have nonzero area, independent of saved normals.
 for i in range(0,ni,3):
  a,bv,c=[struct.unpack_from('<3f',b,12+ids[i+j]*32) for j in range(3)]
  ab=[bv[k]-a[k] for k in range(3)];ac=[c[k]-a[k] for k in range(3)]
  cross=(ab[1]*ac[2]-ab[2]*ac[1],ab[2]*ac[0]-ab[0]*ac[2],ab[0]*ac[1]-ab[1]*ac[0])
  assert sum(x*x for x in cross)>1e-20,(p,i,'degenerate')
 count+=1;vertices+=nv;triangles+=ni//3
s=(root/'Prefabs/Player/ArmoredKnight.prefab').read_text()
local_ids=re.findall(r'^--- !u!\d+ &(\d+)',s,re.M);assert len(local_ids)==len(set(local_ids))
for i in re.findall(r'\{fileID: (\d+)\}',s): assert i=='0' or i in local_ids,('local reference',i)
meta={}
for p in Path('SwordKing/Assets').rglob('*.meta'):
 m=re.search(r'^guid: (\w+)',p.read_text(errors='replace'),re.M)
 if m:meta[m[1]]=p
for g in re.findall(r'guid: ([a-f0-9]+)',s):assert g in meta,('asset reference',g)
assert len(re.findall('m_EditorClassIdentifier: SwordKing.Runtime::SwordKing.KnightMeshSource',s))==count
assert 'CharacterController:' in s and not any(c+':' in s for c in ('BoxCollider','SphereCollider','CapsuleCollider'))
assert '7072597504312420402' in s
assert 60000<triangles<180000
assert not re.search(r'\{fileID: 4300000,',s),'Guessed OBJ subasset IDs must not return'
# Hierarchy must be reciprocal and acyclic.
transforms={}
for ident,block in re.findall(r'--- !u!4 &(\d+)\nTransform:\n(.*?)(?=--- !u!|\Z)',s,re.S):
 parent=re.search(r'm_Father: \{fileID: (\d+)\}',block)[1]
 children=re.findall(r'- \{fileID: (\d+)\}',block)
 transforms[ident]=(parent,children)
for ident,(parent,children) in transforms.items():
 if parent!='0':assert ident in transforms[parent][1]
 for c in children:assert transforms[c][0]==ident
 seen=set();i=ident
 while i!='0':
  assert i not in seen,('hierarchy cycle',ident)
  seen.add(i);i=transforms[i][0]
print(f'PASS: {count} source meshes, {vertices:,} vertices, {triangles:,} nondegenerate triangles; valid normals, indices, material/script/source references, hierarchy, isolated collision, preserved PlayerRig ID.')
