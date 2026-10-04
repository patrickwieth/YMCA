"""Read-only resolved air/naval inventory. Never enables production or designer bindings."""
import argparse
from collections import Counter
import json
import os
from pathlib import Path
import subprocess
from audit_reference_changes import blocks
from survey_vehicles import local_fields, FACTIONS
from list_missing_vehicles import REPO
from survey_loadouts import source_hash, pack, unpack, summary, selected

SNAPSHOT = Path(__file__).with_name('air-naval-loadouts.json')
REPORT = REPO / 'docs/modular/existing-air-naval-loadouts.md'
PATHS = [f'mods/ca/rules/{f}/aircraft.yaml' for f in FACTIONS] + [
    'mods/ca/rules/aircraft.yaml', 'mods/ca/rules/misc/aircraft.yaml', 'mods/ca/rules/misc/ships.yaml']


def declarations():
    result = []
    for source in PATHS:
        for actor, block in blocks((REPO/source).read_text(encoding='utf-8-sig')).items():
            if actor.startswith('^'): continue
            fields, parents, traits = local_fields(block)
            result.append(dict(actor=actor, source=source, parents=parents,
                local_buildable='Buildable' in traits, label=fields.get('Tooltip.Name', actor)))
    ids = [r['actor'].lower() for r in result]
    if len(set(ids)) != len(ids): raise ValueError('Ambiguous actor declarations: record overrides explicitly before exporting')
    return sorted(result, key=lambda r: (r['source'], r['actor']))


def classify(row):
    types = row['trait_types']
    # Ordered structural roles, not a "has Aircraft therefore is a buildable airplane" guess.
    if any('Husk' in t for t in types) or any('Husk' in p for p in row['parents']): return 'wreck'
    if any(t.startswith('MissileSpawnerSlave') for t in types): return 'spawned-missile'
    if any(t.startswith(('CarrierSlave', 'DroneSpawnerSlave')) for t in types): return 'carrier/drone-slave'
    if row['movement_kind'] == 'non-mobile': return 'non-mobile/review'
    if row['production_status'] == 'disabled': return 'disabled-vehicle'
    if row['production_status'] == 'bot-gated': return 'bot-gated-vehicle'
    if row['production_status'] == 'gated-candidate': return 'production-candidate'
    return 'non-production/support/review'


def refresh():
    metadata = declarations()
    env = dict(os.environ, ENGINE_DIR=str(REPO/'engine'), MOD_SEARCH_PATHS=','.join(str(REPO/p) for p in ('mods','engine/mods')))
    result = subprocess.run(['dotnet', str(REPO/'engine/bin/OpenRA.Utility.dll'), str(REPO/'mods/ca'), '--export-vehicle-loadouts']+
        [r['actor'] for r in metadata], cwd=REPO, env=env, capture_output=True, text=True, check=True)
    rows = json.loads(result.stdout)
    if [r['actor'] for r in rows] != [r['actor'] for r in metadata]: raise ValueError('Actor export mismatch')
    for row, meta in zip(rows, metadata):
        row.update(meta); row['summary'] = summary(row); row['category'] = classify(row)
    data = dict(schema=1, source_sha256=source_hash(), scope='nine aircraft/naval declaration files, including nonproduction objects and upgrades', actors=rows)
    SNAPSHOT.write_text(json.dumps(pack(data), indent=2, sort_keys=True, ensure_ascii=False)+'\n', encoding='utf-8')
    return data


def load():
    return unpack(json.loads(SNAPSHOT.read_text(encoding='utf-8')))


def features(row):
    result = []
    if selected(row, 'Carryall'): result.append('Carryall')
    if selected(row, 'AutoCarryall'): result.append('AutoCarryall')
    if selected(row, 'Cargo'): result.append('Cargo')
    if selected(row, 'Rearmable'): result.append('Rearm')
    if selected(row, 'CarrierMaster'): result.append('CarrierMaster')
    if selected(row, 'DroneSpawnerMaster'): result.append('DroneSpawner')
    if selected(row, 'MissileSpawnerMaster'): result.append('MissileSpawner')
    if row['summary']['pdl']: result.append('PDL')
    if row['summary']['armor'] == 'Reflector': result.append('Reflector')
    return ', '.join(result) or '—'


def report(data):
    rows = data['actors']; categories = Counter(r['category'] for r in rows)
    lines = ['# Bestehende Luft- und Wasserfahrzeuge: aufgelöste Aufbauten', '',
        f'**{len(rows)} konkrete Actor-Deklarationen** aus neun Flugzeug-/Schiffsdateien, einschließlich Varianten und Hilfsobjekten.',
        'Keine Familienzahl: eine SHP-/Voxelvariante, ein Support-Power-Flugzeug und eine Trägerdrohne sind nicht automatisch unabhängige Fahrzeugfamilien.', '',
        '## Strukturelle Einordnung', '', '| Kategorie | Anzahl |', '|---|---:|']
    lines += [f'| {k} | {v} |' for k,v in sorted(categories.items())]
    inherited = [r['actor'] for r in rows if r['category'] == 'production-candidate' and not r['local_buildable']]
    lines += ['', 'Der Produktionsstatus kommt aus **aufgelösten Buildable-Traits**, nicht nur lokalen Deklarationen.',
        '`production-candidate` heißt nicht überall sofort baubar: Tech, Nation, Promotions und Spielmodus gelten weiterhin.',
        '**Geerbte Buildable-Kandidaten ohne lokale Buildable-Deklaration:** '+(', '.join('`'+a+'`' for a in inherited) or 'keine')+'.', '',
        'Daten: `tools/modular/air-naval-loadouts.json`. Identische Trait-Dokumentationen sind über `trait_definitions` dedupliziert.',
        'Enthalten: Waffen/Conditions, Aircraft-Flug-/Landeparameter, Mobile-Locomotor, Munition/Nachladen, Rearmable-Basen,',
        'Cargo, Carryall, Träger-/Drohnen-/Raketen-Spawner, Grafik und PDL/Reflector. Geschwindigkeiten kommen bei Aircraft',
        'aus AircraftInfo – nicht aus einem fälschlich vorausgesetzten Mobile-Trait. Werte sind Rohwerte, keine effektiven Upgrade-Werte.', '',
        'Die Felder sind Dokumentationsstrings, kein verlustfreies MiniYaml. Helpers/Raketen/Wracks werden nicht als reguläre',
        'Produktionsfahrzeuge gezählt; `non-mobile/review` enthält beispielsweise Helix-Ausrüstungs-Upgrades.',
        'Unklare Nichtproduktionsobjekte bleiben bewusst review statt automatisch als Support-Flugzeuge klassifiziert zu werden.', '',
        '**Keine neue Designer-Freigabe und keine neuen Strom-/Balancewerte.** Die bisherige Boden-Baukastenabdeckung bleibt unverändert.', '',
        '## Träger, Transport und Wiederbewaffnung', '', '| Actor | Rolle | Mechaniken | Spawner-Akteure / Rearm-Basen |', '|---|---|---|---|']
    for r in rows:
        relevant = [t for t in r['traits'] if t['type'].startswith(('CarrierMaster','DroneSpawnerMaster','MissileSpawnerMaster','Rearmable'))]
        if not relevant and not selected(r,'Carryall') and not selected(r,'AutoCarryall') and not selected(r,'Cargo'): continue
        links = sorted({t['fields'][k] for t in relevant for k in ('Actors','RearmActors') if t['fields'].get(k)})
        lines.append(f'| `{r["actor"]}` | {r["category"]} | {features(r)} | '+ '; '.join('`'+v+'`' for v in links)+' |')
    for source in PATHS:
        lines += ['', '## `'+source+'`', '', '| Actor | Einordnung | Bewegung / Tempo | Produktion | Waffenkanäle / Waffen | Mechaniken |', '|---|---|---|---|---|---|']
        for r in rows:
            if r['source'] != source: continue
            s = r['summary']; movement = r['locomotor'] or r['movement_kind']
            weapons = ', '.join('`'+w+'`' for w in s['weapons']) or '—'
            lines.append(f'| `{r["actor"]}` | {r["category"]} | {movement} / {r["speed"] if r["speed"] is not None else "—"} | {r["production_status"]} | {s["armament_channels"]} / {weapons} | {features(r)} |')
    lines += ['', '## Grenzen', '',
        '- Strukturelle Erfassung, kein Match-/Flug-/Landungs-/Trägertest.',
        '- Keine neue Familienpolitik für Luft/See, keine ungeprüften Modul-Kombinationen.',
        '- Das separate [Bodeninventar](existing-vehicle-loadouts.md) enthält weiterhin seine vollständigen 388 Deklarationen.',
        '- Alle Klassifikationen lassen sich auf die gespeicherten Traits und Quellpfade zurückführen.', '']
    return '\n'.join(lines)+'\n'


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__); p.add_argument('--refresh', action='store_true'); args = p.parse_args()
    data = refresh() if args.refresh else load()
    if data['source_sha256'] != source_hash(): raise ValueError('Stale snapshot: rebuild CA, then run --refresh')
    REPORT.write_text(report(data), encoding='utf-8'); print(REPORT)
