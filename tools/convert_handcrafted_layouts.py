"""Convert original rectangular logical cells to isometric storage without stretching.

Produces audited, tileset-independent layout plans, NOT playable/render-ready maps.
No source, runtime map, replay, or generator settings are modified.
"""
from collections import Counter
from dataclasses import dataclass, field
import hashlib
import heapq
import json
import math
from pathlib import Path
import struct

from prepare_handcrafted_sources import ROOT, OUT, REVISION, git, save_exact


@dataclass
class Node:
    key: str
    value: str
    children: list = field(default_factory=list)

    def child(self, key):
        found = [n for n in self.children if n.key == key]
        if len(found) != 1:
            raise ValueError(f'Expected one {key} in {self.key}, got {len(found)}')
        return found[0]

    def json(self):
        return {'key': self.key, 'value': self.value, 'children': [n.json() for n in self.children]}


def miniyaml(text):
    root = Node('root', '')
    stack = [root]
    for number, line in enumerate(text.lstrip('\ufeff').splitlines(), 1):
        if not line.strip() or line.lstrip().startswith('#'):
            continue
        depth = len(line) - len(line.lstrip('\t'))
        if line[depth:1+depth] == ' ' or ':' not in line:
            raise ValueError(f'Unsupported indentation/content on line {number}')
        key, value = line[depth:].split(':', 1)
        if depth >= len(stack):
            raise ValueError(f'Invalid indentation on line {number}')
        stack = stack[:depth+1]
        node = Node(key, value.strip())
        stack[-1].children.append(node)
        stack.append(node)
    return root


def integer_list(value):
    return [int(x.strip()) for x in value.split(',')]


def to_mpos(cell):
    x, y = cell
    # Engine C# integer division truncates toward zero (not Python floor division).
    return (math.trunc((x-y)/2), x+y)


def to_cpos(memory):
    u, v = memory
    y = (v-(v & 1))//2-u
    return (v-y, y)


def translation(bounds, stored_size=None):
    bx, by, width, height = bounds
    if stored_size is not None:
        bx = by = 0
        width, height = stored_size
    # Positive x-y avoids the negative-division ambiguity. Include source border/scenery.
    half = (height+1)//2
    return (half+16-bx, 8-half-by)


def transformed(point, offset):
    return (point[0]+offset[0], point[1]+offset[1])


def world(cell, isometric):
    x, y = cell
    return (724*(x-y+1), 724*(x+y+1)) if isometric else (1024*x+512, 1024*y+512)


def shortest_paths(cells, start, targets, terrain_speeds, isometric):
    """Static terrain-only lower bound; no actor footprints/dynamic congestion/bridges."""
    allowed = {p: terrain_speeds.get(c['terrain'], 0) for p,c in cells.items()}
    if not allowed.get(start):
        raise ValueError('Start on impassable source terrain')
    remaining = set(targets)
    distance = {start: 0.0}; queue = [(0.0, start)]; result = {}
    steps = [(x,y) for x in (-1,0,1) for y in (-1,0,1) if x or y]
    while queue and remaining:
        cost, p = heapq.heappop(queue)
        if cost > distance[p]: continue
        if p in remaining:
            result[p] = cost; remaining.remove(p)
        for dx, dy in steps:
            q = (p[0]+dx,p[1]+dy)
            if not allowed.get(q): continue
            # Conservative corner clearance; native unit movement still needs validation.
            if dx and dy and (not allowed.get((p[0]+dx,p[1])) or not allowed.get((p[0],p[1]+dy))): continue
            a,b = world(p,isometric),world(q,isometric)
            extra = math.hypot(a[0]-b[0],a[1]-b[1])/1024 * 100/allowed[q]
            candidate = cost+extra
            if candidate < distance.get(q,float('inf')):
                distance[q] = candidate; heapq.heappush(queue,(candidate,q))
    return {p: result.get(p) for p in targets}


def profiles():
    raw = git('show', REVISION+':mods/ca/rules/world.yaml')
    save_exact(OUT.parent/'source-rules/world.yaml',raw)
    nodes = miniyaml(raw.decode('utf-8-sig'))
    found = {}
    for actor in nodes.children:
        for trait in actor.children:
            if trait.key.startswith('Locomotor@'):
                name = trait.child('Name').value
                if name in ('foot','wheeled','tracked','lighttracked'):
                    found[name] = {n.key: int(n.value) for n in trait.child('TerrainSpeeds').children}
    if 'foot' not in found or 'wheeled' not in found:
        raise ValueError('Missing source locomotor profiles')
    return found


def convert(record, movement):
    source = OUT/record['slug']
    text = (source/'map.yaml').read_text(encoding='utf-8-sig')
    nodes = miniyaml(text)
    bounds = integer_list(nodes.child('Bounds').value)
    bx,by,bw,bh = bounds
    raw = git('show', REVISION+':mods/ca/tilesets/'+record['tileset'].lower()+'.yaml')
    save_exact(OUT.parent/'source-tilesets'/ (record['tileset'].lower()+'.yaml'),raw)
    tileset = miniyaml(raw.decode('utf-8-sig'))
    templates = {int(t.child('Id').value):t for t in tileset.child('Templates').children}
    binary=(source/'map.bin').read_bytes()
    version,width,height=struct.unpack_from('<BHH',binary)
    if version==1: tile_offset,height_offset,resource_offset=5,0,5+3*width*height
    elif version==2: tile_offset,height_offset,resource_offset=struct.unpack_from('<III',binary,5)
    else: raise ValueError('Unsupported source binary format')
    offset=translation(bounds,(width,height)); cells={}; translated={}; memory=set(); rows=[]
    for x in range(bx,bx+bw):
        for y in range(by,by+bh):
            index=x*height+y
            tile,variant=struct.unpack_from('<HB',binary,tile_offset+3*index)
            resolved_variant=x%4+y%4*4 if variant==255 else variant
            terrain=templates[tile].child('Tiles').child(str(resolved_variant)).value
            if not terrain: raise ValueError('Unresolved source terrain')
            resource,density=struct.unpack_from('<BB',binary,resource_offset+2*index) if resource_offset else (0,0)
            elevation=binary[height_offset+index] if height_offset else 0
            if elevation: raise ValueError('Non-flat source requires an explicit height conversion')
            cell={'terrain':terrain,'resource':resource,'density':density,'source_tile':tile,'source_variant':variant,'height':elevation}
            p=(x,y); q=transformed(p,offset); uv=to_mpos(q)
            if to_cpos(uv)!=q or uv in memory: raise ValueError('Non-bijective coordinate transform')
            memory.add(uv);cells[p]=cell;translated[q]=cell
            rows.append([x,y,q[0],q[1],uv[0],uv[1],terrain,tile,variant,resource,density,elevation])
    actors=[]
    for actor in nodes.child('Actors').children:
        location=next((n for n in actor.children if n.key=='Location'),None)
        entry={'id':actor.key,'type':actor.value,'source_initializers':[n.json() for n in actor.children]}
        if location:
            old=integer_list(location.value);new=transformed(old,offset)
            entry.update(source_cell=old,target_cell=list(new),target_memory=list(to_mpos(new)),inside_original_bounds=tuple(old) in cells)
        actors.append(entry)
    # Prove every directed adjacency, including absent/exterior neighbours, is unchanged.
    edges=0
    for p,cell in cells.items():
        q=transformed(p,offset)
        for dx,dy in [(a,b) for a in (-1,0,1) for b in (-1,0,1) if a or b]:
            before=cells.get((p[0]+dx,p[1]+dy));after=translated.get((q[0]+dx,q[1]+dy))
            if before!=after: raise ValueError('Neighbour topology changed')
            if before is not None: edges+=1
    starts=[tuple(p) for p in record['starts']]
    paths=[]; ratio=724*math.sqrt(2)/1024
    for profile,speeds in movement.items():
        for i,p in enumerate(starts):
            targets=starts[i+1:]
            old=shortest_paths(cells,p,targets,speeds,False)
            new_targets=[transformed(t,offset) for t in targets]
            new=shortest_paths(translated,transformed(p,offset),new_targets,speeds,True)
            for j,t in enumerate(targets,i+1):
                a=old[t];b=new[transformed(t,offset)]
                if (a is None)!=(b is None) or a is not None and not math.isclose(b,a*ratio,rel_tol=1e-9,abs_tol=1e-7):
                    raise ValueError('Static terrain route changed')
                paths.append({'profile':profile,'start_one':i+1,'start_two':j+1,
                    'source_weighted_cell_lengths':a,'target_weighted_cell_lengths':b})
    min_u=min(p[0] for p in memory);max_u=max(p[0] for p in memory)
    min_v=min(p[1] for p in memory);max_v=max(p[1] for p in memory)
    backing=[to_mpos(transformed(p,offset)) for p in [(0,0),(width-1,0),(0,height-1),(width-1,height-1)]]
    backing += [tuple(a['target_memory']) for a in actors if 'target_memory' in a]
    if any(p[0]<0 or p[1]<0 for p in backing): raise ValueError('Scenery outside protected backing')
    oil_access=[]
    for actor in actors:
        if actor['type'].lower()!='oilb': continue
        x,y=actor['source_cell']
        # OILB's original and current declared footprint are both xx/xx (2x2).
        goals=[(a,b) for a in range(x-1,x+3) for b in range(y-1,y+3)
               if not (x<=a<x+2 and y<=b<y+2) and (a,b) in cells]
        access=[]
        for start in starts:
            distances=shortest_paths(cells,start,goals,movement['foot'],False)
            access.append(any(d is not None for d in distances.values()))
        oil_access.append({'actor':actor['id'],'source_cell':[x,y],'target_cell':actor['target_cell'],'from_starts':access})
    report={'slug':record['slug'],'title':record['title'],'category':'Handcrafted Maps',
        'status':'logical conversion only; no runtime package or art approval',
        'source_revision':REVISION,'source_bounds':bounds,'source_grid':'Rectangular','target_grid':'RectangularIsometric',
        'cell_translation':list(offset),'target_map_size':[max(p[0] for p in backing)+9,max(p[1] for p in backing)+9],
        'target_enclosing_bounds':[min_u,min_v,max_u-min_u+1,max_v-min_v+1],
        'exterior_policy':'All cells outside the explicitly enumerated original footprint MUST remain unplayable for every movement class.',
        'cell_count':len(cells),'verified_directed_neighbour_edges':edges,'terrain_counts':dict(sorted(Counter(c['terrain'] for c in cells.values()).items())),
        'world_distance_ratio':ratio,'path_scope':'Static terrain-only conservative 8-neighbour graph with original terrain speeds. Excludes actor footprints, bridges, resource slowdown and congestion. Not measured in-game travel time.',
        'paths':paths,'oil_ground_access':oil_access,'actors':actors,
        'columns':['old_x','old_y','new_cx','new_cy','new_u','new_v','terrain','source_tile','source_variant','resource_type','resource_density','height'],
        'cells':rows}
    return report


def main():
    manifest=json.loads((OUT/'manifest.json').read_text(encoding='utf-8'))
    movement=profiles();destination=OUT.parent/'layouts';destination.mkdir(parents=True,exist_ok=True)
    summaries=[]
    for record in manifest['maps']:
        result=convert(record,movement)
        path=destination/(record['slug']+'.layout.json')
        path.write_text(json.dumps(result,ensure_ascii=False,separators=(',',':'))+'\n',encoding='utf-8')
        summaries.append({k:v for k,v in result.items() if k not in ('actors','cells','columns')})
        print(result['slug'],'cells',result['cell_count'],'edges',result['verified_directed_neighbour_edges'],
              'path comparisons',len(result['paths']),'enclosing bounds',result['target_enclosing_bounds'])
    (destination/'audit.json').write_text(json.dumps(summaries,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')


if __name__=='__main__':
    main()
