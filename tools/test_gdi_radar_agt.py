"""Guard tower foundation and bounded radar gold matching regressions."""
import hashlib
import json
import unittest
from PIL import Image
from art.match_gdi_radar_gold import ROOT, OUT, RUNTIME, RAMP, recolor


class GdiRadarAgtTests(unittest.TestCase):
    def test_radar_only_authored_gold_changes(self):
        source=Image.open(OUT/'radar-original.png').convert('RGBA')
        runtime=Image.open(RUNTIME).convert('RGBA')
        expected,count=recolor(source)
        self.assertEqual(runtime.size,source.size)
        self.assertEqual(runtime.tobytes(),expected.tobytes())
        self.assertEqual(runtime.getchannel('A').tobytes(),source.getchannel('A').tobytes())
        self.assertGreater(count,0)
        for a,b in zip(source.getdata(),runtime.getdata()):
            if a==b:continue
            self.assertTrue(any(max(abs(a[i]-c[i]) for i in range(3))<=2 for c in RAMP))
        reference=set()
        for name in ['conyard.png','barracks.png']:
            reference.update(p[:3] for p in Image.open(RUNTIME.parent/name).convert('RGBA').getdata() if p[3])
        self.assertTrue(set(RAMP.values()).issubset(reference))
        manifest=json.loads((OUT/'manifest.json').read_text())
        self.assertEqual(hashlib.sha256((OUT/'radar-original.png').read_bytes()).hexdigest(),manifest['source_sha256'])
        self.assertEqual(hashlib.sha256(RUNTIME.read_bytes()).hexdigest(),manifest['result_sha256'])

    def test_agt_uses_current_gdi_foundation(self):
        rules=(ROOT/'mods/ca/rules/gdi/missile-tower-art.yaml').read_text()
        self.assertIn('Inherits@BIBCOLOR: ^GdiBibCornerColor',rules)
        self.assertIn('WithIdleOverlay@GDIBIB:\n\t\tImage: gdibib1x1\n\t\tSequence: idle\n\t\tRequiresCondition: !build-incomplete\n\t\tIsDecoration: true',rules)
        self.assertIn('-WithBuildingBib:',rules)


if __name__=='__main__':
    unittest.main()
