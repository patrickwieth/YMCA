# Bestehende Fahrzeugvarianten und Aufbauten

Erfassung aus **aufgelösten Engine-Regeln**, nicht nur lokalen YAML-Blöcken. Keine neuen Balancewerte, Stromverbräuche oder Designer-Freigaben.

**388 Bodenakteure** aus den sechs Fraktionsdateien; darunter **252 redaktionell zugeordnete Varianten**.
**247 Varianten mit Buildable ohne disabled/botplayer-Sperre sind noch nicht nativ angebunden**. Weitere Tech-/Fraktions-/Promotionsbedingungen gelten trotzdem.
Produktionsstatus: `bot-gated`: 2, `disabled`: 5, `gated-candidate`: 355, `no-buildable`: 26.

Detaildaten: `tools/modular/vehicle-loadouts.json`. Enthält Waffenkanäle, Bedingungen, Turmnamen/Offsets/Drehraten,
Grafikreferenzen, Passagiere/Schießscharten, PDL-Magazine/Nachladen, Armor/Reflector, Aufstellung, Transformation und Raketen-Spawner.
Feldwerte sind **Dokumentationsstrings**, insbesondere Dictionaries; kein verlustfreies MiniYaml und kein Compiler-Eingabeformat.
Identische Trait-Konfigurationen sind unter `trait_definitions` dedupliziert; Actor-Einträge referenzieren ihre stabilen Hash-IDs.

## Wichtige Abgrenzungen

- Turreted-Traits sind **keine automatisch abgeleiteten Baukastenplätze**. PDL kann einen unsichtbaren eigenen Ziel-Turm besitzen.
- Mehrere Armaments können Upgrade-, Visual-, Hilfs- oder situationsabhängige Kanäle derselben Waffe sein.
- Gleiche Waffen-ID bedeutet wiederverwendbare Waffe, nicht automatisch identischer Turm oder passende Grafik.
- PDL wird an PointDefense-Traits erkannt, Reflector am tatsächlichen Armor-Typ – nicht am Namenssuffix.
- Reflector ist im Original hier ein Armor-/Zielklassen-/Palette-Paket; es wird kein neuer elektrischer Verbrauch erfunden.
- Fahrzeugfamilien, konkrete Varianten, Helfer/Wracks und Editor-Bindungen werden nicht zusammengerechnet.
- Flugzeuge/Schiffe bleiben außerhalb dieser Detailerfassung; ihre ID-Liste steht in [missing-vehicles.md](missing-vehicles.md).

## Battle Fortress: Originalzustände

| Actor | Produktion | Turreted-Traits | Waffenkanäle | Waffen | Cargo | PDL | Armor |
|---|---|---:|---:|---|---|---|---|
| `BATF` | disabled | 4 | 3 | `BATFGun`, `BATFRockets` | False | False | Heavy |
| `BATF.AI` | bot-gated | 4 | 3 | `BATFGun`, `BATFRockets` | False | False | Heavy |
| `BATF.Artillery` | gated-candidate | 4 | 3 | `155mm`, `BATFGun` | False | False | Heavy |
| `BATF.Artillery.PDL` | gated-candidate | 5 | 4 | `155mm`, `AdvancedPointLaser`, `BATFGun` | False | True | Heavy |
| `BATF.Artillery.Reflector` | gated-candidate | 4 | 3 | `155mm`, `BATFGun` | False | False | Reflector |
| `BATF.Bunker` | gated-candidate | 0 | 2 | `M60mg`, `ZSU-23` | True | False | Heavy |
| `BATF.Bunker.PDL` | gated-candidate | 1 | 3 | `AdvancedPointLaser`, `M60mg`, `ZSU-23` | True | True | Heavy |
| `BATF.Bunker.Reflector` | gated-candidate | 0 | 2 | `M60mg`, `ZSU-23` | True | False | Reflector |
| `BATF.Prism` | gated-candidate | 0 | 1 | `BattlePrisLaser` | False | False | Light |
| `BATF.Prism.PDL` | gated-candidate | 1 | 2 | `AdvancedPointLaser`, `BattlePrisLaser` | False | True | Light |
| `BATF.Prism.Reflector` | gated-candidate | 0 | 1 | `BattlePrisLaser` | False | False | Reflector |
| `BATF.Support` | gated-candidate | 2 | 1 | `BATF.Repair` | True | False | Heavy |
| `BATF.Support.PDL` | gated-candidate | 3 | 2 | `AdvancedPointLaser`, `BATF.Repair` | True | True | Heavy |
| `BATF.Support.Reflector` | gated-candidate | 2 | 1 | `BATF.Repair` | True | False | Reflector |

Die Designer-Regel **Battle Fortress: 3 Plätze, Bunker: 3/3** bleibt davon unabhängig bestehen.
Die Prism Fortress hat einen Frontangriff, keinen frei drehbaren Prismturm. Für einen frei montierbaren Sonic-Turm
liefert DISR die getrennte Sprite-Turmsequenz sowie SonicZap/Visual/Upgrade-Kanäle; diese neue Kombination ist noch nicht freigegeben.
Auffällig im Original: BATF.Artillery bindet sein 155mm-Armament an `cargo`, besitzt aber laut aufgelösten Regeln keinen Cargo-Trait.
Das ist ein Prüffall, keine hier vorgenommene Reparatur. Außerdem existieren bereits Bunker+PDL und Bunker+Reflector:
neue Stromgrenzen dürfen diese Originalkombinationen nicht stillschweigend als unmöglich deklarieren.

## Gemeinsam verwendete Waffen

Nur identische Waffen-IDs (case-insensitive Engine-Lookup), mindestens zwei redaktionelle Familien. Keine Preis-/Leistungsangleichung.

- `120mmheat`: allies/2TNK, allies/RTNK
- `125mm`: soviet/Heavy_Tank, soviet/Peoples_Tank
- `155mm`: allies/ARTY, allies/BATF
- `183mm`: allies/2TNK, allies/TNKD
- `30mm`: nod/LTNK, soviet/Devil_Tank, soviet/T-34
- `advancedpointlaser`: allies/1TNK, allies/BATF, allies/CTNK, allies/IFV, allies/RTNK, allies/TNKD, china/chbattle, china/chcrawl2, china/chdragon, china/chgtnk, china/choutpost, china/choverlord, gdi/DISR, gdi/MSAR, gdi/MTNK, gdi/Mammoth, gdi/TITN, gdi/hmlrs, nod/BGGY, nod/BIKE, nod/FTNK, nod/HFTK, nod/STNK, scrin/CORR, scrin/Channeler, scrin/LACE, scrin/LCHR, scrin/RPTP, scrin/SEEK, scrin/STCR, scrin/TPOD, soviet/Devil_Tank, soviet/Heavy_Tank, soviet/ISU, soviet/Peoples_Tank, soviet/Source_of_Pollution, soviet/apoc
- `chinamgatt.0`: china/chgtnk, china/choverlord, soviet/Soviet_Miner
- `chinamgatt.0g`: china/chgtnk, china/choverlord, soviet/Soviet_Miner
- `chinamgatt.1`: china/chgtnk, china/choverlord, soviet/Soviet_Miner
- `chinamgatt.1g`: china/chgtnk, china/choverlord, soviet/Soviet_Miner
- `chinamgatt.2`: china/chgtnk, china/choverlord, soviet/Soviet_Miner
- `chinamgatt.2g`: china/chgtnk, china/choverlord, soviet/Soviet_Miner
- `chinamgatt.3`: china/chgtnk, china/choverlord, soviet/Soviet_Miner
- `chinamgatt.3g`: china/chgtnk, china/choverlord, soviet/Soviet_Miner
- `demotrucktargeting`: nod/TTRK, soviet/DTRK
- `ifvrockets`: allies/IFV, soviet/BTR
- `ifvrocketsaa`: allies/IFV, soviet/BTR
- `m60mg`: allies/APC, allies/BATF, allies/IFV, allies/JEEP
- `m60mgtd`: gdi/HMMV, nod/APC2, nod/BGGY, soviet/kims_wheel
- `mammothtusk`: gdi/Mammoth, soviet/apoc
- `ttankzap`: allies/IFV, soviet/TTNK

## Vollständige Basis-/Varianten-Prüfliste

Alle lokalen Kategorien sind enthalten, auch deaktivierte und nicht produzierbare Helfer. `nativ` bedeutet eine konkrete Compiler-Bindung.

### GDI

| Actor | Familie / Kategorie | Produktion | Nativ | Türme / Kanäle | PDL / Reflector | Cargo / Deploy / Transform |
|---|---|---|---|---|---|---|
| `MDRN.Attached` |  / attachment | no-buildable | nein | 3 / 1 | False / False | False / False / False |
| `DISR` | DISR / base | gated-candidate | ja | 1 / 4 | False / False | False / False / False |
| `DISR.PDL` | DISR / variant | gated-candidate | nein | 2 / 5 | True / False | False / False / False |
| `DISR.Reflector` | DISR / variant | gated-candidate | nein | 1 / 4 | False / True | False / False / False |
| `GDRN` | GDRN / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `HMMV` | HMMV / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `Juggernaut` | Juggernaut / base | gated-candidate | ja | 0 / 2 | False / False | False / False / False |
| `Juggernaut.Emp` | Juggernaut / variant | gated-candidate | nein | 0 / 2 | False / False | False / False / False |
| `Juggernaut.Firerate` | Juggernaut / variant | gated-candidate | nein | 0 / 2 | False / False | False / False / False |
| `MAMMOTHMK2` | MAMMOTHMK2 / base | gated-candidate | ja | 0 / 3 | False / False | False / False / False |
| `MARV` | MARV / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `MDRN` | MDRN / base | gated-candidate | ja | 3 / 1 | False / False | False / False / False |
| `MEMP` | MEMP / base | gated-candidate | nein | 0 / 0 | False / False | False / False / False |
| `MEMP.Ranged` | MEMP / variant | gated-candidate | nein | 2 / 1 | False / False | False / False / False |
| `MEMP.Ranged.Improved` | MEMP / variant | gated-candidate | nein | 2 / 1 | False / False | False / False / False |
| `MEMP.Volatile` | MEMP / variant | gated-candidate | nein | 0 / 0 | False / False | False / False / False |
| `MLRS` | MLRS / base | gated-candidate | ja | 1 / 2 | False / False | False / False / False |
| `MLRS.AA` | MLRS / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `MLRS.Hailstorm` | MLRS / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `MSAR` | MSAR / base | gated-candidate | nein | 0 / 0 | False / False | False / False / False |
| `MSAR.PDL` | MSAR / variant | gated-candidate | nein | 1 / 1 | True / False | False / False / False |
| `MTNK` | MTNK / base | gated-candidate | ja | 2 / 1 | False / False | False / False / False |
| `MTNK.PDL` | MTNK / variant | gated-candidate | nein | 3 / 2 | True / False | False / False / False |
| `MTNK.Reflector` | MTNK / variant | gated-candidate | nein | 2 / 1 | False / True | False / False / False |
| `Mammoth` | Mammoth / base | gated-candidate | ja | 2 / 2 | False / False | False / False / False |
| `Mammoth.Hover` | Mammoth / variant | gated-candidate | nein | 2 / 2 | False / False | False / False / False |
| `Mammoth.Hover.PDL` | Mammoth / variant | gated-candidate | nein | 3 / 3 | True / False | False / False / False |
| `Mammoth.Hover.Reflector` | Mammoth / variant | gated-candidate | nein | 2 / 2 | False / True | False / False / False |
| `Mammoth.Ion` | Mammoth / variant | gated-candidate | nein | 2 / 1 | False / False | False / False / False |
| `Mammoth.Ion.PDL` | Mammoth / variant | gated-candidate | nein | 3 / 2 | True / False | False / False / False |
| `Mammoth.Ion.Reflector` | Mammoth / variant | gated-candidate | nein | 2 / 1 | False / True | False / False / False |
| `Mammoth.Nanite` | Mammoth / variant | gated-candidate | nein | 2 / 2 | False / False | False / False / False |
| `Mammoth.Nanite.PDL` | Mammoth / variant | gated-candidate | nein | 3 / 3 | True / False | False / False / False |
| `Mammoth.Nanite.Reflector` | Mammoth / variant | gated-candidate | nein | 2 / 2 | False / True | False / False / False |
| `SLNG` | SLNG / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `TITN` | TITN / base | gated-candidate | ja | 2 / 2 | False / False | False / False / False |
| `TITN.Battle` | TITN / variant | gated-candidate | nein | 2 / 2 | False / False | False / False / False |
| `TITN.Battle.PDL` | TITN / variant | gated-candidate | nein | 3 / 3 | True / False | False / False / False |
| `TITN.Battle.Reflector` | TITN / variant | gated-candidate | nein | 2 / 2 | False / True | False / False / False |
| `TITN.Railgun` | TITN / variant | gated-candidate | nein | 2 / 2 | False / False | False / False / False |
| `TITN.Railgun.PDL` | TITN / variant | gated-candidate | nein | 3 / 3 | True / False | False / False / False |
| `TITN.Railgun.Reflector` | TITN / variant | gated-candidate | nein | 2 / 2 | False / True | False / False / False |
| `VULC` | VULC / base | gated-candidate | ja | 1 / 7 | False / False | True / False / False |
| `XO` | XO / base | gated-candidate | ja | 1 / 2 | False / False | False / False / False |
| `hmlrs` | hmlrs / base | gated-candidate | ja | 1 / 4 | False / False | False / False / False |
| `hmlrs.PDL` | hmlrs / variant | gated-candidate | nein | 2 / 5 | True / False | False / False / False |
| `hmlrs.Reflector` | hmlrs / variant | gated-candidate | nein | 1 / 4 | False / True | False / False / False |

### Nod

| Actor | Familie / Kategorie | Produktion | Nativ | Türme / Kanäle | PDL / Reflector | Cargo / Deploy / Transform |
|---|---|---|---|---|---|---|
| `AMCV` | AMCV / base | gated-candidate | nein | 0 / 0 | False / False | False / False / True |
| `AMCV.Nukular` | AMCV / non-production form | no-buildable | nein | 0 / 0 | False / False | False / False / True |
| `APC2` | APC2 / base | gated-candidate | ja | 0 / 1 | False / False | True / False / False |
| `APC2.Reinforce` | APC2 / non-production form | no-buildable | nein | 0 / 1 | False / False | True / False / False |
| `ARTY.nod` | ARTY.nod / base | gated-candidate | ja | 0 / 1 | False / False | False / False / False |
| `BGGY` | BGGY / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `BGGY.AA` | BGGY / variant | gated-candidate | nein | 1 / 2 | False / False | False / False / False |
| `BGGY.PDL` | BGGY / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `BIKE` | BIKE / base | gated-candidate | ja | 0 / 2 | False / False | False / False / False |
| `BIKE.Explosive` | BIKE / variant | gated-candidate | nein | 0 / 2 | False / False | False / False / False |
| `BIKE.Explosive.PDL` | BIKE / variant | gated-candidate | nein | 1 / 3 | True / False | False / False / False |
| `BIKE.Explosive.Reflector` | BIKE / variant | gated-candidate | nein | 0 / 2 | False / True | False / False / False |
| `BIKE.RocketHail` | BIKE / variant | gated-candidate | nein | 0 / 2 | False / False | False / False / False |
| `BIKE.RocketHail.PDL` | BIKE / variant | gated-candidate | nein | 1 / 3 | True / False | False / False / False |
| `BIKE.RocketHail.Reflector` | BIKE / variant | gated-candidate | nein | 0 / 2 | False / True | False / False / False |
| `BIKE.Scrin` | BIKE / variant | gated-candidate | nein | 0 / 2 | False / False | False / False / False |
| `BIKE.Scrin.PDL` | BIKE / variant | gated-candidate | nein | 1 / 3 | True / False | False / False / False |
| `BIKE.Scrin.Reflector` | BIKE / variant | gated-candidate | nein | 0 / 2 | False / True | False / False / False |
| `Beam_Cannon` | Beam_Cannon / base | gated-candidate | ja | 1 / 8 | False / False | False / False / False |
| `COORDINATOR` | COORDINATOR / base | gated-candidate | nein | 0 / 0 | False / False | False / False / False |
| `FTNK` | FTNK / base | gated-candidate | ja | 0 / 1 | False / False | False / False / False |
| `FTNK.PDL` | FTNK / variant | gated-candidate | nein | 1 / 2 | True / False | False / False / False |
| `FTNK.PDL.Chem` | FTNK / variant | gated-candidate | nein | 1 / 2 | True / False | False / False / False |
| `FTNK.Reflector` | FTNK / variant | gated-candidate | nein | 0 / 1 | False / True | False / False / False |
| `FTNK.Reflector.Chem` | FTNK / variant | gated-candidate | nein | 0 / 1 | False / True | False / False / False |
| `HAR2` | HAR2 / base | gated-candidate | nein | 0 / 0 | False / False | False / False / False |
| `HFTK` | HFTK / base | gated-candidate | ja | 1 / 2 | False / False | False / False / False |
| `HFTK.PDL` | HFTK / variant | gated-candidate | nein | 2 / 3 | True / False | False / False / False |
| `HFTK.PDL.Fireball` | HFTK / variant | gated-candidate | nein | 2 / 3 | True / False | False / False / False |
| `HFTK.Reflector` | HFTK / variant | gated-candidate | nein | 1 / 2 | False / True | False / False / False |
| `HFTK.Reflector.Fireball` | HFTK / variant | gated-candidate | nein | 1 / 2 | False / True | False / False / False |
| `HOWI` | HOWI / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `LTNK` | LTNK / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `LTNK.Laser` | LTNK / variant | gated-candidate | nein | 0 / 1 | False / False | False / False / False |
| `MSG` | MSG / base | gated-candidate | nein | 0 / 0 | False / False | False / True / False |
| `SPEC` | SPEC / base | gated-candidate | ja | 1 / 1 | False / False | False / True / False |
| `SSM` | SSM / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `SSM.Bunkerbuster` | SSM / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `SSM.Multi` | SSM / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `SSM.Toxin` | SSM / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `STNK` | STNK / base | gated-candidate | ja | 1 / 2 | False / False | False / False / False |
| `STNK.AP` | STNK / variant | gated-candidate | nein | 1 / 2 | False / False | False / False / False |
| `STNK.AP.PDL` | STNK / variant | gated-candidate | nein | 2 / 3 | True / False | False / False / False |
| `STNK.AP.Reflector` | STNK / variant | gated-candidate | nein | 1 / 2 | False / True | False / False / False |
| `STNK.HE` | STNK / variant | gated-candidate | nein | 1 / 2 | False / False | False / False / False |
| `STNK.HE.PDL` | STNK / variant | gated-candidate | nein | 2 / 3 | True / False | False / False / False |
| `STNK.HE.Reflector` | STNK / variant | gated-candidate | nein | 1 / 2 | False / True | False / False / False |
| `STNK.Scrin` | STNK / variant | gated-candidate | nein | 1 / 2 | False / False | False / False / False |
| `STNK.Scrin.PDL` | STNK / variant | gated-candidate | nein | 2 / 3 | True / False | False / False / False |
| `STNK.Scrin.Reflector` | STNK / variant | gated-candidate | nein | 1 / 2 | False / True | False / False / False |
| `TTRK` | TTRK / base | gated-candidate | ja | 0 / 1 | False / False | False / True / False |
| `WTNK` | WTNK / base | gated-candidate | ja | 1 / 2 | False / False | False / False / False |

### China

| Actor | Familie / Kategorie | Produktion | Nativ | Türme / Kanäle | PDL / Reflector | Cargo / Deploy / Transform |
|---|---|---|---|---|---|---|
| `Bixi.Missile` |  / projectile | no-buildable | nein | 0 / 0 | False / False | False / False / False |
| `CHTRUK` |  / review | disabled | nein | 0 / 0 | False / False | False / False / False |
| `charty.Husk` |  / wreck | no-buildable | nein | 0 / 0 | False / False | False / False / False |
| `chcrawl.Husk` |  / wreck | no-buildable | nein | 0 / 0 | False / False | False / False / False |
| `chdragon.Husk` |  / wreck | no-buildable | nein | 0 / 0 | False / False | False / False / False |
| `chgtnk.Husk` |  / wreck | no-buildable | nein | 0 / 0 | False / False | False / False / False |
| `chharv.Husk` |  / wreck | no-buildable | nein | 0 / 0 | False / False | False / False / False |
| `chnukecann.Husk` |  / wreck | no-buildable | nein | 0 / 0 | False / False | False / False / False |
| `chnukecannon.shell` |  / projectile | no-buildable | nein | 0 / 0 | False / False | False / False / False |
| `chnukecannon.shell.neutron` |  / projectile | no-buildable | nein | 0 / 0 | False / False | False / False / False |
| `chnukecannon.shell.range` |  / projectile | no-buildable | nein | 0 / 0 | False / False | False / False / False |
| `choverlord.Husk` |  / wreck | no-buildable | nein | 0 / 0 | False / False | False / False / False |
| `Bixi` | Bixi / base | gated-candidate | ja | 0 / 1 | False / False | False / False / False |
| `CHNMCV` | CHNMCV / base | gated-candidate | nein | 0 / 0 | False / False | False / False / True |
| `CHNMCV.Nukular` | CHNMCV / non-production form | no-buildable | nein | 0 / 0 | False / False | False / False / True |
| `charty` | charty / base | gated-candidate | ja | 0 / 2 | False / False | False / False / False |
| `chbattle` | chbattle / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `chbattle.Autoloader` | chbattle / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `chbattle.Autoloader.PDL` | chbattle / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `chbattle.Autoloader.Reflector` | chbattle / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |
| `chbattle.Mass_Production` | chbattle / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `chbattle.Mass_Production.PDL` | chbattle / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `chbattle.Mass_Production.Reflector` | chbattle / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |
| `chbattle.Nuclear_Shells` | chbattle / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `chbattle.Nuclear_Shells.PDL` | chbattle / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `chbattle.Nuclear_Shells.Reflector` | chbattle / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |
| `chcrawl` | chcrawl / base | gated-candidate | nein | 0 / 0 | False / False | True / False / False |
| `chcrawl2` | chcrawl2 / base | gated-candidate | ja | 1 / 0 | False / False | True / False / False |
| `chcrawl2.Assault` | chcrawl2 / variant | gated-candidate | nein | 1 / 0 | False / False | True / False / False |
| `chcrawl2.Assault.PDL` | chcrawl2 / variant | gated-candidate | nein | 2 / 1 | True / False | True / False / False |
| `chcrawl2.Assault.Reflector` | chcrawl2 / variant | gated-candidate | nein | 1 / 0 | False / True | True / False / False |
| `chcrawl2.Hunter` | chcrawl2 / variant | gated-candidate | nein | 1 / 0 | False / False | True / False / False |
| `chcrawl2.Hunter.PDL` | chcrawl2 / variant | gated-candidate | nein | 2 / 1 | True / False | True / False / False |
| `chcrawl2.Hunter.Reflector` | chcrawl2 / variant | gated-candidate | nein | 1 / 0 | False / True | True / False / False |
| `chdragon` | chdragon / base | gated-candidate | ja | 1 / 4 | False / False | False / True / False |
| `chdragon.PDL` | chdragon / variant | gated-candidate | nein | 2 / 5 | True / False | False / True / False |
| `chdragon.Reflector` | chdragon / variant | gated-candidate | nein | 1 / 4 | False / True | False / True / False |
| `checm` | checm / base | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `checm.chain` | checm / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `checm.focus` | checm / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `checm.pulse` | checm / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `chgtnk` | chgtnk / base | gated-candidate | ja | 1 / 8 | False / False | False / False / False |
| `chgtnk.PDL` | chgtnk / variant | gated-candidate | nein | 2 / 9 | True / False | False / False / False |
| `chgtnk.Reflector` | chgtnk / variant | gated-candidate | nein | 1 / 8 | False / True | False / False / False |
| `chharv` | chharv / base | gated-candidate | nein | 0 / 0 | False / False | False / False / False |
| `chnukecann` | chnukecann / base | gated-candidate | ja | 1 / 2 | False / False | False / True / False |
| `chnukecann.Neutron` | chnukecann / variant | gated-candidate | nein | 1 / 2 | False / False | False / True / False |
| `chnukecann.Range` | chnukecann / variant | gated-candidate | nein | 1 / 2 | False / False | False / True / False |
| `choutpost` | choutpost / base | gated-candidate | nein | 1 / 0 | False / False | True / False / False |
| `choutpost.Bunker` | choutpost / variant | gated-candidate | nein | 1 / 0 | False / False | True / False / False |
| `choutpost.Bunker.PDL` | choutpost / variant | gated-candidate | nein | 2 / 1 | True / False | True / False / False |
| `choutpost.Bunker.Reflector` | choutpost / variant | gated-candidate | nein | 1 / 0 | False / True | True / False / False |
| `choutpost.Propaganda` | choutpost / variant | gated-candidate | nein | 1 / 0 | False / False | True / False / False |
| `choutpost.Propaganda.PDL` | choutpost / variant | gated-candidate | nein | 2 / 1 | True / False | True / False / False |
| `choutpost.Propaganda.Reflector` | choutpost / variant | gated-candidate | nein | 1 / 0 | False / True | True / False / False |
| `choverlord` | choverlord / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `choverlord.Bunker` | choverlord / variant | gated-candidate | nein | 1 / 1 | False / False | True / False / False |
| `choverlord.Bunker.PDL` | choverlord / variant | gated-candidate | nein | 2 / 2 | True / False | True / False / False |
| `choverlord.Bunker.Reflector` | choverlord / variant | gated-candidate | nein | 1 / 1 | False / True | True / False / False |
| `choverlord.Gatling` | choverlord / variant | gated-candidate | nein | 2 / 9 | False / False | False / False / False |
| `choverlord.Gatling.PDL` | choverlord / variant | gated-candidate | nein | 3 / 10 | True / False | False / False / False |
| `choverlord.Gatling.Reflector` | choverlord / variant | gated-candidate | nein | 2 / 9 | False / True | False / False / False |
| `choverlord.Nuke_Shells` | choverlord / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `choverlord.Nuke_Shells.PDL` | choverlord / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `choverlord.Nuke_Shells.Reflector` | choverlord / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |
| `choverlord.Plasma` | choverlord / variant | gated-candidate | nein | 1 / 2 | False / False | False / False / False |
| `choverlord.Plasma.PDL` | choverlord / variant | gated-candidate | nein | 2 / 3 | True / False | False / False / False |
| `choverlord.Plasma.Reflector` | choverlord / variant | gated-candidate | nein | 1 / 2 | False / True | False / False / False |
| `choverlord.Propaganda` | choverlord / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `choverlord.Propaganda.PDL` | choverlord / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `choverlord.Propaganda.Reflector` | choverlord / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |

### Alliierte

| Actor | Familie / Kategorie | Produktion | Nativ | Türme / Kanäle | PDL / Reflector | Cargo / Deploy / Transform |
|---|---|---|---|---|---|---|
| `1TNK` | 1TNK / base | gated-candidate | ja | 2 / 1 | False / False | False / False / False |
| `1TNK.PDL` | 1TNK / variant | gated-candidate | nein | 3 / 2 | True / False | False / False / False |
| `1TNK.Reflector` | 1TNK / variant | gated-candidate | nein | 2 / 1 | False / True | False / False / False |
| `2TNK` | 2TNK / non-production form | no-buildable | nein | 1 / 1 | False / False | False / False / False |
| `2TNK.Chrono` | 2TNK / variant | no-buildable | nein | 1 / 3 | False / False | False / False / False |
| `Challenger_Tank` | 2TNK / variant | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `Leclerc_Tank` | 2TNK / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `Leopard_Tank` | 2TNK / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `APC` | APC / base | gated-candidate | ja | 0 / 1 | False / False | True / False / False |
| `ARTY` | ARTY / base | gated-candidate | ja | 0 / 1 | False / False | False / False / False |
| `ARTY.Chrono` | ARTY / non-production form | no-buildable | nein | 0 / 1 | False / False | False / False / False |
| `BATF` | BATF / prototype | disabled | nein | 4 / 3 | False / False | False / False / False |
| `BATF.AI` | BATF / variant | bot-gated | nein | 4 / 3 | False / False | False / False / False |
| `BATF.Artillery` | BATF / variant | gated-candidate | nein | 4 / 3 | False / False | False / False / False |
| `BATF.Artillery.PDL` | BATF / variant | gated-candidate | nein | 5 / 4 | True / False | False / False / False |
| `BATF.Artillery.Reflector` | BATF / variant | gated-candidate | nein | 4 / 3 | False / True | False / False / False |
| `BATF.Bunker` | BATF / variant | gated-candidate | ja | 0 / 2 | False / False | True / False / False |
| `BATF.Bunker.PDL` | BATF / variant | gated-candidate | nein | 1 / 3 | True / False | True / False / False |
| `BATF.Bunker.Reflector` | BATF / variant | gated-candidate | nein | 0 / 2 | False / True | True / False / False |
| `BATF.Prism` | BATF / variant | gated-candidate | nein | 0 / 1 | False / False | False / False / False |
| `BATF.Prism.PDL` | BATF / variant | gated-candidate | nein | 1 / 2 | True / False | False / False / False |
| `BATF.Prism.Reflector` | BATF / variant | gated-candidate | nein | 0 / 1 | False / True | False / False / False |
| `BATF.Support` | BATF / variant | gated-candidate | nein | 2 / 1 | False / False | True / False / False |
| `BATF.Support.PDL` | BATF / variant | gated-candidate | nein | 3 / 2 | True / False | True / False / False |
| `BATF.Support.Reflector` | BATF / variant | gated-candidate | nein | 2 / 1 | False / True | True / False / False |
| `CHPR` | CHPR / base | gated-candidate | ja | 1 / 4 | False / False | False / False / False |
| `CHPR.AA` | CHPR / variant | gated-candidate | nein | 1 / 8 | False / False | False / False / False |
| `CHPR.Range` | CHPR / variant | gated-candidate | nein | 1 / 4 | False / False | False / False / False |
| `CRYO` | CRYO / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `CTNK` | CTNK / base | gated-candidate | ja | 1 / 2 | False / False | False / False / False |
| `CTNK.PDL` | CTNK / variant | gated-candidate | nein | 2 / 3 | True / False | False / False / False |
| `CTNK.Reflector` | CTNK / variant | gated-candidate | nein | 1 / 2 | False / True | False / False / False |
| `HARV` | HARV / base | gated-candidate | nein | 0 / 0 | False / False | False / False / False |
| `HARV.Chrono` | HARV / variant | gated-candidate | nein | 0 / 0 | False / False | False / False / False |
| `IFV` | IFV / base | gated-candidate | ja | 2 / 19 | True / False | True / False / False |
| `IFV.AI` | IFV / variant | bot-gated | nein | 1 / 5 | False / False | False / False / False |
| `AA_Jeep` | JEEP / variant | gated-candidate | nein | 0 / 1 | False / False | False / False / False |
| `Heavy_Jeep` | JEEP / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `JEEP` | JEEP / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `Tow_Jeep` | JEEP / variant | gated-candidate | nein | 0 / 1 | False / False | False / False / False |
| `MCV` | MCV / base | gated-candidate | nein | 0 / 0 | False / False | False / False / True |
| `MCV.Nukular` | MCV / non-production form | no-buildable | nein | 0 / 0 | False / False | False / False / True |
| `MGG` | MGG / base | gated-candidate | nein | 0 / 0 | False / False | False / False / False |
| `MRJ` | MRJ / base | gated-candidate | nein | 0 / 0 | False / False | False / False / False |
| `PBLASTER` | Prismtank / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `PCAN` | Prismtank / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `Prismtank` | Prismtank / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `RTNK` | RTNK / base | gated-candidate | ja | 2 / 1 | False / False | False / False / False |
| `RTNK.PDL` | RTNK / variant | gated-candidate | nein | 3 / 2 | True / False | False / False / False |
| `RTNK.PDL.Firerate` | RTNK / variant | gated-candidate | nein | 3 / 2 | True / False | False / False / False |
| `RTNK.PDL.Toughness` | RTNK / variant | gated-candidate | nein | 3 / 2 | True / False | False / False / False |
| `RTNK.Reflector` | RTNK / variant | gated-candidate | nein | 2 / 1 | False / True | False / False / False |
| `RTNK.Reflector.Firerate` | RTNK / variant | gated-candidate | nein | 2 / 1 | False / True | False / False / False |
| `RTNK.Reflector.Toughness` | RTNK / variant | gated-candidate | nein | 2 / 1 | False / True | False / False / False |
| `TNKD` | TNKD / base | gated-candidate | ja | 0 / 1 | False / False | False / False / False |
| `TNKD.PDL` | TNKD / variant | gated-candidate | nein | 1 / 2 | True / False | False / False / False |
| `TNKD.PDL.Burstfire` | TNKD / variant | gated-candidate | nein | 1 / 2 | True / False | False / False / False |
| `TNKD.PDL.Tough` | TNKD / variant | gated-candidate | nein | 1 / 2 | True / False | False / False / False |
| `TNKD.Reflector` | TNKD / variant | gated-candidate | nein | 0 / 1 | False / True | False / False / False |
| `TNKD.Reflector.Burstfire` | TNKD / variant | gated-candidate | nein | 0 / 1 | False / True | False / False / False |
| `TNKD.Reflector.Tough` | TNKD / variant | gated-candidate | nein | 0 / 1 | False / True | False / False / False |

### Sowjets

| Actor | Familie / Kategorie | Produktion | Nativ | Türme / Kanäle | PDL / Reflector | Cargo / Deploy / Transform |
|---|---|---|---|---|---|---|
| `Rice_PassengerShell` |  / projectile | no-buildable | nein | 0 / 0 | False / False | False / False / False |
| `Rice_Shell` |  / projectile | no-buildable | nein | 0 / 0 | False / False | False / False / False |
| `V3` |  / projectile | no-buildable | nein | 0 / 0 | False / False | False / False / False |
| `V3B` |  / projectile | no-buildable | nein | 0 / 0 | False / False | False / False / False |
| `V4` |  / projectile | no-buildable | nein | 0 / 0 | False / False | False / False / False |
| `2S3` | 2S3 / base | gated-candidate | ja | 1 / 2 | False / False | False / False / False |
| `BTR` | BTR / base | gated-candidate | ja | 1 / 2 | False / False | True / False / False |
| `BTR.Surveillance` | BTR / variant | gated-candidate | nein | 0 / 0 | False / False | True / False / False |
| `Chem_Sprayer` | Chem_Sprayer / base | gated-candidate | ja | 0 / 1 | False / False | False / False / False |
| `Chem_Sprayer.Cloud` | Chem_Sprayer / variant | gated-candidate | nein | 0 / 1 | False / False | False / False / False |
| `Chem_Sprayer.Cloud.Metal_Acid` | Chem_Sprayer / variant | gated-candidate | nein | 0 / 1 | False / False | False / False / False |
| `Chem_Sprayer.Cloud.Spread` | Chem_Sprayer / variant | gated-candidate | nein | 0 / 1 | False / False | False / False / False |
| `Chem_Sprayer.Range` | Chem_Sprayer / variant | gated-candidate | nein | 0 / 1 | False / False | False / False / False |
| `Chem_Sprayer.Range.Metal_Acid` | Chem_Sprayer / variant | gated-candidate | nein | 0 / 1 | False / False | False / False / False |
| `Chem_Sprayer.Range.Spread` | Chem_Sprayer / variant | gated-candidate | nein | 0 / 1 | False / False | False / False / False |
| `Chem_Sprayer.Splash` | Chem_Sprayer / variant | gated-candidate | nein | 0 / 1 | False / False | False / False / False |
| `Chem_Sprayer.Splash.Metal_Acid` | Chem_Sprayer / variant | gated-candidate | nein | 0 / 1 | False / False | False / False / False |
| `Chem_Sprayer.Splash.Spread` | Chem_Sprayer / variant | gated-candidate | nein | 0 / 1 | False / False | False / False / False |
| `DTRK` | DTRK / base | gated-candidate | ja | 0 / 1 | False / False | False / True / False |
| `Devil_Tank` | Devil_Tank / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `Devil_Tank.PDL` | Devil_Tank / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `Devil_Tank.Reflector` | Devil_Tank / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |
| `FTRK` | FTRK / base | gated-candidate | ja | 1 / 2 | False / False | False / False / False |
| `FTRK.Barrage` | FTRK / variant | gated-candidate | nein | 1 / 2 | False / False | False / False / False |
| `Gene_Splicer` | Gene_Splicer / base | gated-candidate | ja | 2 / 4 | False / False | False / False / False |
| `HQ7` | HQ7 / base | gated-candidate | ja | 1 / 2 | False / False | False / False / False |
| `HTK5` | HTK5 / base | gated-candidate | ja | 1 / 2 | False / False | False / False / False |
| `Heavy_Tank` | Heavy_Tank / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `Heavy_Tank.AP` | Heavy_Tank / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `Heavy_Tank.AP.PDL` | Heavy_Tank / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `Heavy_Tank.AP.Reflector` | Heavy_Tank / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |
| `Heavy_Tank.Tesla` | Heavy_Tank / variant | gated-candidate | nein | 2 / 3 | False / False | False / False / False |
| `Heavy_Tank.Tesla.PDL` | Heavy_Tank / variant | gated-candidate | nein | 3 / 4 | True / False | False / False / False |
| `Heavy_Tank.Tesla.Reflector` | Heavy_Tank / variant | gated-candidate | nein | 2 / 3 | False / True | False / False / False |
| `Hyena` | Hyena / base | gated-candidate | ja | 1 / 6 | False / False | False / False / False |
| `ISU` | ISU / base | gated-candidate | ja | 0 / 2 | False / False | False / False / False |
| `ISU.AP` | ISU / variant | gated-candidate | nein | 0 / 2 | False / False | False / False / False |
| `ISU.AP.PDL` | ISU / variant | gated-candidate | nein | 1 / 3 | True / False | False / False / False |
| `ISU.AP.Reflector` | ISU / variant | gated-candidate | nein | 0 / 2 | False / True | False / False / False |
| `ISU.Cluster` | ISU / variant | gated-candidate | nein | 0 / 2 | False / False | False / False / False |
| `ISU.Cluster.PDL` | ISU / variant | gated-candidate | nein | 1 / 3 | True / False | False / False / False |
| `ISU.Cluster.Reflector` | ISU / variant | gated-candidate | nein | 0 / 2 | False / True | False / False / False |
| `ISU.Concussion` | ISU / variant | gated-candidate | nein | 0 / 2 | False / False | False / False / False |
| `ISU.Concussion.PDL` | ISU / variant | gated-candidate | nein | 1 / 3 | True / False | False / False / False |
| `ISU.Concussion.Reflector` | ISU / variant | gated-candidate | nein | 0 / 2 | False / True | False / False / False |
| `KATY` | KATY / base | gated-candidate | ja | 0 / 4 | False / False | False / False / False |
| `MCV.Nukular.Soviet` | MCV.Soviet / non-production form | no-buildable | nein | 0 / 0 | False / False | False / False / True |
| `MCV.Soviet` | MCV.Soviet / base | gated-candidate | nein | 0 / 0 | False / False | False / False / True |
| `NonaSVK` | NonaSVK / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `Peoples_Tank` | Peoples_Tank / base | gated-candidate | ja | 1 / 2 | False / False | False / False / False |
| `Peoples_Tank.Mass_Production` | Peoples_Tank / variant | gated-candidate | nein | 1 / 2 | False / False | False / False / False |
| `Peoples_Tank.Mass_Production.PDL` | Peoples_Tank / variant | gated-candidate | nein | 2 / 3 | True / False | False / False / False |
| `Peoples_Tank.Mass_Production.Reflector` | Peoples_Tank / variant | gated-candidate | nein | 1 / 2 | False / True | False / False / False |
| `Peoples_Tank.Speaker` | Peoples_Tank / variant | gated-candidate | nein | 1 / 2 | False / False | False / False / False |
| `Peoples_Tank.Speaker.PDL` | Peoples_Tank / variant | gated-candidate | nein | 2 / 3 | True / False | False / False / False |
| `Peoples_Tank.Speaker.Reflector` | Peoples_Tank / variant | gated-candidate | nein | 1 / 2 | False / True | False / False / False |
| `QTNK` | QTNK / base | gated-candidate | ja | 0 / 0 | False / False | False / True / False |
| `Rice_Cooker` | Rice_Cooker / base | gated-candidate | ja | 1 / 2 | False / False | True / False / False |
| `SFTNK` | SFTNK / prototype | disabled | nein | 0 / 1 | False / False | False / False / False |
| `Source_of_Pollution` | Source_of_Pollution / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `Source_of_Pollution.Chem_Bomb` | Source_of_Pollution / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `Source_of_Pollution.Chem_Bomb.PDL` | Source_of_Pollution / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `Source_of_Pollution.Chem_Bomb.Reflector` | Source_of_Pollution / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |
| `Source_of_Pollution.Chem_Spray` | Source_of_Pollution / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `Source_of_Pollution.Chem_Spray.PDL` | Source_of_Pollution / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `Source_of_Pollution.Chem_Spray.Reflector` | Source_of_Pollution / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |
| `Source_of_Pollution.Metal_Acid` | Source_of_Pollution / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `Source_of_Pollution.Metal_Acid.PDL` | Source_of_Pollution / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `Source_of_Pollution.Metal_Acid.Reflector` | Source_of_Pollution / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |
| `Soviet_Miner` | Soviet_Miner / base | gated-candidate | nein | 2 / 8 | False / False | False / False / False |
| `T-34` | T-34 / base | gated-candidate | ja | 1 / 2 | False / False | False / False / False |
| `TTNK` | TTNK / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `TTNK.Arc` | TTNK / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `TTNK.RA2` | TTNK.RA2 / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `TTNK.RA2.Arc` | TTNK.RA2 / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `Tsar_Tank` | Tsar_Tank / base | gated-candidate | ja | 1 / 2 | False / False | False / True / False |
| `V3BRL` | V3RL / prototype | disabled | nein | 0 / 1 | False / False | False / False / False |
| `V3RL` | V3RL / base | gated-candidate | ja | 0 / 1 | False / False | False / False / False |
| `V4RL` | V3RL / prototype | disabled | nein | 0 / 1 | False / False | False / False / False |
| `apoc` | apoc / base | gated-candidate | ja | 1 / 2 | False / False | False / False / False |
| `apoc.Drozd` | apoc / variant | gated-candidate | nein | 1 / 2 | False / False | False / False / False |
| `apoc.Drozd.PDL` | apoc / variant | gated-candidate | nein | 2 / 3 | True / False | False / False / False |
| `apoc.Drozd.Reflector` | apoc / variant | gated-candidate | nein | 1 / 2 | False / True | False / False / False |
| `apoc.Nuke` | apoc / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `apoc.Nuke.PDL` | apoc / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `apoc.Nuke.Reflector` | apoc / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |
| `apoc.Speaker` | apoc / variant | gated-candidate | nein | 1 / 2 | False / False | False / False / False |
| `apoc.Speaker.PDL` | apoc / variant | gated-candidate | nein | 2 / 3 | True / False | False / False / False |
| `apoc.Speaker.Reflector` | apoc / variant | gated-candidate | nein | 1 / 2 | False / True | False / False / False |
| `kims_wheel` | kims_wheel / base | gated-candidate | ja | 0 / 1 | False / False | False / False / False |

### Scrin

| Actor | Familie / Kategorie | Produktion | Nativ | Türme / Kanäle | PDL / Reflector | Cargo / Deploy / Transform |
|---|---|---|---|---|---|---|
| `ATMZ` | ATMZ / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `ATMZ.AA` | ATMZ / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `ATMZ.Range` | ATMZ / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `CORR` | CORR / base | gated-candidate | ja | 0 / 1 | False / False | False / False / False |
| `CORR.Area` | CORR / variant | gated-candidate | nein | 0 / 4 | False / False | False / False / False |
| `CORR.Area.PDL` | CORR / variant | gated-candidate | nein | 1 / 5 | True / False | False / False / False |
| `CORR.Area.Reflector` | CORR / variant | gated-candidate | nein | 0 / 4 | False / True | False / False / False |
| `CORR.Range` | CORR / variant | gated-candidate | nein | 0 / 1 | False / False | False / False / False |
| `CORR.Range.PDL` | CORR / variant | gated-candidate | nein | 1 / 2 | True / False | False / False / False |
| `CORR.Range.Reflector` | CORR / variant | gated-candidate | nein | 0 / 1 | False / True | False / False / False |
| `Channeler` | Channeler / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `Channeler.chain` | Channeler / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `Channeler.chain.pdl` | Channeler / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `Channeler.chain.reflector` | Channeler / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |
| `Channeler.disc` | Channeler / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `Channeler.disc.pdl` | Channeler / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `Channeler.disc.reflector` | Channeler / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |
| `DEVO` | DEVO / base | gated-candidate | ja | 2 / 1 | False / False | False / False / False |
| `DEVO.acid` | DEVO / variant | gated-candidate | nein | 2 / 2 | False / False | False / False / False |
| `DEVO.heavy` | DEVO / variant | gated-candidate | nein | 2 / 1 | False / False | False / False / False |
| `GUNW` | GUNW / base | gated-candidate | ja | 0 / 2 | False / False | False / False / False |
| `GUNW.sensor` | GUNW / variant | gated-candidate | nein | 0 / 2 | False / False | False / False / False |
| `HARV.Scrin` | HARV.Scrin / base | gated-candidate | nein | 0 / 0 | False / False | False / False / False |
| `Hexapod` | Hexapod / base | gated-candidate | ja | 0 / 2 | False / False | False / False / False |
| `INTL` | INTL / base | gated-candidate | ja | 1 / 1 | False / False | True / False / False |
| `INTL.AA` | INTL / variant | gated-candidate | nein | 1 / 1 | False / False | True / False / False |
| `INTL.Teleport` | INTL / variant | gated-candidate | nein | 1 / 1 | False / False | True / False / False |
| `LACE` | LACE / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `LACE.AP` | LACE / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `LACE.PDL` | LACE / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `LACE.Reflector` | LACE / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |
| `LCHR` | LCHR / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `LCHR.Drain` | LCHR / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `LCHR.Drain.PDL` | LCHR / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `LCHR.Drain.Reflector` | LCHR / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |
| `LCHR.Slow` | LCHR / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `LCHR.Slow.PDL` | LCHR / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `LCHR.Slow.Reflector` | LCHR / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |
| `RPTP` | RPTP / base | gated-candidate | ja | 2 / 1 | False / False | False / False / False |
| `RPTP.acid` | RPTP / variant | gated-candidate | nein | 2 / 2 | False / False | False / False / False |
| `RPTP.acid.PDL` | RPTP / variant | gated-candidate | nein | 3 / 3 | True / False | False / False / False |
| `RPTP.acid.reflector` | RPTP / variant | gated-candidate | nein | 2 / 2 | False / True | False / False / False |
| `RPTP.range` | RPTP / variant | gated-candidate | nein | 2 / 1 | False / False | False / False / False |
| `RPTP.range.PDL` | RPTP / variant | gated-candidate | nein | 3 / 2 | True / False | False / False / False |
| `RPTP.range.reflector` | RPTP / variant | gated-candidate | nein | 2 / 1 | False / True | False / False / False |
| `RUIN` | RUIN / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `RUIN.barrage` | RUIN / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `RUIN.splash` | RUIN / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `SEEK` | SEEK / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `SEEK.PDL` | SEEK / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `SEEK.Reflector` | SEEK / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |
| `SMCV` | SMCV / base | gated-candidate | nein | 0 / 0 | False / False | False / False / True |
| `SMCV.Nukular` | SMCV / non-production form | no-buildable | nein | 0 / 0 | False / False | False / False / True |
| `STCR` | STCR / base | gated-candidate | ja | 0 / 1 | False / False | False / False / False |
| `STCR.Arc` | STCR / variant | gated-candidate | nein | 0 / 2 | False / False | False / False / False |
| `STCR.Arc.PDL` | STCR / variant | gated-candidate | nein | 1 / 3 | True / False | False / False / False |
| `STCR.Arc.Reflector` | STCR / variant | gated-candidate | nein | 0 / 2 | False / True | False / False / False |
| `STCR.Range` | STCR / variant | gated-candidate | nein | 0 / 2 | False / False | False / False / False |
| `STCR.Range.PDL` | STCR / variant | gated-candidate | nein | 1 / 3 | True / False | False / False / False |
| `STCR.Range.Reflector` | STCR / variant | gated-candidate | nein | 0 / 2 | False / True | False / False / False |
| `TPOD` | TPOD / base | gated-candidate | ja | 1 / 1 | False / False | False / False / False |
| `TPOD.acid` | TPOD / variant | gated-candidate | nein | 1 / 2 | False / False | False / False / False |
| `TPOD.acid.pdl` | TPOD / variant | gated-candidate | nein | 2 / 3 | True / False | False / False / False |
| `TPOD.acid.reflector` | TPOD / variant | gated-candidate | nein | 1 / 2 | False / True | False / False / False |
| `TPOD.chain` | TPOD / variant | gated-candidate | nein | 1 / 1 | False / False | False / False / False |
| `TPOD.chain.PDL` | TPOD / variant | gated-candidate | nein | 2 / 2 | True / False | False / False / False |
| `TPOD.chain.reflector` | TPOD / variant | gated-candidate | nein | 1 / 1 | False / True | False / False / False |

