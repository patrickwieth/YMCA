"""Internal native-grid assembly test: cut/paste asphalt plus allowed sand color shift.

No runtime tileset registration. Source-cut markings are now applied separately;
continuous four-sided sidewalks/curb joins remain unfinished. All asphalt RGB comes
from a bounded source rectangle; paint is pasted from source, without synthetic noise.
"""
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageChops

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'art-source/urban-warfare'
OUT = ART / 'road-core'
SAND = ROOT / 'mods/ca/bits/terrain/rubberduck/sand_base.png'
ROAD = ROOT / 'mods/ca/bits/misc/buildings/paved_roads-991488041-0.png'
ROAD_CROP = (119,139,135,155)
DIRECTIONS = ((-1,0), (0,1), (1,0), (0,-1))



def road_mask(connections):
    mask = Image.new('L', (128,64))
    pixels = mask.load()
    low, high = .18, .82
    for y in range(64):
        for x in range(128):
            # Pixel-center ownership avoids the transparent one-pixel fringe of
            # the sand sprite exposing snow between adjacent asphalt cells.
            u = ((x+.5-64)/64 + (y+.5)/32)/2
            v = ((y+.5)/32 - (x+.5-64)/64)/2
            if not (0 <= u < 1 and 0 <= v < 1):
                continue
            filled = low <= u <= high and low <= v <= high
            filled |= bool(connections & 1) and u < low and low <= v <= high
            filled |= bool(connections & 2) and v > high and low <= u <= high
            filled |= bool(connections & 4) and u > high and low <= v <= high
            filled |= bool(connections & 8) and v < low and low <= u <= high
            if filled:
                pixels[x,y] = 255
    return mask


def mark_road(tile, connections, catalog, crossing=False):
    """Paste native source dashes/bars; never draw vector replacement paint."""
    overlay = Image.new('RGBA', tile.size)
    instances = []
    def stamp(name, center):
        record = catalog[name]
        sprite = Image.open(ROOT/record['output']).convert('RGBA')
        gx,gy = record['ground_in_cutout']
        at = (center[0]-gx, center[1]-gy)
        overlay.alpha_composite(sprite, at)
        instances.append(dict(asset=name, at=list(at)))
    if crossing and connections == 5:
        # Source stripe runs across the u-axis road. Three overlapping source
        # copies span its width; four separately pasted bars form the crossing.
        for row in range(4):
            for part in (-1,0,1):
                stamp('crosswalk-stripe-rise-east', (82-6*row+12*part,41-3*row-6*part))
    elif connections == 5:
        for point in [(48,24),(80,40)]:
            stamp('street-dash-fall-east', point)
    elif connections == 10:
        for point in [(48,40),(80,24)]:
            stamp('street-dash-rise-east', point)
    overlay.putalpha(ImageChops.multiply(overlay.getchannel('A'),tile.getchannel('A')))
    result = tile.copy()
    result.alpha_composite(overlay)
    return result, instances


def prepare():
    OUT.mkdir(parents=True, exist_ok=True)
    hashes = {p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
              for p in (SAND,ROAD)}
    original = Image.open(SAND).convert('RGBA')
    snow = original.copy()
    # User-authorized terrain-only color shift; preserve spatial detail and alpha.
    snow.putdata([(min(255, ((r*77+g*150+b*29)//256)+59),
                   min(255, ((r*77+g*150+b*29)//256)+72),
                   min(255, ((r*77+g*150+b*29)//256)+79), a)
                  for r,g,b,a in original.getdata()])
    assert snow.getchannel('A').tobytes() == original.getchannel('A').tobytes()
    snow.save(OUT / 'snow-from-sand.png')
    source = Image.open(ROAD).convert('RGBA')
    patch = source.crop(ROAD_CROP)
    plane = Image.new('RGBA', (128,64))
    for y in range(0,64,patch.height):
        for x in range(0,128,patch.width):
            plane.paste(patch, (x,y))
    plane.save(OUT / 'asphalt-source-plane.png')
    tiles = {}
    for connections in range(16):
        tile = plane.copy()
        tile.putalpha(road_mask(connections))
        for pixel, original_pixel in zip(tile.getdata(), plane.getdata()):
            assert not pixel[3] or pixel[:3] == original_pixel[:3]
        tile.save(OUT / f'road-{connections:02x}.png')
        tiles[connections] = tile

    catalog = {r['id']:r for r in json.loads((ART/'cutout-catalog.json').read_text())}
    marked, stamps = {}, {}
    for connections in (5,10):
        marked[connections], stamps[str(connections)] = mark_road(tiles[connections], connections, catalog)
        marked[connections].save(OUT/f'marked-road-{connections:02x}.png')
    crossing, stamps['5-crosswalk'] = mark_road(tiles[5],5,catalog,crossing=True)
    crossing.save(OUT/'marked-road-05-crosswalk.png')

    # Unscaled source assets in a freely recomposed 128x64 native-grid test.
    # A visual development assembly, not evidence of engine passability or balance.
    board = Image.new('RGBA', (1536,864), (34,42,48,255))
    cells = [(u,v) for u in range(11) for v in range(11)]
    roads = {(u,v) for u,v in cells if u in (1,5,9) or v in (1,5,9)}
    def origin(u,v):
        return round((u-v)*64+704), round((u+v)*32+64)
    for u,v in sorted(cells, key=lambda p: sum(p)):
        variant = (u*3+v*5) % 16
        tile = snow.crop(((variant%8)*128,(variant//8)*64,
                          (variant%8+1)*128,(variant//8+1)*64))
        board.alpha_composite(tile, origin(u,v))
    # Overlay after ALL ground cells: neighboring snow-sprite edge pixels must
    # not overpaint a road segment that was drawn earlier in cell order.
    for u,v in sorted(roads, key=lambda p: sum(p)):
        connections = sum(1 << bit for bit,(du,dv) in enumerate(DIRECTIONS)
                          if (u+du,v+dv) in roads)
        surface = marked.get(connections,tiles[connections])
        if connections == 5 and u+1 in (1,5,9):
            surface = crossing
        board.alpha_composite(surface, origin(u,v))
    placements = [
        ('city-shop-block',3,3),
        ('village-cottage-porch',7,3), ('snow-log-house',6.5,8),
        ('village-timber-house',8,7), ('village-barn',8,4),
        ('wooden-utility-hall',4.25,7), ('roofed-stone-well',3,4),
        ('small-well',8,8), ('empty-handcart',7,4),
        ('city-minibus-se',2.25,3.75), ('delivery-truck-se',3,8.25),
        ('city-compact-se',6.25,2.25), ('city-van-se',6.25,6.25),
    ]
    for name,u,v in sorted(placements, key=lambda p:p[1]+p[2]):
        record = catalog[name]
        cut = Image.open(ROOT/record['output']).convert('RGBA')
        x,y = origin(u,v)
        gx,gy = record['ground_in_cutout']
        board.alpha_composite(cut,(x+64-gx,y+32-gy))
    board.save(OUT/'native-grid-assembly.png')
    manifest = dict(status='internal source-marked road assembly; continuous sidewalks/curbs and runtime registration pending',
                    sources=hashes, asphalt_crop=list(ROAD_CROP), tile_size=[128,64],
                    asphalt_method='repeat exact 16x16 source crop; binary grid masks only',
                    snow_method='luma=(77R+150G+29B)//256; RGB=luma+(59,72,79), capped at255; source alpha',
                    connections={'1':'u-','2':'v+','4':'u+','8':'v-'}, placements=placements,
                    marking_stamps=stamps,
                    marking_sources={name:catalog[name] for name in
                        ['street-dash-fall-east','street-dash-rise-east','crosswalk-stripe-rise-east']})
    (OUT/'provenance.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
    for name,digest in hashes.items():
        assert hashlib.sha256((ROOT/name).read_bytes()).hexdigest() == digest
    print('Prepared source-pasted road cores and a separate snow derivative; runtime unchanged.')


if __name__ == '__main__':
    prepare()
