"""Build Liberty Island separately, without selecting it or touching supplied art."""
from pathlib import Path
import os,subprocess,zipfile,re,json,math,argparse
p=argparse.ArgumentParser();p.add_argument('--output',default='liberty-shellmap');args=p.parse_args()
R=Path(__file__).resolve().parents[1];OUT=R/'mods/ca/maps'/args.output;W=R/'.art-validation/liberty-shellmap'
OUT.mkdir(parents=True,exist_ok=True);W.mkdir(parents=True,exist_ok=True)
env=dict(os.environ,ENGINE_DIR=str(R/'engine'),MOD_SEARCH_PATHS=f'{R/"mods"},{R/"engine/mods"}')
utility=os.environ.get('YMCA_UTILITY',str(R/'.art-validation/shellmap-runtime/bin/OpenRA.Utility.dll'))
subprocess.run(['dotnet',utility,str(R/'mods/ca'),'--prepare-liberty-shellmap',str(W)],env=env,cwd=R,check=True)
with zipfile.ZipFile(W/'archipelago-players-2-seed-1886.oramap') as z:
 text=z.read('map.yaml').decode('utf-8-sig')
 for name in ('map.bin','map.png'):(OUT/name).write_bytes(z.read(name))
terrain_actors=text.split('Actors:',1)[1].split('\nRules:',1)[0].strip('\n')
text=text[:text.index('Players:')]
text=re.sub(r'^Title:.*$', 'Title: Liberty Island - Kirov Assault',text,flags=re.M)
text=re.sub(r'^Visibility:.*$', 'Visibility: 0',text,flags=re.M)
text+='''Players:
	PlayerReference@Neutral:
		Name: Neutral
		OwnsWorld: True
		NonCombatant: True
		Faction: england
	PlayerReference@Creeps:
		Name: Creeps
		NonCombatant: True
		Faction: england
	PlayerReference@Allies:
		Name: Allies
		Faction: england
		Color: 5C9DFF
		Enemies: Raiders
	PlayerReference@Raiders:
		Name: Raiders
		Faction: russia
		Color: E34D43
		Enemies: Allies
Actors:
'''
text+=terrain_actors+'\n'
# Generator's screen-aligned (u,v) plan -> engine isometric cell coordinates.
def cell(u,v):
 u+=29;v+=45;y=(v-(v&1))//2-u;return (v-y,y)
def actor(name,kind,u,v,owner='Allies'):
 global text
 x,y=cell(u,v);text+=f'\t{name}: {kind}\n\t\tOwner: {owner}\n\t\tLocation: {x},{y}\n'
actor('Liberty','liberty.statue',38,31)
flaks=[(36,32),(36,36),(38,40),(40,37),(40,33),(38,28)]
ships=[(round(38+7*math.cos(math.radians(135+i*36))),round(34+14*math.sin(math.radians(135+i*36)))) for i in range(10)]
# Front-to-rear ordering along the incoming southwest attack direction.
ships.sort(key=lambda xy: xy[0]-xy[1]/2)
for i,(x,y) in enumerate(flaks):actor('Flak'+str(i),'agun',x,y)
for i,(x,y) in enumerate(ships):actor('Cruiser'+str(i),'aegis_cruiser',x,y)
actor('CameraAnchor','waypoint',35,39,'Neutral')
text+='\nRules: liberty-rules.yaml\nSequences: liberty-sequences.yaml\nWeapons: liberty-weapons.yaml\n'
(OUT/'map.yaml').write_text(text,encoding='utf-8',newline='\n')
(OUT/'layout.json').write_text(json.dumps({'statue':cell(38,31),'flaks':[cell(*p) for p in flaks],'cruisers':[cell(*p) for p in ships],'spawns':[cell(-25,96)],'fleet_entry':cell(54,-6),'camera':cell(35,39)},indent=2)+'\n')
print(OUT)
