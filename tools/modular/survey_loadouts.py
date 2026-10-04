"""Resolved ground-vehicle variant/loadout inventory. Read-only research, not native admission."""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
import os
from pathlib import Path
import subprocess
from list_missing_vehicles import inventory, native_actors, NAMES, REPO

SNAPSHOT = Path(__file__).with_name('vehicle-loadouts.json')
REPORT = REPO / 'docs/modular/existing-vehicle-loadouts.md'


def source_hash():
    h = hashlib.sha256()
    files = sorted(set((REPO / 'mods/ca/rules').rglob('*.yaml')) | set((REPO / 'mods/ca/weapons').rglob('*.yaml')) |
        {REPO/'mods/ca/mod.yaml', REPO/'engine/VERSION', REPO/'OpenRA.Mods.CA/UtilityCommands/ExportVehicleLoadoutsCommand.cs'})
    for path in files:
        h.update(path.relative_to(REPO).as_posix().encode()); h.update(b'\0'); h.update(path.read_bytes().replace(b'\r\n', b'\n')); h.update(b'\0')
    return h.hexdigest()


def selected(row, prefix):
    return [t for t in row['traits'] if t['type'].startswith(prefix)]


def summary(row):
    arms = selected(row, 'Armament')
    turrets = selected(row, 'Turreted')
    return dict(weapons=sorted({t['fields']['Weapon'] for t in arms if t['fields'].get('Weapon')}),
        armament_channels=len(arms), turret_traits=len(turrets),
        pdl=bool(selected(row, 'PointDefense')),
        armor=next((t['fields'].get('Type', '') for t in selected(row, 'Armor')), ''),
        cargo=bool(selected(row, 'Cargo')),
        deploy=bool(selected(row, 'GrantConditionOnDeploy')),
        transform=bool(selected(row, 'Transform')),
        spawned_missiles=bool(selected(row, 'MissileSpawner')))


def pack(data):
    definitions = {}; actors = []
    for row in data['actors']:
        ids = []
        for trait in row['traits']:
            canonical = json.dumps(trait, sort_keys=True, ensure_ascii=False)
            key = hashlib.sha256(canonical.encode()).hexdigest()[:24]
            if key in definitions and definitions[key] != trait: raise ValueError('Trait hash collision')
            definitions[key] = trait; ids.append(key)
        actors.append(dict(row, traits=ids))
    return dict(data, actors=actors, trait_definitions=definitions)


def unpack(data):
    return dict(data, actors=[dict(r, traits=[data['trait_definitions'][k] for k in r['traits']]) for r in data['actors']])


def load():
    return unpack(json.loads(SNAPSHOT.read_text(encoding='utf-8')))


def refresh():
    rows, _ = inventory()
    rows = sorted(rows, key=lambda r: (r['faction'], r['family'], r['actor']))
    env = dict(os.environ, ENGINE_DIR=str(REPO/'engine'), MOD_SEARCH_PATHS=','.join(str(REPO/p) for p in ('mods','engine/mods')))
    result = subprocess.run(['dotnet', str(REPO/'engine/bin/OpenRA.Utility.dll'), str(REPO/'mods/ca'), '--export-vehicle-loadouts'] +
        [r['actor'] for r in rows], cwd=REPO, env=env, text=True, capture_output=True, check=True)
    resolved = json.loads(result.stdout)
    if [r['actor'] for r in resolved] != [r['actor'] for r in rows]: raise ValueError('Actor inventory mismatch')
    for r, editorial in zip(resolved, rows):
        r.update({k: editorial[k] for k in ('faction','family','category','source')})
        r['summary'] = summary(r)
    data = dict(schema=1, source_sha256=source_hash(), scope='six ground faction files, including variants and helpers; no air/naval', actors=resolved)
    # Deduplicate identical resolved trait configurations; reuse is evidence, not module compatibility.
    SNAPSHOT.write_text(json.dumps(pack(data), ensure_ascii=False, indent=2, sort_keys=True)+'\n', encoding='utf-8')
    return data


def report(data):
    rows = data['actors']; native = native_actors(); categories = Counter(r['category'] for r in rows)
    status = Counter(r['production_status'] for r in rows)
    pending = [r for r in rows if r['category'] == 'variant' and r['production_status'] == 'gated-candidate' and r['actor'].lower() not in native]
    lines = ['# Bestehende Fahrzeugvarianten und Aufbauten', '',
        'Erfassung aus **aufgelösten Engine-Regeln**, nicht nur lokalen YAML-Blöcken. Keine neuen Balancewerte, Stromverbräuche oder Designer-Freigaben.', '',
        f'**{len(rows)} Bodenakteure** aus den sechs Fraktionsdateien; darunter **{categories["variant"]} redaktionell zugeordnete Varianten**.',
        f'**{len(pending)} Varianten mit Buildable ohne disabled/botplayer-Sperre sind noch nicht nativ angebunden**. Weitere Tech-/Fraktions-/Promotionsbedingungen gelten trotzdem.',
        'Produktionsstatus: '+', '.join(f'`{k}`: {v}' for k,v in sorted(status.items()))+'.', '',
        'Detaildaten: `tools/modular/vehicle-loadouts.json`. Enthält Waffenkanäle, Bedingungen, Turmnamen/Offsets/Drehraten,',
        'Grafikreferenzen, Passagiere/Schießscharten, PDL-Magazine/Nachladen, Armor/Reflector, Aufstellung, Transformation und Raketen-Spawner.',
        'Feldwerte sind **Dokumentationsstrings**, insbesondere Dictionaries; kein verlustfreies MiniYaml und kein Compiler-Eingabeformat.',
        'Identische Trait-Konfigurationen sind unter `trait_definitions` dedupliziert; Actor-Einträge referenzieren ihre stabilen Hash-IDs.', '',
        '## Wichtige Abgrenzungen', '',
        '- Turreted-Traits sind **keine automatisch abgeleiteten Baukastenplätze**. PDL kann einen unsichtbaren eigenen Ziel-Turm besitzen.',
        '- Mehrere Armaments können Upgrade-, Visual-, Hilfs- oder situationsabhängige Kanäle derselben Waffe sein.',
        '- Gleiche Waffen-ID bedeutet wiederverwendbare Waffe, nicht automatisch identischer Turm oder passende Grafik.',
        '- PDL wird an PointDefense-Traits erkannt, Reflector am tatsächlichen Armor-Typ – nicht am Namenssuffix.',
        '- Reflector ist im Original hier ein Armor-/Zielklassen-/Palette-Paket; es wird kein neuer elektrischer Verbrauch erfunden.',
        '- Fahrzeugfamilien, konkrete Varianten, Helfer/Wracks und Editor-Bindungen werden nicht zusammengerechnet.',
        '- Flugzeuge/Schiffe bleiben außerhalb dieser Detailerfassung; ihre ID-Liste steht in [missing-vehicles.md](missing-vehicles.md).', '',
        '## Battle Fortress: Originalzustände', '',
        '| Actor | Produktion | Turreted-Traits | Waffenkanäle | Waffen | Cargo | PDL | Armor |',
        '|---|---|---:|---:|---|---|---|---|']
    for r in rows:
        if r['family'] != 'BATF': continue
        s = r['summary']
        lines.append(f'| `{r["actor"]}` | {r["production_status"]} | {s["turret_traits"]} | {s["armament_channels"]} | '+', '.join('`'+w+'`' for w in s['weapons'])+f' | {s["cargo"]} | {s["pdl"]} | {s["armor"]} |')
    lines += ['', 'Die Designer-Regel **Battle Fortress: 3 Plätze, Bunker: 3/3** bleibt davon unabhängig bestehen.',
        'Die Prism Fortress hat einen Frontangriff, keinen frei drehbaren Prismturm. Für einen frei montierbaren Sonic-Turm',
        'liefert DISR die getrennte Sprite-Turmsequenz sowie SonicZap/Visual/Upgrade-Kanäle; diese neue Kombination ist noch nicht freigegeben.',
        'Auffällig im Original: BATF.Artillery bindet sein 155mm-Armament an `cargo`, besitzt aber laut aufgelösten Regeln keinen Cargo-Trait.',
        'Das ist ein Prüffall, keine hier vorgenommene Reparatur. Außerdem existieren bereits Bunker+PDL und Bunker+Reflector:',
        'neue Stromgrenzen dürfen diese Originalkombinationen nicht stillschweigend als unmöglich deklarieren.', '',
        '## Gemeinsam verwendete Waffen', '',
        'Nur identische Waffen-IDs (case-insensitive Engine-Lookup), mindestens zwei redaktionelle Familien. Keine Preis-/Leistungsangleichung.', '']
    weapons = defaultdict(list)
    for r in rows:
        if r['category'] not in ('base','variant'): continue
        for w in r['summary']['weapons']: weapons[w.lower()].append(r)
    for weapon, users in sorted(weapons.items()):
        families = sorted({r['faction']+'/'+r['family'] for r in users})
        if len(families) > 1: lines.append(f'- `{weapon}`: '+', '.join(families))
    lines += ['', '## Vollständige Basis-/Varianten-Prüfliste', '',
        'Alle lokalen Kategorien sind enthalten, auch deaktivierte und nicht produzierbare Helfer. `nativ` bedeutet eine konkrete Compiler-Bindung.', '']
    for faction in NAMES:
        lines += ['### '+NAMES[faction], '', '| Actor | Familie / Kategorie | Produktion | Nativ | Türme / Kanäle | PDL / Reflector | Cargo / Deploy / Transform |', '|---|---|---|---|---|---|---|']
        for r in rows:
            if r['faction'] != faction: continue
            s = r['summary']
            lines.append(f'| `{r["actor"]}` | {r["family"]} / {r["category"]} | {r["production_status"]} | {"ja" if r["actor"].lower() in native else "nein"} | {s["turret_traits"]} / {s["armament_channels"]} | {s["pdl"]} / {s["armor"] == "Reflector"} | {s["cargo"]} / {s["deploy"]} / {s["transform"]} |')
        lines.append('')
    return '\n'.join(lines)+'\n'


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('--refresh', action='store_true'); args = parser.parse_args()
    data = refresh() if args.refresh else load()
    if data['source_sha256'] != source_hash(): raise ValueError('Stale resolved snapshot; build the engine/CA and run --refresh')
    REPORT.write_text(report(data), encoding='utf-8')
    print(REPORT)
