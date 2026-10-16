"""Render the authored geometry for review; requires numpy and Pillow. Not a Unity capture."""
import importlib.util,math,numpy as np
from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
spec=importlib.util.spec_from_file_location('knight','Tools/build_knight_prefab.py');k=importlib.util.module_from_spec(spec);spec.loader.exec_module(k)
refs=k.author()
def rx(deg):
 a=math.radians(deg);c,s=math.cos(a),math.sin(a);return np.array([[1,0,0],[0,c,-s],[0,s,c]])
def ry(deg):
 a=math.radians(deg);c,s=math.cos(a),math.sin(a);return np.array([[c,0,s],[0,1,0],[-s,0,c]])
rot={};pos={}
for n in k.NODES:
 rot[n['name']]=np.eye(3);pos[n['name']]=np.array(n['pos'],float)
rot[refs['sword']['name']]=ry(-10)@rx(35)
# Pose lower legs naturally with a small outward stance.
def transform(n):
 if not n['parent']:return np.eye(3),np.array(n['pos'],float)
 pr,pp=transform(n['parent']);return pr@rot[n['name']],pp+pr@pos[n['name']]
# Reference-style relaxed left arm.
rot['Left shoulder']=rx(-8);rot['Left elbow']=rx(-20)
# Exact two-bone IK, matching the runtime arm reach constraint.
def align(a,b):
 a=a/np.linalg.norm(a);b=b/np.linalg.norm(b);v=np.cross(a,b);c=np.dot(a,b)
 if c<-.9999:return rx(180)
 skew=np.array([[0,-v[2],v[1]],[v[2],0,-v[0]],[-v[1],v[0],0]])
 return np.eye(3)+skew+skew@skew/(1+c)
shoulder=refs['rightArm'];elbow=refs['rightForearm'];sword=refs['sword'];grip=refs['weaponGrip']
s=transform(shoulder)[1];target=transform(grip)[1];delta=target-s;reach=.572
if np.linalg.norm(delta)>reach:
 clamp=s+delta/np.linalg.norm(delta)*reach
 pos[sword['name']]+=clamp-target;target=clamp
f=(target-s)/np.linalg.norm(target-s);distance=np.linalg.norm(target-s);bend=np.array([1,0,-.35]);bend-=np.dot(bend,f)*f;bend/=np.linalg.norm(bend)
a=(.285**2-.29**2+distance**2)/(2*distance);out=math.sqrt(max(0,.285**2-a*a));ep=s+f*a+bend*out
parentrot=transform(shoulder['parent'])[0];rot[shoulder['name']]=parentrot.T@align(np.array([0,-1.,0]),ep-s)
er,epactual=transform(elbow);rot[elbow['name']]=transform(shoulder)[0].T@align(np.array([0,-1.,0]),target-epactual)
allv=[];allf=[];allc=[];alln=[];allvc=[]
for (bone,mat),mesh in k.MESHES.items():
 n=next(n for n in k.NODES if n['name']==bone);r,p=transform(n);v=np.asarray(mesh.v)@r.T+p
 allf.extend(np.asarray(mesh.f)+len(allv));allv.extend(v);allc.extend([mat]*len(mesh.f));alln.extend(np.asarray(mesh.normals())@r.T);allvc.extend([k.PALETTE[mat][0]]*len(mesh.v))
v=np.asarray(allv);f=np.asarray(allf);base=np.asarray([k.PALETTE[x][0] for x in allc])
# Software geometry preview, not a Unity capture. Draw depth-sorted mesh faces.
def render(azimuth,name):
 w,h=900,1250;center=np.array([0,1.0,0]);az=math.radians(azimuth);view=np.array([math.sin(az),.18,math.cos(az)]);view/=np.linalg.norm(view)
 right=np.cross([0,1,0],view);right/=np.linalg.norm(right);up=np.cross(view,right)
 pts=v-center;xy=np.stack((pts@right,pts@up),axis=1);depth=pts@view
 screen=np.round(np.stack((w*.5+xy[:,0]*525,h*.53-xy[:,1]*525),axis=1)).astype(np.int32)
 normals=np.asarray(alln);light=np.array([-.55,.8,.7]);light/=np.linalg.norm(light)
 halfv=light+view;halfv/=np.linalg.norm(halfv)
 diffuse=np.maximum(0,normals@light);rim=np.maximum(0,normals@np.array([.7,.25,-.65]));specular=np.maximum(0,normals@halfv)**55
 colors=np.asarray(allvc)*(.35+.55*diffuse[:,None]+.24*rim[:,None])+specular[:,None]*.7
 colors=np.clip(colors,0,1)**(1/1.4)*255
 img=np.zeros((h,w,3),np.uint8)
 for y in range(h):img[y,:,:]=np.array([15,20,28])+np.uint8(12*y/h)
 pil=Image.fromarray(img);draw=ImageDraw.Draw(pil)
 draw.ellipse((w//2-200,int(h*.94)-16,w//2+200,int(h*.94)+16),fill=(9,12,17));img=np.array(pil)
 zbuf=np.full((h,w),-np.inf)
 for ids in f:
  p=screen[ids].astype(float);lo=np.maximum(np.floor(p.min(axis=0)).astype(int),0);hi=np.minimum(np.ceil(p.max(axis=0)).astype(int),[w-1,h-1])
  if np.any(hi<lo):continue
  x0,y0=p[0];x1,y1=p[1];x2,y2=p[2];den=(y1-y2)*(x0-x2)+(x2-x1)*(y0-y2)
  if abs(den)<.01:continue
  yy,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1];xx=xx+.5;yy=yy+.5
  a=((y1-y2)*(xx-x2)+(x2-x1)*(yy-y2))/den;b=((y2-y0)*(xx-x2)+(x0-x2)*(yy-y2))/den;c=1-a-b
  z=a*depth[ids[0]]+b*depth[ids[1]]+c*depth[ids[2]]
  region=zbuf[lo[1]:hi[1]+1,lo[0]:hi[0]+1];mask=(a>=0)&(b>=0)&(c>=0)&(z>region)
  if not mask.any():continue
  shade=a[:,:,None]*colors[ids[0]]+b[:,:,None]*colors[ids[1]]+c[:,:,None]*colors[ids[2]]
  img[lo[1]:hi[1]+1,lo[0]:hi[0]+1][mask]=shade[mask].astype(np.uint8);region[mask]=z[mask]
 pil=Image.fromarray(img)
 d=ImageDraw.Draw(pil)
 d.text((30,22),'PLATE KNIGHT / GEOMETRY PREVIEW',fill=(211,216,223))
 d.text((30,43),'Custom mesh + material colors; not a Unity screenshot',fill=(141,151,166))
 Path('Documentation').mkdir(exist_ok=True)
 pil.save('Documentation/'+name)
render(22,'ArmoredKnightPreview.png');render(152,'ArmoredKnightBackPreview.png')
print('Rendered front and back actual mesh previews')
