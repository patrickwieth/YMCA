"""Apply reviewed nearest-grid actor-anchor translations, never independent bib motion.
The actor origin moves body, bib and attachments together. Occupancy/exit cells and
all local weapon/overlay offsets remain unchanged. Explicit entries also protect
already-aligned inherited variants from parent translations.
"""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'art-source/bib-grid-alignment'
geometry={e['Actor']:e for e in json.loads((OUT/'resolved-before.json').read_text())}
audit=json.loads((OUT/'grid-audit.json').read_text());rules=['# Actual128x64 grid registration. Shared actor anchors move body/bib/attachments together.'];records=[]
for row in audit:
 assert not row.get('needs_manual_plane_definition'),row
 g=geometry[row['actor']];dx,dy,dz=[round(v) for v in row['proposed_world_translation']];old=g['BuildingCenterOffset'];new=[old[0]+dx,old[1]+dy,old[2]+dz]
 rules.extend([g['RuleKey']+':','\tBuilding:','\t\tLocalCenterOffset: '+','.join(map(str,new))])
 decoration=g.get('DecorationOffset',[0,0,0]);fallback=g['DecorationBounds'] is None and g['InteractionBounds'] is None
 if fallback:
  decoration=[decoration[0]+dx,decoration[1]+dy,decoration[2]+dz]
 # Pin even zero offsets so a translated parent cannot silently move a variant twice.
 rules.extend(['\tIsometricSelectable:','\t\tDecorationOffset: '+','.join(map(str,decoration))])
 rules.append('');records.append({'actor':row['actor'],'old_center_offset':old,'new_center_offset':new,'world_translation':[dx,dy,dz],'screen_translation':[dx*128/1448,(dy-dz)*64/1448],'decoration_offset':decoration,'preserved_non_anchor_rule_hash':g['GameplayAndAttachmentsHash']})
(ROOT/'mods/ca/rules/bib-grid-alignment.yaml').write_text('\n'.join(rules)+'\n')
(OUT/'applied.json').write_text(json.dumps(records,indent=2)+'\n')
