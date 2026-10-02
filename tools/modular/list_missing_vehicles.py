"""Designer gap list. Editorial roles/local availability, NOT full MiniYaml resolution."""
import json
import re
from pathlib import Path
from audit_reference_changes import blocks
from survey_vehicles import survey, family_summary, local_fields, FACTIONS

REPO = Path(__file__).resolve().parents[2]
NAMES = dict(gdi='GDI', nod='Nod', china='China', allies='Alliierte', soviet='Sowjets', scrin='Scrin')
SUPPORT = {
    'gdi': {'MEMP': 'EMP-Unterstützung, nicht harmlos/unbewaffnet im taktischen Sinn', 'MSAR': 'Sensoren / mobiles Radar'},
    'nod': {'AMCV': 'Baufahrzeug; gemeinsam GDI/Nod (~td)', 'HAR2': 'Sammler; gemeinsam GDI/Nod (~td)',
            'COORDINATOR': 'Verstärkungs-/Produktionskoordination', 'MSG': 'Tarnfeld-Unterstützung'},
    'china': {'CHNMCV': 'Baufahrzeug', 'chharv': 'Sammler', 'chcrawl': 'Truppentransporter',
              'choutpost': 'Aufklärung/Radar/Transport; bewaffnete Bunkervariante separat beachten',
              'checm': 'Elektronische Kampfführung / Fahrzeugdeaktivierung'},
    'allies': {'HARV': 'Sammler inkl. Chrono Miner', 'MCV': 'Baufahrzeug', 'MGG': 'Sicht-/Gap-Unterstützung', 'MRJ': 'Radarstörung'},
    'soviet': {'MCV.Soviet': 'Baufahrzeug', 'Soviet_Miner': 'Bewaffneter Sammler; primär Wirtschaft, nicht unbewaffnet'},
    'scrin': {'HARV.Scrin': 'Sammler', 'SMCV': 'Baufahrzeug / Colony Ship'},
}


def native_actors():
    text = (REPO / 'OpenRA.Mods.CA/Modular/CustomVehicleAssembly.cs').read_text(encoding='utf-8')
    actors = re.findall(r'new CustomVehicleAssembly\("[^"]+", "([^"]+)"', text)
    actors += re.findall(r'(?:ScrinAssembly|NodCombatAssembly|ChinaCombatAssembly)\("[^"]+", "([^"]+)"', text)
    return {a.lower() for a in actors}


def inventory():
    catalog = json.loads((REPO / 'tools/modular/catalog.json').read_text(encoding='utf-8'))
    rows = survey(REPO, catalog)
    families = family_summary(rows)
    native = native_actors()
    for f in families:
        f['native'] = [a for a in f['members'] if a.lower() in native]
        f['role'] = 'support' if f['family'] in SUPPORT[f['faction']] else 'combat'
    indexed = {(f['faction'], f['family']) for f in families}
    for faction, entries in SUPPORT.items():
        for actor in entries:
            if (faction, actor) not in indexed:
                raise ValueError('Stale role classification: ' + actor)
    return rows, families


def report():
    rows, families = inventory()
    missing = [f for f in families if not f['native'] and f['status'] == 'production candidate']
    counts = {role: sum(f['role'] == role for f in missing) for role in ('combat', 'support')}
    lines = ['# Was im Fahrzeugdesigner noch fehlt', '',
        'Stand: aktuelle native Bindings einschließlich aller Nod-/China-Boden-Kampfgrundfamilien; aus Regeln und Compiler-Bindungen erzeugt.', '',
        '**Fehlend bedeutet nicht im Designer konfigurier-/kompilierbar.** Die normalen Fraktionsroster bleiben erhalten;',
        'diese Fahrzeuge können dort bereits existieren. Tabellen-/Excel-Einträge und vorplatzierte Carryalls zählen nicht als Designer-Unterstützung.', '',
        f'In den sechs Bodenfahrzeug-Dateien: **{len(missing)} vollständig fehlende Fraktions-Familiengruppen**:',
        f'**{counts["combat"]} Kampf-/bewaffnete Transportgruppen**, **{counts["support"]} Wirtschafts-/Bau-/Transport-/Unterstützungsgruppen**.',
        'Das sind redaktionelle Gruppen mit lokaler Produktionsdeklaration, keine vollständig aufgelöste Baubarkeitsprüfung.',
        'Fraktionszuordnung folgt der Quelldatei, nicht exklusiver Verfügbarkeit. APC2, AMCV und HAR2 werden von GDI/Nod geteilt.',
        'APCs, kämpfende Besatzungstransporter und Selbstmordfahrzeuge stehen unter Kampf; EMP/ECM/Radar unter Unterstützung.',
        'Unterstützung bedeutet nicht zwingend unbewaffnet: insbesondere Soviet Miner und Outpost-Varianten sind Mischrollen.',
        'Wracks, Projektile, Dummy-Akteure und angehängte Hilfsobjekte werden nicht als eigene Fahrzeuge gezählt.', '']
    for role, title in [('combat', '1. Vollständig fehlende Boden-Kampffahrzeuge'),
                        ('support', '2. Vollständig fehlende andere Bodenfahrzeuge')]:
        lines += ['## ' + title, '']
        for faction in ('gdi', 'nod', 'china', 'allies', 'soviet', 'scrin'):
            selected = [f for f in missing if f['faction'] == faction and f['role'] == role]
            lines += [f'### {NAMES[faction]} ({len(selected)})', '']
            for f in selected:
                note = SUPPORT[faction].get(f['family'], '')
                lines.append(f'- **{f["name"]}** (`{f["family"]}`)' + (' — ' + note if note else ''))
            lines.append('')
    lines += ['## 3. Fehlende Varianten bereits angebundener Familien', '',
        'Hier fehlt nicht die ganze Familie. Eigenständige Varianten/Promotions sind nicht durch ihre Basiseinheit erledigt.',
        'Bereits geerbte bedingte Waffen (z.B. T-34 Cluster, Dragon Black Napalm, Emperor-Zustand) sind dagegen kein zweites fehlendes Fahrzeug.', '']
    for f in families:
        if not f['native']:
            continue
        pending = [r['actor'] for r in rows if r['faction'] == f['faction'] and r['family'] == f['family']
                   and r['actor'].lower() not in native_actors() and r['category'] == 'variant']
        if pending:
            lines.append(f'- **{NAMES[f["faction"]]} / {f["family"]}** (vorhanden: {", ".join(f["native"])}): ' + ', '.join('`' + a + '`' for a in pending))
    lines += ['', '## 4. Alle Varianten vollständig fehlender Bodenfamilien', '',
              'Basisakteure und Varianten als Prüfliste; nicht als zusätzliche Familien zählen.', '']
    for f in missing:
        lines.append(f'- **{NAMES[f["faction"]]} / {f["family"]}**: ' + ', '.join('`' + a + '`' for a in f['members']))
    lines += ['', '## 5. Sonder-/Prüffälle am Boden', '',
              '- `SFTNK` — sowjetischer Flame Tank: Gruppe ohne bestätigte lokale Produktionsdeklaration; separat prüfen.',
              '- `TRUK` — Supply Truck: Basis lokal `~disabled`; `TRUK.Test` besitzt eine Produktionsdeklaration mit `promotion.oil_pumps`. Fehlende Versorgungseinheit außerhalb der sechs Fraktionsdateien.',
              '- `TRUK.DROP` — abgeworfene, nicht normal produzierbare Form; kein zusätzlicher Fahrzeugtyp.',
              '- `CHTRUK` — chinesische Supply-Truck-Ausführung, erbt `TRUK` einschließlich dessen Einschränkungen; separat von regulären Sammlern prüfen.',
              '- `CDRN` — Chaos Drone: lokal `~disabled`; fehlender deaktivierter Kampf-/Kontrollprototyp, nicht als regulär baubares Fahrzeug gezählt.', '',
              '## 6. Luft- und Wasserfahrzeuge: ebenfalls vollständig außerhalb des Designers', '',
              'Keine Flugzeug-/Schiffsbindung ist implementiert. Folgende vollständige Liste lokaler Produktionsdeklarationen',
              'enthält bewusst Varianten, mit Actor-ID zur Unterscheidung. Keine Familien-Gesamtsumme daraus ableiten.',
              'Baubarkeit kann zusätzlich von Promotion, Fraktion oder Spielmodus abhängen. Bewaffnete Transporte sind Mischrollen.', '',
              '**Transport/Unterstützung statt primärem Kampffahrzeug:** Chinook (`TRAN`), Carryall (`OCAR`/`OCAR.Eagle`),',
              'Dropship, Airship of the People (`BLIMP`) und Landungsboot (`LST`). Auch C-17-Versorgungsflugzeuge,',
              'UAV und U-2 fehlen als Designer-Funktionen, sind aber keine normalen Produktionsfamilien.',
              '**Bewaffnete Mischrollen:** Combat Chinook (`TRAN.Eagle`), Seahawk, Helix und Carrier samt Trägerdrohnen.',
              'Die übrigen unten aufgeführten regulären Flugzeuge/Schiffe sind Kampfverbände; ihre Upgrades bleiben jeweils sichtbar.', '']
    paths = [f'mods/ca/rules/{f}/aircraft.yaml' for f in FACTIONS]
    paths += ['mods/ca/rules/aircraft.yaml', 'mods/ca/rules/misc/aircraft.yaml', 'mods/ca/rules/misc/ships.yaml']
    helpers = []
    for path in paths:
        lines += ['### `' + path + '`', '']
        for actor, block in blocks((REPO / path).read_text(encoding='utf-8-sig')).items():
            if actor.startswith('^'):
                continue
            fields, parents, traits = local_fields(block)
            if 'Buildable' not in traits:
                if not any('Husk' in p for p in parents):
                    helpers.append(f'- `{actor}` — {fields.get("Tooltip.Name", "geerbt / ohne lokalen Namen")} (`{path}`)')
                continue
            if actor.startswith('helix.') and actor.endswith('upgrade'):
                continue  # Equipment upgrades are not aircraft.
            note = ' **lokal deaktiviert**' if '~disabled' in fields.get('Buildable.Prerequisites', '') else ''
            lines.append(f'- {fields.get("Tooltip.Name", actor)} (`{actor}`){note}')
        lines.append('')
    lines += ['### Nicht reguläre Produktionsformen / Support-Power- und Trägerobjekte', '',
              'Rohinventar ohne lokale Buildable-Deklaration: umfasst echte Einsatzflugzeuge, Transportformen,',
              'Drohnen, aber auch Abwurfkapseln, Raketen und Dummy-Objekte. Nicht pauschal als fehlende Fahrzeugfamilien zählen.', ''] + helpers
    lines += ['', '## Quellen und Grenzen', '',
              '- `OpenRA.Mods.CA/Modular/CustomVehicleAssembly.cs`: tatsächliche Compiler-Bindungen, nicht Kalibrierungsreferenzen.',
              '- `mods/ca/rules/{gdi,nod,china,allies,soviet,scrin}/vehicles.yaml`: Hauptinventar.',
              '- `tools/modular/vehicle-family-policy.json`: redaktionelle Familiengruppierung.',
              '- Die eingebundenen GLA-/Yuri-Fahrzeugdateien sind derzeit leer; USA hat ebenfalls keine Fahrzeugdefinitionen.',
              '- Infanterie, Gebäude, zivile Kartendekorationen und Wracks sind nicht Teil dieses Fahrzeugdesigner-Backlogs.',
              '- Familienstatus beruht auf lokalen Regeln. Kein allgemeiner MiniYaml-Vererbungsresolver und kein Beweis, dass jede Variante heute baubar ist.', '']
    return '\n'.join(lines)


if __name__ == '__main__':
    target = REPO / 'docs/modular/missing-vehicles.md'
    target.write_text(report(), encoding='utf-8')
    print(target)
