"""Native-density join study using only pasted source sidewalk fragments.

Straight runs only; this is not a completed four-sided sidewalk/junction kit.
Keep the isolated drain-containing strip separate from the plain repeated run.
"""
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT/'art-source/urban-warfare'


def prepare():
    catalog = {r['id']:r for r in json.loads((ART/'cutout-catalog.json').read_text())}
    canvas = Image.new('RGBA',(512,256))
    stamps = []
    for name,start,step in [
        ('sidewalk-fall-east-source',(10,20),(24,12)),
        ('sidewalk-rise-east-curb',(260,180),(16,-8)),
    ]:
        record = catalog[name]
        sprite = Image.open(ROOT/record['output']).convert('RGBA')
        for index in range(8):
            point = (start[0]+step[0]*index,start[1]+step[1]*index)
            canvas.alpha_composite(sprite,point)
            stamps.append(dict(asset=name,at=list(point)))
    output = ART/'road-core'
    output.mkdir(parents=True,exist_ok=True)
    canvas.save(output/'sidewalk-source-runs.png')
    (output/'sidewalk-source-runs.json').write_text(json.dumps(dict(
        status='internal straight-join study; four-sided loops and corners not completed',
        stamps=stamps, sources={s['asset']:catalog[s['asset']] for s in stamps}),indent=2)+'\n')
    review = Image.new('RGBA',canvas.size,(150,170,180,255))
    review.alpha_composite(canvas)
    review.resize((1024,512),Image.Resampling.NEAREST).save(output/'sidewalk-source-runs-review.png')
    print('Pasted source sidewalk runs at native density; no runtime registration.')


if __name__ == '__main__':
    prepare()
