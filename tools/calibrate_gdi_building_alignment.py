"""Propose/review native GDI art placement and DECORATION-only isometric boxes.
Uses resolved engine registration; never edits source PNGs or gameplay traits.
The visual diamond is an art envelope, NOT an inferred collision footprint.
"""
from pathlib import Path
import argparse,json,math,hashlib
from PIL import Image,ImageDraw
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
ACTORS=('afac','nuke','pyle','hq','rep','hpad.td','proc.td','afld.gdi','gtek','weap.td','eye','upgc','syrd.gdi')
# Reviewed native ground contacts, not elevated flags or roof edges.
GROUND_CONTACTS={
 'upgc':[(3,181),(251,181)],
 'syrd.gdi':[(5,151),(290,156)],
 'afac':[(5,166),(299,169)], 'nuke':[(2,229),(253,215)],
 'pyle':[(31,113),(229,124)], 'hq':[(44,209),(218,163)],
 'rep':[(9,166),(281,167)], 'hpad.td':[(7,83),(192,82)],
 'proc.td':[(4,147),(199,114)], 'gtek':[(10,181),(229,188)],
 'weap.td':[(20,295),(370,271)], 'eye':[(20,218),(245,194)],
 'afld.gdi':[(43,139),(307,121),(350,163),(78,298),(283,253)],
}

def signed_round(value):return math.floor(value+.5) if value>=0 else math.ceil(value-.5)
def world_offset(pixel,tile,scale):return int(math.copysign(math.ceil(abs(pixel)*scale/tile),pixel)) if pixel else 0

def propose(entry, ground_contacts=None, bib_prefix='gdibib', extra_files=()):
 path=ROOT/'mods/ca'/entry['File'];im=Image.open(path).convert('RGBA');alpha=np.array(im)[:,:,3]>=32
 additional=[]
 for filename in extra_files:
  other_path=ROOT/'mods/ca'/filename
  with Image.open(other_path) as other:
   assert other.size==im.size,filename
   alpha|=np.array(other.convert('RGBA'))[:,:,3]>=32
  additional.append({'File':filename,'SHA256':hashlib.sha256(other_path.read_bytes()).hexdigest()})
 ys,xs=np.where(alpha);left,right=int(xs.min()),int(xs.max())+1
 contacts=(GROUND_CONTACTS if ground_contacts is None else ground_contacts)[entry['Actor']]
 for x,y in contacts:assert alpha[y,x],(entry['Actor'],x,y)
 if entry['Actor']=='syrd.gdi':
  bib=None;nx,ny=entry['BuildingDimensions'] # mathematical envelope only; water stays visible
 else:
  bib=next(b for b in entry['Bibs'] if b['Image'].startswith(bib_prefix))['Image']
  if bib_prefix=='gdibib' and entry['Actor']=='afld.gdi':bib='gdibib4x5'
  nx,ny=map(int,bib.removeprefix(bib_prefix).split('x'))
 # Isometric ground axes u=x+2y, v=-x+2y. Align the two BACK contact
 # extrema to the two rear bib edges (2 screen pixels of clearance).
 du=-nx*64+4-min(x+2*y for x,y in contacts)
 dv=-ny*64+4-min(-x+2*y for x,y in contacts)
 ox=signed_round((du-dv)/2+im.width/2)
 oy=signed_round((du+dv)/4+im.height-im.width/4)
 origin_x=-im.width/2+ox;origin_y=-im.height+im.width/4+oy
 front=[]
 for fraction in (.25,.5,.75):
  x=int(left+(right-left-1)*fraction);column=np.where(alpha[:,x])[0]
  if len(column):front.append((x,int(column.max())))
 ground=contacts+front
 umin=min(x+2*y for x,y in contacts);vmin=min(-x+2*y for x,y in contacts)
 umax=max(x+2*y for x,y in ground);vmax=max(-x+2*y for x,y in ground)
 # Center on the ground arrangement, not antenna/flag alpha extrema.
 preferred_x=signed_round((umin+umax-vmin-vmax)/4+origin_x)
 # An explicit decoration box is a symmetric diamond. On elongated art choose
 # the nearest feasible center that keeps both the art envelope and box on the
 # bib; do not pretend this symmetric UI box is the actual collision footprint.
 choices=[]
 for cx in range(preferred_x-128,preferred_x+129):
  xx=xs+.5+origin_x-cx
  w=math.ceil((2*float(np.max(np.abs(xx)))+8)/16)*16;d=w//2
  envelope=float(np.max(ys+.5+origin_y+.5*np.abs(xx)))
  cy=max(math.ceil(envelope-d/2+2),math.ceil((-nx*64+w/2-cx)/2),math.ceil((-ny*64+w/2+cx)/2))
  if cy<=min((nx*64-w/2-cx)/2,(ny*64-w/2+cx)/2):choices.append((abs(cx-preferred_x),w,cx,cy))
 assert choices,entry['Actor']
 _,width,center_x,center_y=min(choices);depth=width//2
 screen_x=xs+.5+origin_x-center_x;rear=center_y-depth/2
 height=math.ceil(float(np.max(rear+.5*np.abs(screen_x)-(ys+.5+origin_y))))+2
 tile_x,tile_y=entry['TileSize'];scale=entry['TileScale']
 bounds=[int(width*scale/tile_x),int(depth*scale/tile_y),world_offset(center_x,tile_x,scale),world_offset(center_y,tile_y,scale)]
 result={'Actor':entry['Actor'],'Image':entry['Image'],'File':entry['File'],'Bib':bib,'BibCells':[nx,ny],
  'Offset':[ox,oy],'DecorationBounds':bounds,'DecorationHeight':height,
  'VisualGroundWidth':width,'VisualGroundDepth':depth,'GroundRearY':rear,'SourceGroundContacts':contacts,'FrontContactSamples':front,
  'SourceSize':list(im.size),'SourceSHA256':hashlib.sha256(path.read_bytes()).hexdigest()}
 if entry['Actor']=='hpad.td':
  # A small helipad centered on its foundation, not pushed against the rear edge.
  shift=-center_y
  result['Offset'][1]+=shift;result['DecorationBounds'][3]=0
  result['GroundRearY']+=shift;result['PlacementPolicy']='centered-visual-ground-envelope'
 if additional:result['AdditionalSourceImages']=additional
 if bib is None:result['GroundEnvelopeCells']=result.pop('BibCells')
 return result

def image_origin(im,offset,anchor='Bottom'):
 x,y=map(float,offset.split(',')[:2]);return (x-im.width/2,y-im.height+im.width/4) if anchor=='Bottom' else (x-im.width/2,y-im.height/2)

def report(entries,out,actors=ACTORS,bib_prefix='gdibib'):
 # Engine-exported polygon/offsets, not a separately invented selection rendering.
 cards=[]
 for entry in entries:
  if entry['Actor'] not in actors:continue
  im=Image.open(ROOT/'mods/ca'/entry['File']).convert('RGBA');ix,iy=image_origin(im,entry['Offset'],entry['Anchor'])
  bib=next((b for b in entry['Bibs'] if b['Image'].startswith(bib_prefix)),None)
  if bib:
   bi=Image.open(ROOT/'mods/ca'/bib['File']).convert('RGBA');bx,by=image_origin(bi,bib['Offset'],bib['Anchor'])
   bx+=bib['ScreenOffset'][0];by+=bib['ScreenOffset'][1]
  else:
   bi=Image.new('RGBA',(1,1));bx,by=0,0 # neutral diagnostic backdrop, NOT terrain artwork
  polygon=entry['SelectionPolygon'];ground=entry['GroundCorners']
  left=min(ix,bx,min(p[0] for p in polygon))-12;right=max(ix+im.width,bx+bi.width,max(p[0] for p in polygon))+12
  top=min(iy,by,min(p[1] for p in polygon))-12;bottom=max(iy+im.height,by+bi.height,max(p[1] for p in polygon))+12
  image=Image.new('RGBA',(math.ceil(right-left),math.ceil(bottom-top)),(68,71,68,255))
  image.alpha_composite(bi,(round(bx-left),round(by-top)));image.alpha_composite(im,(round(ix-left),round(iy-top)))
  d=ImageDraw.Draw(image);xy=lambda points:[(round(x-left),round(y-top)) for x,y in points]
  d.line(xy(ground+[ground[0]]),fill='#e3b249',width=1)
  d.line(xy(polygon+[polygon[0]]),fill='#63ef91',width=1)
  # Render the upright edges too, to expose box/base alignment.
  for a,b in ((ground[1],polygon[1]),(ground[3],polygon[5])):d.line(xy([a,b]),fill='#63ef91',width=1)
  d.line(xy([[-4,0],[4,0]]),fill='#ff79b0');d.line(xy([[0,-4],[0,4]]),fill='#ff79b0')
  cards.append((entry,image))
 width=576;height=max(im.height for _,im in cards)+58
 sheet=Image.new('RGB',(width*3,height*math.ceil(len(cards)/3)),'#303530');d=ImageDraw.Draw(sheet)
 for i,(entry,im) in enumerate(cards):
  x,y=(i%3)*width,(i//3)*height
  sheet.paste(im,(x+(width-im.width)//2,y+35),im)
  d.text((x+12,y+7),f"{entry['Actor'].upper()}   offset {entry['Offset']}   selection height {entry['Height']}",fill='white')
  d.text((x+12,y+21),'Green: resolved selection | amber: visual ground | pink: actor pivot',fill='#cccccc')
 out.parent.mkdir(parents=True,exist_ok=True);sheet.save(out)

if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('geometry',type=Path)
 p.add_argument('--propose',type=Path);p.add_argument('--preview',type=Path);a=p.parse_args()
 entries=json.loads(a.geometry.read_text(encoding='utf-8-sig'))
 if a.propose:
  values=[propose(e) for e in entries if e['Actor'] in ACTORS]
  a.propose.write_text(json.dumps({'policy':'Native art: reviewed rear ground contacts on the two rear bib edges; visual selection envelope follows the art, not collision.',
   'contact_axis_clearance':4,'entries':values},indent=2)+'\n')
  for e in values:print(e['Actor'],e['Offset'],e['DecorationBounds'],e['DecorationHeight'])
 if a.preview:report(entries,a.preview)
