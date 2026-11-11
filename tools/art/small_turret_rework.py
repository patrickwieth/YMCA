"""Clean mechanical reconstruction: explicit UV islands, no camera-projected moving parts.
Called by model_gdi_small_turrets for pass >= 6. Previous studies remain untouched.
"""
import math
import bpy
from mathutils import Vector

def rebuild(c):
    kind=c['args'].kind; iteration=c['args'].iteration
    moving=c['moving']; pivot=c['pivot']; materials=c['materials']
    for o in list(moving.objects):
        if o.type=='MESH': bpy.data.objects.remove(o,do_unlink=True)
    c['records'][:]=[r for r in c['records'] if r['stationary']]
    shades={
        'armor':[(110,76,37),(144,98,43),(176,127,51),(202,155,66),(225,184,88),(243,208,114)],
        'metal':[(24,29,34),(40,48,53),(60,73,79),(83,99,106),(112,132,138),(151,174,178)]}
    for family,colors in shades.items():
        for i,rgb in enumerate(colors):
            name=f'REWORK {family} {i}';col=c['nearest'](rgb)
            m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;n.clear()
            e=n.new('ShaderNodeEmission');out=n.new('ShaderNodeOutputMaterial');e.inputs[0].default_value=(*map(c['lin'],col),1);m.node_tree.links.new(e.outputs[0],out.inputs[0]);materials[name]=m
    for i,factor in enumerate((.52,.65,.78,.90,1.,1.08)):
        m=materials['source gold'].copy();name=f'REWORK texarmor {i}';m.name=name
        nodes=m.node_tree.nodes;em=next(n for n in nodes if n.type=='EMISSION');link=em.inputs[0].links[0];up=link.from_socket;m.node_tree.links.remove(link)
        mul=nodes.new('ShaderNodeMixRGB');mul.blend_type='MULTIPLY';mul.inputs[0].default_value=1;mul.inputs[2].default_value=(factor,factor,factor,1);m.node_tree.links.new(up,mul.inputs[1]);m.node_tree.links.new(mul.outputs[0],em.inputs[0]);materials[name]=m
    lit=[]
    def mesh(name,vs,fs,family='metal',texture=None,shaded_texture=False):
        # Every UV island is explicit. Flat material faces never sample the photo.
        uv={i:[(48,56)]*len(f) for i,f in enumerate(fs)};fm={}
        if texture:
            for idx,quad in texture.items():uv[idx]=quad;fm[idx]='source gold' if family=='armor' else 'source steel'
        o=c['mesh'](name,vs,fs,material=f'REWORK {family} 3',face_uv=uv,face_materials=fm)
        if shaded_texture:o['shaded_texture']=True
        lit.append((o,family,set(texture or {})));return o
    def tube(name,a,b,r,inner=0,family='metal',n=32):
        a,b=Vector(a),Vector(b);axis=(b-a).normalized();u=axis.cross(Vector((0,0,1)) if abs(axis.z)<.9 else Vector((0,1,0))).normalized();v=axis.cross(u)
        vs=[]
        for center,rr in [(a,r),(b,r)]+([(a,inner),(b,inner)] if inner else []):
            vs += [tuple(center+rr*(u*math.cos(i*math.tau/n)+v*math.sin(i*math.tau/n))) for i in range(n)]
        fs=[]
        for i in range(n):
            j=(i+1)%n;fs.append((i,j,n+j,n+i))
            if inner:fs += [(n+i,n+j,3*n+j,3*n+i),(i,2*n+i,2*n+j,j),(2*n+i,3*n+i,3*n+j,2*n+j)]
        if not inner:fs += [tuple(range(n-1,-1,-1)),tuple(n+i for i in range(n))]
        return mesh(name,vs,fs,family)
    def block(name,center,size,family='metal',texture=None):
        center=Vector(center);size=Vector(size)/2
        vs=[tuple(center+Vector((x*size.x,y*size.y,z*size.z))) for z in (-1,1) for y in (-1,1) for x in (-1,1)]
        return mesh(name,vs,[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)],family,texture)
    def strip(name,points,r=.025,family='metal'):
        for i in range(len(points)-1):tube(name+f' {i}',points[i],points[i+1],r,family=family,n=8)
    def patch(name,center,u,v,quad,family='armor'):
        center,u,v=Vector(center),Vector(u),Vector(v)
        return mesh(name,[tuple(center-u+v),tuple(center+u+v),tuple(center+u-v),tuple(center-u-v)],[(0,1,2,3)],family,{0:quad})
    if kind=='aa':
        # Closed inset backing under armor plates replaces the empty black slot.
        levels=[(1.49,2.22),(1.51,2.53),(1.40,2.98),(1.13,3.46),(.62,3.83),(.08,3.96)]
        n=64
        vs=[(r*.94*math.cos(i*math.tau/n),r*.94*math.sin(i*math.tau/n),z) for r,z in levels for i in range(n)]
        fs=[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(levels)-1) for i in range(n)]
        fs.append(tuple((len(levels)-1)*n+i for i in range(n)));mesh('AA enclosed inner armor',vs,fs)
        # Panel seams are actual narrow geometric gaps, not stretched photo lines.
        for sector in range(8):
            lo=sector*math.pi/4+.009;hi=(sector+1)*math.pi/4-.009
            for band in range(2):
                inds=range(0,3) if band==0 else range(2,len(levels))
                rings=list(inds);vs=[]
                for j in rings:
                    r,z=levels[j];z+=.012 if band==1 and j==2 else (-.012 if band==0 and j==2 else 0)
                    for k in range(9):
                        theta=lo+(hi-lo)*k/8
                        # Narrow armored longitudinal gun channel; solid backing remains.
                        rr=r-(.13 if 1.24<theta<1.90 and j>=2 else 0)
                        vs.append((rr*math.cos(theta),rr*math.sin(theta),z))
                fs=[(j*9+k,j*9+k+1,(j+1)*9+k+1,(j+1)*9+k) for j in range(len(rings)-1) for k in range(8)]
                tex={i:[(48+15*(vi%9)/8,68-25*(vs[vi][2]-2.22)/1.74) for vi in face] for i,face in enumerate(fs)} if iteration>=7 else None
                mesh(f'AA curved armor plate {sector} {band}',vs,fs,'armor',tex,True)
        tube('AA lower steel retaining rim',(0,0,2.21),(0,0,2.28),1.50,inner=1.43)
        axis=Vector((0,math.cos(math.radians(25)),math.sin(math.radians(25))));root=Vector((0,.75,3.12));u=Vector((1,0,0));v=axis.cross(u)
        tube('AA gun receiver',root-axis*.30,root+axis*.61,.36,n=32)
        tube('AA gun socket lip',root+axis*.48,root+axis*.60,.43,inner=.32)
        for x in (-.43,.43):
            block('AA cheek armor '+str(x),(x,.82,3.12),(.19,.60,.65),'armor')
        for j in range(6):
            off=.22*(u*math.cos(j*math.tau/6)+v*math.sin(j*math.tau/6))
            tube('AA hollow barrel '+str(j),root+axis*.50+off,root+axis*2.35+off,.088,inner=.048,n=12)
        for t in (.68,1.63,2.13):tube('AA machined barrel collar '+str(t),root+axis*t,root+axis*(t+.09),.328,inner=.27,n=24)
        # Source detail cuts confined to their own plates; never spread onto the dome.
        for theta in (-math.pi/4,3*math.pi/4,math.pi):
            normal=Vector((math.cos(theta),math.sin(theta),0));tangent=Vector((-normal.y,normal.x,0));center=normal*1.49+Vector((0,0,2.70))
            patch('AA vent plate',center,tangent*.19,(0,0,.23),[(40,57),(46,58),(46,66),(40,65)],'metal')
        for theta in (-math.pi/2,0):
            normal=Vector((math.cos(theta),math.sin(theta),0));tangent=Vector((-normal.y,normal.x,0));center=normal*1.515+Vector((0,0,2.50))
            patch('AA blue indicator',center,tangent*.14,(0,0,.16),[(55,70),(62,70),(62,77),(55,77)],'metal')
        sensor=c['floor'](31,40,3.25)
        block('AA optical sensor body',sensor,(.35,.40,.49))
        block('AA optical bracket',sensor+Vector((.12,0,-.59)),(.15,.24,.77))
        patch('AA source optic',sensor+Vector((0,-.206,0)),(.16,0,0),(0,0,.22),[(29,34),(34,35),(34,43),(29,42)],'metal')
        tube('AA antenna',(-1.55,0,2.45),(-1.55,0,4.15),.025,n=8)
        dish=c['floor'](53,25,4.3);normal=Vector((.5,-.7,.4)).normalized();tube('AA radar support',dish-Vector((0,0,.7)),dish,.05,n=8)
        tube('AA radar rim',dish-normal*.05,dish+normal*.02,.28,inner=.23,n=20)
        tube('AA radar reflector',dish-normal*.07,dish-normal*.055,.235,n=20)
        tube('AA radar feed',dish-normal*.03,dish+normal*.15,.025,n=8)
    else:
        # Real chamfered receiver with a level roof, not an extruded photo silhouette.
        xmin,xmax=-2.25,.55;y=1.05 if iteration>=7 else .83;z0,z1=(3.65,5.65) if iteration>=7 else (3.85,5.12);b=.18
        profile=[(-y,z0+b),(-y+b,z0),(y-b,z0),(y,z0+b),(y,z1-b),(y-b,z1),(-y+b,z1),(-y,z1-b)]
        vs=[(x,yy,zz) for x in (xmin,xmax) for yy,zz in profile]
        fs=[tuple(range(7,-1,-1)),tuple(range(8,16))]+[(i,(i+1)%8,8+(i+1)%8,8+i) for i in range(8)]
        # Roof and side are bounded UV islands of actual observed armor only.
        tex={7:[(18,13),(36,5),(57,14),(39,23)],9:[(12,17),(12,27),(29,34),(29,23)],5:[(12,17),(12,27),(29,34),(29,23)]}
        mesh('SONIC chamfered receiver',vs,fs,'armor',tex,iteration>=7)
        block('SONIC rear service plate',(-2.265,0,4.40),(.06,1.12,.55))
        for yy in (-.37,-.12,.12,.37):block('SONIC rear cooling slot',(-2.305,yy,4.40),(.035,.08,.35))
        block('SONIC rotating root',(0,0,2.51),(1.40,1.95,.30))
        for yy in (-.91,.91):
            # Broad dogleg solid support, with rim and inset instead of mirrored photo cutout.
            outline=[(-.85,2.65),(.45,2.65),(.36,3.02),(-.12,3.52),(-.20,4.28),(-1.03,4.28),(-1.10,3.77),(-.76,3.40)]
            vs=[(x,yy+d,z) for d in (-.14,.14) for x,z in outline];n=len(outline)
            fs=[tuple(range(n)),tuple(range(2*n-1,n-1,-1))]+[(i,n+i,n+(i+1)%n,(i+1)%n) for i in range(n)]
            arm_uv=[(44,74),(55,77),(58,73),(50,57),(46,36),(37,33),(30,43),(36,58)]
            mesh('SONIC solid support '+str(yy),vs,fs,'armor',{0:arm_uv,1:list(reversed(arm_uv))} if iteration>=7 else None,iteration>=7)
            tube('SONIC support bearing '+str(yy),(-.62,yy-.20,4.10),(-.62,yy+.20,4.10),.49,inner=.36)
            tube('SONIC bearing hub '+str(yy),(-.62,yy-.215,4.10),(-.62,yy+.215,4.10),.28,n=24)
        root=Vector((-1.44,.1,4.50));axis=Vector((1,0,.20)).normalized()
        tube('SONIC central barrel',root,root+axis*5.02,.83,n=40)
        for i,(distance,radius) in enumerate(((2.65,1.70),(3.90,1.88),(5.25,2.00))):
            center=root+axis*distance
            # Separate stepped shoulders, raised rims and apertures, no image of other rings.
            tube('SONIC emitter body '+str(i),center-axis*.11,center+axis*.11,radius-.07,inner=.77 if i==2 else 1.02,n=64)
            tube('SONIC emitter edge '+str(i),center-axis*.065,center+axis*.065,radius,inner=radius-.11,n=64)
            tube('SONIC front raised ring '+str(i),center+axis*.11,center+axis*.15,radius-.04 if iteration>=7 else radius-.22,inner=radius-.13 if iteration>=7 else radius-.28,n=64)
            if iteration>=7:
                groove=tube('SONIC dark machined groove '+str(i),center+axis*.112,center+axis*.117,radius-.13,inner=radius-.29,n=64)
                groove['shade_bias']=-2
            tube('SONIC inner aperture rim '+str(i),center+axis*.11,center+axis*.16,.91 if i==2 else 1.10,inner=.76 if i==2 else 1.01,n=48)
            tube('SONIC rear raised ring '+str(i),center-axis*.15,center-axis*.11,radius-.22,inner=radius-.28,n=64)
            # Four restrained fastening bosses; geometry rather than mottled projected texture.
            u=axis.cross(Vector((0,0,1))).normalized();v=axis.cross(u)
            for j in range(4):
                r=(radius-.36)*(u*math.cos(j*math.pi/2)+v*math.sin(j*math.pi/2))
                tube('SONIC emitter fastener',center+r+axis*.11,center+r+axis*.17,.055,n=8)
        face=root+axis*5.25
        tube('SONIC lens bezel',face-axis*.08,face+axis*.11,.75,inner=.59,n=48)
        lens=tube('SONIC recessed cyan lens',face-axis*.09,face-axis*.075,.59,n=48)
        core=tube('SONIC bright core',face-axis*.074,face-axis*.07,.38,n=40)
        for o,role in ((lens,'cyan'),(core,'lightcyan')):
            for poly in o.data.polygons:poly.material_index=list(materials).index(role)
            lit[:]=[entry for entry in lit if entry[0]!=o]
        dish=Vector((-1.85,.15,5.55));normal=Vector((.4,-.8,.3)).normalized()
        tube('SONIC sensor stalk',dish-Vector((0,0,.45)),dish,.05,n=8)
        tube('SONIC sensor rim',dish-normal*.035,dish+normal*.035,.24,inner=.18,n=20)
        tube('SONIC sensor face',dish-normal*.04,dish-normal*.03,.18,n=20)
    def relight():
        light=Vector((-.35,-.55,.80)).normalized();names=list(materials)
        for o,family,excluded in lit:
            rot=o.matrix_world.to_3x3()
            for poly in o.data.polygons:
                textured=poly.index in excluded
                if textured and not o.get('shaded_texture'):continue
                normal=(rot@poly.normal).normalized();value=normal.dot(light)
                bias=o.get('shade_bias',0)
                if iteration>=7 and any(t in o.name for t in ('raised ring','aperture rim','socket lip','lens bezel')):bias+=1
                shade=max(0,min(5,int((value+1)*2.8)+bias))
                poly.material_index=names.index(f'REWORK {"texarmor" if textured else family} {shade}')
    return relight
