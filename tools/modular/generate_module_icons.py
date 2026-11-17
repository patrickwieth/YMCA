"""Original 64x48 military pixel art for the vehicle designer (Pillow, no external assets).
Visual references only: GDI vehicle_armor / energy_weapons and Nod improved_lasers PNGs.
Hand-authored integer-pixel geometry, restricted material palette, no antialiasing.
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'mods/ca/bits/modular'
IDS = ('weapon', 'engine', 'generator', 'gear', 'ammo', 'armor', 'battery', 'pdl', 'reflector')
P = {'ink':'#0b1018','shadow':'#17212b','dark':'#293742','steel':'#50616a','light':'#8b9b9b','edge':'#d1d5b6',
     'olive':'#686e45','olive_dark':'#3e4833','olive_light':'#9b9b62','copper':'#a46b39','gold':'#d6ac5d',
     'red':'#c65749','green':'#72b779','blue':'#699bc4','violet':'#b087cd','cyan':'#74d8dd','white':'#e2fbef'}

class Art:
    def __init__(self):
        self.image = Image.new('RGBA', (64,48)); self.d = ImageDraw.Draw(self.image)
    def color(self,c): return P.get(c,c)
    def r(self,box,c): self.d.rectangle(box,fill=self.color(c))
    def p(self,points,c,outline='ink'): self.d.polygon(points,fill=self.color(c),outline=self.color(outline) if outline else None)
    def l(self,points,c,w=1): self.d.line(points,fill=self.color(c),width=w)
    def e(self,box,c,outline='ink'): self.d.ellipse(box,fill=self.color(c),outline=self.color(outline) if outline else None)
    def bolt(self,x,y):
        self.r((x,y,x+2,y+2),'ink'); self.r((x,y,x+1,y),'light'); self.r((x+1,y+1,x+1,y+1),'steel')
    def box(self,x,y,w,h,front='olive',depth=5):
        self.p([(x,y),(x+depth,y-depth),(x+w+depth,y-depth),(x+w,y)],'olive_light' if front=='olive' else 'light')
        self.p([(x+w,y),(x+w+depth,y-depth),(x+w+depth,y+h-depth),(x+w,y+h)],'olive_dark' if front=='olive' else 'dark')
        self.p([(x,y),(x+w,y),(x+w,y+h),(x,y+h)],front)
        self.l([(x+1,y+1),(x+w-1,y+1)],'edge')
    def shadow(self):
        self.e((6,38,58,45),(5,9,14,115),None)


def weapon():
    a=Art(); a.shadow()
    a.p([(7,31),(19,22),(35,26),(40,37),(25,44),(9,40)],'dark')
    a.p([(9,29),(18,23),(32,28),(23,35)],'olive_light')
    a.p([(9,29),(23,35),(23,43),(9,39)],'olive_dark')
    a.p([(23,35),(32,28),(37,34),(25,43)],'olive')
    a.e((16,27,31,39),'steel'); a.e((20,29,28,37),'dark')
    # Breech and slanted smoothbore barrel, lit along its top-left ridge.
    a.p([(19,27),(26,19),(38,21),(33,33),(24,36)],'steel')
    a.p([(25,20),(52,5),(60,8),(34,27)],'dark')
    a.p([(27,20),(53,6),(57,8),(32,25)],'light')
    a.l([(28,20),(52,7)],'edge'); a.l([(33,25),(58,10)],'steel',2)
    a.p([(50,6),(56,3),(62,6),(60,12),(55,14),(51,10)],'steel')
    a.p([(56,5),(61,6),(59,10),(55,12),(54,8)],'ink')
    a.l([(56,4),(61,6)],'edge'); a.l([(28,27),(34,29)],'edge')
    a.p([(18,24),(22,21),(27,23),(23,28)],'olive'); a.bolt(20,23)
    for x,y in [(29,29),(12,34),(26,38)]: a.bolt(x,y)
    a.l([(8,40),(23,45),(38,38)],'shadow'); return a.image


def engine():
    a=Art(); a.shadow(); a.box(12,23,34,17,'olive')
    a.p([(14,23),(20,15),(35,15),(31,27)],'dark')
    a.p([(31,27),(35,15),(48,20),(43,32)],'steel')
    for i in range(4):
        x=16+i*6
        a.p([(x,20),(x+3,13),(x+7,14),(x+4,22)],'light')
        a.l([(x+1,20),(x+4,14)],'edge')
        a.p([(x+2,25),(x+5,20),(x+9,23),(x+6,28)],'steel')
        a.l([(x+3,25),(x+6,21)],'light')
    a.box(19,11,21,6,'dark',3)
    for x in range(22,39,4): a.l([(x,10),(x,14)],'steel')
    a.e((8,28,20,40),'shadow'); a.e((11,30,18,37),'steel'); a.e((13,32,16,35),'ink')
    a.l([(14,29),(23,23),(29,26),(16,39)],'ink',2)
    a.l([(43,23),(53,23),(54,33),(48,36)],'ink',3); a.l([(43,23),(52,24),(52,31)],'copper')
    a.r((34,31,42,33),'green'); a.r((34,31,40,31),'edge')
    a.r((8,20,13,29),'steel'); a.r((8,20,13,21),'edge')
    for x in (24,30,38): a.bolt(x,37)
    return a.image


def generator():
    a=Art(); a.shadow(); a.box(27,16,23,22,'olive')
    a.p([(20,18),(36,13),(45,18),(44,33),(25,40),(15,33)],'dark')
    for x in range(27,43,3): a.l([(x,18),(x,32)],'copper')
    a.e((7,18,32,42),'steel'); a.e((9,20,29,39),'shadow')
    for pts in [[(18,29),(12,22),(21,21)],[(20,29),(26,23),(27,31)],[(20,31),(25,36),(18,38)],[(17,31),(11,35),(11,27)]]:
        a.p(pts,'light','dark')
    a.e((15,27,22,34),'olive'); a.bolt(17,29)
    a.box(38,10,13,12,'olive',3)
    a.r((40,13,48,18),'ink'); a.r((41,14,46,16),'green'); a.r((41,14,43,14),'white')
    a.l([(48,27),(55,29),(54,38),(34,42)],'ink',3); a.l([(48,27),(54,30),(53,37)],'copper')
    a.bolt(47,31); a.l([(6,42),(32,45),(54,39)],'steel',2)
    return a.image


def gear():
    a=Art(); a.shadow()
    a.p([(3,25),(11,15),(50,15),(61,24),(60,36),(50,44),(12,44),(3,36)],'ink')
    a.p([(6,26),(13,18),(49,18),(58,26),(57,34),(48,41),(13,41),(6,34)],'steel')
    a.p([(9,27),(15,22),(48,22),(54,27),(53,33),(47,37),(15,37),(9,32)],'shadow')
    for x in range(12,49,9):
        a.e((x,24,x+10,36),'olive'); a.e((x+2,26,x+8,34),'olive_dark'); a.e((x+4,28,x+6,31),'light')
        a.l([(x+2,26),(x+6,25)],'olive_light')
    for x in range(12,52,5):
        a.r((x,16,x+3,20),'dark'); a.l([(x,16),(x+3,16)],'light')
        a.r((x,39,x+3,43),'dark'); a.l([(x,39),(x+3,39)],'light')
    for x,y in [(4,25),(4,31),(7,36),(54,21),(57,27),(55,34)]:
        a.r((x,y,x+3,y+3),'dark'); a.r((x,y,x+3,y),'light')
    a.p([(12,14),(21,9),(50,9),(55,13),(48,16),(16,16)],'olive')
    a.l([(21,10),(48,10)],'olive_light'); a.bolt(26,12); a.bolt(42,12)
    return a.image


def ammo():
    a=Art(); a.shadow(); a.box(9,28,37,15,'olive',7)
    # Three brass rounds, distinct from the crate and purple magazine stripe.
    for x,y in [(17,8),(30,5),(42,9)]:
        a.p([(x,y),(x+3,y-4),(x+6,y),(x+7,y+22),(x-1,y+22)],'copper')
        a.p([(x,y),(x+3,y-4),(x+5,y),(x+5,y+6),(x,y+6)],'steel')
        a.r((x,y+7,x+5,y+20),'gold'); a.l([(x+1,y+7),(x+1,y+19)],'edge')
        a.r((x-1,y+19,x+6,y+22),'copper'); a.r((x,y+19,x+5,y+19),'gold')
        a.r((x,y+8,x+5,y+10),'violet')
    a.p([(9,29),(46,29),(46,42),(9,42)],'olive')
    a.l([(10,30),(45,30)],'olive_light'); a.r((11,34,44,37),'olive_dark')
    a.r((23,32,33,38),'violet'); a.r((25,33,30,34),'light')
    for x in (11,40): a.bolt(x,32); a.bolt(x,38)
    return a.image


def armor():
    a=Art(); a.shadow()
    for dx,dy,c in [(0,6,'shadow'),(2,3,'steel'),(4,0,'olive')]:
        a.p([(7+dx,16+dy),(21+dx,6+dy),(47+dx,11+dy),(54+dx,30+dy),(38+dx,39+dy),(10+dx,33+dy)],c)
        a.l([(8+dx,16+dy),(21+dx,7+dy),(46+dx,12+dy)],'light')
    a.p([(15,17),(27,12),(43,15),(49,29),(37,34),(18,30)],'olive_dark')
    a.l([(17,17),(28,13),(42,16)],'olive_light')
    a.p([(17,22),(46,25),(47,29),(18,26)],'blue',None)
    for x,y in [(13,17),(25,9),(49,16),(47,31),(17,31)]: a.bolt(x,y)
    a.l([(26,17),(30,18)],'olive'); a.l([(37,29),(41,30)],'light')
    return a.image


def battery():
    a=Art(); a.shadow(); a.box(15,12,29,29,'olive',7)
    a.r((20,7,25,11),'ink'); a.r((21,6,25,8),'copper'); a.r((22,6,24,6),'gold')
    a.r((36,6,41,10),'ink'); a.r((37,5,41,7),'red')
    a.l([(21,7),(14,4),(9,7),(9,19),(15,22)],'ink',3); a.l([(21,7),(14,5),(10,8),(10,18)],'copper')
    a.r((19,16,39,35),'shadow')
    for x in (21,27,33):
        a.r((x,18,x+4,31),'steel'); a.r((x,18,x+3,18),'light')
        a.r((x+1,21,x+2,28),'cyan'); a.r((x+1,21,x+1,24),'white')
    a.p([(29,20),(25,26),(29,26),(27,31),(34,23),(30,23)],'gold')
    a.r((17,37,42,39),'dark')
    for x in range(18,41,6): a.l([(x,37),(x+2,39)],'gold',2)
    for x,y in [(17,13),(39,13),(16,32),(40,33)]: a.bolt(x,y)
    return a.image


def pdl():
    a=Art(); a.shadow()
    a.p([(13,37),(23,31),(42,33),(51,40),(36,45),(16,43)],'olive')
    a.p([(23,31),(28,23),(38,23),(42,33),(31,38)],'steel')
    a.e((21,26,40,37),'dark'); a.e((24,27,36,34),'light')
    a.p([(17,23),(22,14),(37,9),(48,16),(42,28),(29,32)],'olive_dark')
    a.p([(22,14),(37,9),(45,13),(31,20)],'olive_light')
    a.p([(31,20),(45,13),(48,17),(43,27),(32,31)],'steel')
    a.p([(37,18),(44,14),(51,18),(47,27),(40,29),(35,25)],'dark')
    a.p([(41,19),(45,17),(48,20),(45,25),(40,26),(38,23)],'cyan')
    a.l([(41,21),(45,19),(46,21),(43,24)],'white',2)
    a.l([(23,18),(29,20)],'light'); a.l([(21,21),(27,23)],'steel')
    a.box(12,16,8,8,'dark',2); a.r((13,17,17,20),'red')
    a.l([(30,10),(29,3)],'steel'); a.r((28,2,30,3),'cyan')
    a.bolt(20,38); a.bolt(40,39); return a.image


def reflector():
    a=Art(); a.shadow()
    a.p([(8,16),(21,4),(46,8),(57,26),(46,41),(21,42),(9,31)],'dark')
    a.p([(12,17),(23,8),(43,11),(52,26),(43,36),(23,38),(13,29)],'blue')
    a.p([(23,8),(30,21),(12,17)],'light')
    a.p([(23,8),(43,11),(30,21)],'cyan')
    a.p([(43,11),(52,26),(30,21)],'steel')
    a.p([(12,17),(30,21),(23,38),(13,29)],'dark')
    a.p([(30,21),(52,26),(43,36),(23,38)],'blue')
    a.l([(12,17),(30,21),(43,11)],'white')
    a.l([(30,21),(23,37)],'cyan'); a.l([(30,21),(51,26)],'light')
    for x,y in [(10,19),(23,6),(47,16),(45,36),(19,36)]: a.bolt(x,y)
    a.l([(35,10),(35,18)],'white'); a.l([(32,14),(39,14)],'white')
    return a.image


DRAW = dict(zip(IDS, (weapon, engine, generator, gear, ammo, armor, battery, pdl, reflector)))

def render(): return {id: DRAW[id]() for id in IDS}

def atlas(images):
    # ChromeProvider uploads the whole PNG directly; texture dimensions must be
    # powers of two. Keep the 192x144 icon region unchanged and pad transparently.
    sheet = Image.new('RGBA', (256,256))
    for i,id in enumerate(IDS): sheet.paste(images[id], (i%3*64, i//3*48))
    return sheet

def contact_sheet(images):
    # Nearest-neighbor review enlargement, not a runtime asset.
    sheet = Image.new('RGB', (648, 558), '#202c34'); d=ImageDraw.Draw(sheet)
    for i,id in enumerate(IDS):
        x=12+i%3*216; y=12+i//3*186
        sheet.paste(images[id].resize((192,144), Image.Resampling.NEAREST), (x,y), images[id].resize((192,144), Image.Resampling.NEAREST))
        d.text((x+4,y+150), {'weapon':'105mm Smoothbore Cannon', 'gear':'Heavy Tank Treads', 'pdl':'PDL', 'ammo':'Ammunition'}.get(id,id.title()), fill='#d1d5b6')
    return sheet

def main():
    images=render(); OUT.mkdir(parents=True,exist_ok=True)
    for id,image in images.items(): image.save(OUT/f'{id}.png')
    atlas(images).save(OUT/'module-icons.png')
    preview=ROOT/'docs/modular/module-icons-preview.png'; contact_sheet(images).save(preview)
    print(preview)

if __name__ == '__main__': main()
