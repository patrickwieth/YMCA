"""Build an autonomous GDI/Soviet shellmap from the real Two Sides generator.
Does not modify any existing map or global gameplay rule.
"""
from pathlib import Path
import os,sys,subprocess,zipfile,re,json
R=Path(__file__).resolve().parents[1];sys.path.insert(0,str(R/'tools'))
from test_soviet_building_art import resolved,traits
OUT=R/'mods/ca/maps/two-sides-shellmap';WORK=R/'.art-validation/two-sides-shellmap'
OUT.mkdir(exist_ok=True);WORK.mkdir(parents=True,exist_ok=True)
source=WORK/'two-sides-players-2-seed-1967.oramap'
if not source.exists():
 env=dict(os.environ,ENGINE_DIR=str(R/'engine'),MOD_SEARCH_PATHS=f'{R/"mods"},{R/"engine/mods"}')
 subprocess.run(['dotnet',os.environ.get('YMCA_UTILITY',str(R/'engine/bin/OpenRA.Utility.dll')),str(R/'mods/ca'),'--debug-map-generator',str(WORK),'mode=tactical','preset=two-sides','seed=1967','players=2','teams=2','size=96x96','resources=patches','finite=false','tech=none','expansions=1','final-only=true','export-map=true'],cwd=R,env=env,check=True)
with zipfile.ZipFile(source) as z:
 text=z.read('map.yaml').decode('utf-8-sig')
 for name in ('map.bin','map.png'):(OUT/name).write_bytes(z.read(name))
starts=re.findall(r'\tActor[01]: mpspawn\n\t\tLocation: ([^\n]+)\n\t\tOwner: Neutral',text)
assert len(starts)==2
starts=[tuple(map(int,p.split(','))) for p in starts]
players='''Players:
\tPlayerReference@Neutral:
\t\tName: Neutral
\t\tOwnsWorld: True
\t\tNonCombatant: True
\t\tFaction: RandomAllies
\tPlayerReference@Creeps:
\t\tName: Creeps
\t\tNonCombatant: True
\t\tFaction: RandomAllies
\tPlayerReference@GDI:
\t\tName: GDI
\t\tFaction: talon
\t\tColor: F4D76A
\t\tBot: normal
\t\tEnemies: Soviets
\tPlayerReference@Soviets:
\t\tName: Soviets
\t\tFaction: russia
\t\tColor: E34D43
\t\tBot: normal
\t\tEnemies: GDI
'''
text=re.sub(r'^Title:.*$', 'Title: Two Sides - GDI vs Soviets',text,flags=re.M)
text=re.sub(r'^Visibility:.*$', 'Visibility: 0',text,flags=re.M)
text=re.sub(r'^Categories:.*$', 'Categories: Shellmap, Tactical',text,flags=re.M)
text=text[:text.index('Players:')]+players+'\n'+text[text.index('Actors:'):]
text=text[:text.index('\nRules:')]
for i,name in enumerate(('GDIStart','SovietStart')):text=text.replace(f'\tActor{i}: mpspawn',f'\t{name}: waypoint')
layouts=[('GDI',[('Conyard','afac',0,0),('Power','nuke',-6,0),('Barracks','pyle',0,6),('Factory','weap.td',0,-6),('Guard','gtwr',5,5),('Proxy','gdi.dummy',0,0)]),('Soviets',[('Conyard','fact.soviet',0,0),('Power','powr.soviet',6,0),('Barracks','barr',0,-6),('Factory','weap.soviet',0,6),('Guard','ftur',-5,-5),('Proxy','soviet.dummy',0,0)])]
for (owner,actors),(x,y) in zip(layouts,starts):
 for suffix,actor,dx,dy in actors:
  text+=f'\n\t{owner}{suffix}: {actor}\n\t\tOwner: {owner}\n'
  if suffix!='Proxy':text+=f'\t\tLocation: {x+dx},{y+dy}\n'
text+='\nRules: shellmap-rules.yaml\n'
(OUT/'map.yaml').write_text(text,encoding='utf-8',newline='\n')
# Override AI choice dictionaries map-locally, preserving the ordinary skirmish AI.
player=traits(resolved('--resolved-rules','Player'))
def dictionary(trait,field):
 match=re.search(r'^\t'+re.escape(field)+r':[^\n]*\n((?:\t\t[^\n]*\n?)*)',player[trait],re.M)
 return {line.strip().split(':')[0]:0 for line in match[1].splitlines()} if match else {}
fractions=dictionary('BaseBuilderBotModuleCA@normal','BuildingFractions')
fractions.update({'nuke':12,'powr.soviet':12,'proc.td':20,'soviet_refinery':20,'pyle':4,'barr':4,'weap.td':10,'weap.soviet':10,'hq':5,'dome.soviet':5,'gtek':2,'stek':2,'gtwr':4,'ftur':4,'atwr':2,'tsla':2})
limits=dictionary('BaseBuilderBotModuleCA@normal','BuildingLimits')
limits.update({'nuke':3,'powr.soviet':3,'proc.td':1,'soviet_refinery':1,'pyle':1,'barr':1,'weap.td':2,'weap.soviet':2,'hq':1,'dome.soviet':1,'gtek':1,'stek':1,'gtwr':2,'ftur':2,'atwr':1,'tsla':1})
units=dictionary('UnitBuilderBotModuleCA@normal','UnitsToBuild')
units.update({'mercenary':35,'rocket_mercenary':15,'grenadier':10,'hmmv':15,'mtnk':35,'mammoth':6,'mlrs':6,'conscript':35,'rpg_soldier':15,'flamethrower':10,'heavy_tank':35,'btr':15,'v2rl':6,'harv':8,'har2':8,'soviet_miner':8})
rules='''# Self-contained menu battle; no multiplayer balance changes.
World:
\t-CrateSpawner:
\t-SpawnStartingUnits:
\t-StartGameNotification:
\t-TimeLimitManager:
\tLuaScript:
\t\tScripts: shellmap.lua
\tShellmapBattleMonitor:
\tMusicPlaylist:
\t\tDisableWorldSounds: true
Player:
\t-ConquestVictoryConditions:
\t-CheckpointVictoryConditions:
\tPlayerResources:
\t\tDefaultCash: 20000
\tShroud:
\t\tFogCheckboxEnabled: false
\t\tFogCheckboxLocked: true
\t\tExploredMapCheckboxEnabled: true
\t\tExploredMapCheckboxLocked: true
\tSupportPowerBotModule:
\t\tRequiresCondition: false
\tCaptureManagerBotModuleCA:
\t\tRequiresCondition: false
\tMcvManagerBotModule@lower:
\t\tMinimumConstructionYardCount: 1
\tHarvesterBotModuleCA:
\t\tHarvesterTypes: harv, har2, soviet_miner
\t\tRefineryTypes: proc.td, soviet_refinery
\tBaseBuilderBotModuleCA@normal:
\t\tBuildingQueues: Building, Defense
\t\tVehiclesFactoryTypes: weap.td, weap.soviet
\t\tMaxBaseRadius: 16
\t\tBuildingProductionMinCashRequirement: 500
\t\tBuildingDelays:
\t\t\thq: 750
\t\t\tdome.soviet: 750
\t\t\tgtek: 1500
\t\t\tstek: 1500
'''
for key,data in [('BuildingFractions',fractions),('BuildingLimits',limits)]:
 rules+='\t\t'+key+':\n'+''.join(f'\t\t\t{k}: {v}\n' for k,v in data.items())
rules+='''\tUnitBuilderBotModuleCA@normal:
\t\tIdleBaseUnitsMaximum: 30
\t\tMaxAircraft: 0
\t\tUnitQueues: Infantry, Vehicle
\t\tUnitsToBuild:
'''+''.join(f'\t\t\t{k}: {v}\n' for k,v in units.items())
rules+='''\tSquadManagerBotModule@normal:
\t\tMinimumAttackForceDelay: 250
\t\tSquadSize: 6
\t\tSquadSizeRandomBonus: 3
'''
for actor in ('AFAC','FACT.Soviet','GTWR','FTUR'):
 rules+=actor+':\n\tExternalCondition@SHELLMAPCORE:\n\t\tCondition: shellmap-core\n\tDamageMultiplier@SHELLMAPCORE:\n\t\tRequiresCondition: shellmap-core\n\t\tModifier: 0\n'
(OUT/'shellmap-rules.yaml').write_text(rules,encoding='utf-8',newline='\n')
(OUT/'generation.json').write_text(json.dumps({'preset':'two-sides','mode':'tactical','seed':1967,'size':[96,96],'players':2,'starts':starts,'original_map':source.name,'purpose':'Autonomous, bounded GDI versus Soviet menu battle.'},indent=2)+'\n')
print(OUT)
