"""Promote approved native Scrin bib cuts; decorative overlays only."""
from pathlib import Path
import json,hashlib,shutil,sys
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'art-source/scrin bib';OUT=ART/'integration'
sys.path.insert(0,str(ROOT/'tools'));from test_soviet_building_art import traits
before=json.loads((OUT/'rules-before.json').read_text())
sizes={'SFAC':3,'REAC':2,'REA2':3,'PROC.SCRIN':3,'PORT':2,'WSPH':3,'NERV':2,'SCRT':2,'GRAV':3,'SIGN':3,'SREP':3,'SILO.SCRIN':1,'MANI':2,'RIFT':3,'PTUR':1,'SHAR':1,'SCOL':1}
seq=['# Native cut/paste plating. Top surface is the tile-registration plane.'];rules=['# Decorative Scrin plating only: no occupancy, production, docking or weapon changes.'];records={}
manifest=json.loads((ART/'cut-paste-manifest.json').read_text())
for e in manifest['outputs']:
 n=e['cells'][0];name=e['name'];file='bits/scrin/buildings/'+name
 shutil.copy2(ART/name,ROOT/'mods/ca'/file)
 # Align the TOP surface to the existing building contact plane. The visible
 # side lip extends below it; the former -1 candidate referred to the lower lip.
 w,h=e['canvas'];oy=h-w/4-e['top_surface_center'][1];assert oy==23
 seq.extend([f'scrinbib{n}x{n}:','\tidle:','\t\tFilename: '+file,'\t\tStart: 0','\t\tLength: 1','\t\tFacings: 1','\t\tScale: 1','\t\tAnchor: Bottom',f'\t\tOffset: 0,{oy:g}','\t\tZOffset: -4096',''])
for actor,n in sizes.items():
 rules.append(actor+':')
 if 'WithBuildingBib' in traits(before[actor]):rules.append('\t-WithBuildingBib:')
 rules.extend(['\tWithIdleOverlay@SCRINBIB:',f'\t\tImage: scrinbib{n}x{n}','\t\tSequence: idle','\t\tPalette: effect','\t\tRequiresCondition: !build-incomplete','\t\tIsDecoration: true',''])
 records[actor]={'image':f'scrinbib{n}x{n}','file':f'bits/scrin/buildings/bib{n}x{n}.png','size':n}
for actor,parent in [('SFAC.Nukular','SFAC'),('SCOL.Temp','SCOL')]:records[actor]=dict(records[parent])
(ROOT/'mods/ca/sequences/scrin-bibs.yaml').write_text('\n'.join(seq)+'\n')
(ROOT/'mods/ca/rules/scrin/bibs.yaml').write_text('\n'.join(rules)+'\n')
(OUT/'binding.json').write_text(json.dumps({'actors':records,'hashes':{e['name']:hashlib.sha256((ART/e['name']).read_bytes()).hexdigest() for e in manifest['outputs']},'surface_anchor':'Top surface center at actor render origin. Bottom0,23; lower side lip is not used as the tile plane. Global actor anchors are subsequently registered by rules/bib-grid-alignment.yaml.'},indent=2)+'\n')
