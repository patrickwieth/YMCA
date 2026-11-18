"""Corner surfaces only for GDI; lower vertical faces only for Nod."""
import hashlib
import json
import math
import unittest
from PIL import Image
from art.generate_bib_color_variants import ROOT, OUT, CONCRETE, NOD_SIDES


class BibColorVariantsTests(unittest.TestCase):
    def test_masks(self):
        entries = json.loads((OUT/'manifest.json').read_text())
        self.assertEqual(len(entries),9)
        for e in entries:
            with self.subTest(mask=e['mask']):
                folder=ROOT/f"mods/ca/bits/{e['family']}/buildings"
                source=Image.open(folder/e['source']).convert('RGBA')
                indexed=Image.open(folder/e['mask'])
                mask=indexed.convert('RGBA')
                self.assertEqual(indexed.mode,'P')
                self.assertEqual(mask.size,source.size)
                self.assertGreater(e['pixels'],0)
                for key in ['source','mask']:
                    self.assertEqual(hashlib.sha256((folder/e[key]).read_bytes()).hexdigest(),e[key+'_sha256'])
                for x in range(mask.width):
                    occupied=[y for y in range(source.height) if source.getpixel((x,y))[3]]
                    for y in range(mask.height):
                        if not mask.getpixel((x,y))[3]: continue
                        self.assertEqual(mask.getpixel((x,y))[3],255)
                        self.assertEqual(source.getpixel((x,y))[3],255)
                        rgb=source.getpixel((x,y))[:3]
                        if e['family']=='gdi':
                            self.assertEqual(rgb,CONCRETE) # no joint pixels
                            nx,ny=e['cells']
                            panel_x=math.floor((y+.5)/32 + (x+.5-ny*64)/64)
                            panel_y=math.floor((y+.5)/32 - (x+.5-ny*64)/64)
                            self.assertIn(panel_x,(0,2*nx-1))
                            self.assertIn(panel_y,(0,2*ny-1))
                        else:
                            self.assertIn(rgb,NOD_SIDES)
                            self.assertLessEqual(max(occupied)-y,6) # no top plates
                if e['family']=='gdi':
                    nx,ny=e['cells']
                    # Centers of the four outer half-cell panels must all be colored.
                    for u,v in [(.25,.25),(nx-.25,.25),(.25,ny-.25),(nx-.25,ny-.25)]:
                        x,y=round(ny*64+(u-v)*64),round((u+v)*32)
                        self.assertEqual(mask.getpixel((x,y))[3],255)
                    if nx>1:
                        self.assertEqual(mask.getpixel((ny*64,48))[3],0)
                    # Every original dark joint remains unmasked, not just sampled ones.
                    for s,m in zip(source.getdata(),mask.getdata()):
                        if s[:3]!=CONCRETE: self.assertEqual(m[3],0)


if __name__=='__main__':
    unittest.main()
