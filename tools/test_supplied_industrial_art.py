"""Regression guards for supplied art cutouts and the repaired oil-platform piers."""
from pathlib import Path
import unittest
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]

class SuppliedIndustrialArtTests(unittest.TestCase):
    def test_oil_upper_structure_is_unchanged(self):
        before=Image.open(ROOT/'art-source/oil-platform/cutout-before-pier-repair.png').convert('RGBA')
        after=Image.open(ROOT/'mods/ca/bits/terrain/environment/oil-platform.png').convert('RGBA')
        self.assertEqual(before.size,after.size)
        regions=[(51,181,80,201),(111,211,144,226),(176,190,204,201)]
        for y in range(after.height):
            for x in range(after.width):
                if not any(x0<=x<=x1 and y0<=y<=y1 for x0,y0,x1,y1 in regions):
                    self.assertEqual(before.getpixel((x,y)),after.getpixel((x,y)),(x,y))

    def test_piers_have_closed_contours_without_alpha_holes(self):
        im=Image.open(ROOT/'mods/ca/bits/terrain/environment/oil-platform.png').convert('RGBA')
        for x0,x1,top,bottom in [(54,76,181,201),(114,141,211,226),(179,201,190,201)]:
            for x in range(x0,x1+1):
                opaque=[y for y in range(top,bottom+1) if im.getpixel((x,y))[3]]
                self.assertTrue(opaque)
                self.assertEqual(opaque,list(range(top,max(opaque)+1)),(x,opaque))
                self.assertLess(max(opaque),bottom)

    def test_water_contact_is_local_soft_and_uses_source_colors(self):
        water=Image.open(ROOT/'art-source/oil-platform/water-contact-retired.png').convert('RGBA')
        source=Image.open(ROOT/'art-source/oil-platform/original.png').convert('RGBA')
        colors={p[:3] for p in source.getdata()}
        regions=[(47,184,83,196),(106,211,149,223),(172,184,208,196)]
        counts=[0,0,0]
        self.assertEqual(water.size,(256,256))
        for y in range(256):
            for x in range(256):
                r,g,b,a=water.getpixel((x,y))
                if not a:continue
                self.assertLessEqual(a,205)
                self.assertIn((r,g,b),colors)
                self.assertGreater(b,r)
                matches=[i for i,(x0,y0,x1,y1) in enumerate(regions) if x0<=x<=x1 and y0<=y<=y1]
                self.assertEqual(len(matches),1,(x,y))
                counts[matches[0]]+=1
        self.assertTrue(all(n>20 for n in counts),counts)

    def test_nuclear_cutout_retains_source_colors_and_canvas(self):
        source=Image.open(ROOT/'art-source/soviet-nuclear-plant/original.png').convert('RGBA')
        cut=Image.open(ROOT/'mods/ca/bits/soviet/buildings/nuclear-power-plant.png').convert('RGBA')
        self.assertEqual(source.size,cut.size)
        for a,b in zip(source.getdata(),cut.getdata()):
            self.assertEqual(a[:3],b[:3])
            self.assertLessEqual(b[3],a[3])
        # Foreground troops/trucks are outside the retained plant silhouette.
        for p in [(89,190),(120,184),(128,205),(178,242),(13,181)]:
            self.assertEqual(cut.getpixel(p)[3],0,p)

if __name__=='__main__':unittest.main()
