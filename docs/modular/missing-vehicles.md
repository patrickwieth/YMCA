# Was im Fahrzeugdesigner noch fehlt

Stand: aktuelle native Bindings einschließlich Titan, Slingshot und MARV; aus Regeln und Compiler-Bindungen erzeugt.

**Fehlend bedeutet nicht im Designer konfigurier-/kompilierbar.** Die normalen Fraktionsroster bleiben erhalten;
diese Fahrzeuge können dort bereits existieren. Tabellen-/Excel-Einträge und vorplatzierte Carryalls zählen nicht als Designer-Unterstützung.

In den sechs Bodenfahrzeug-Dateien: **77 vollständig fehlende Fraktions-Familiengruppen**:
**58 Kampf-/bewaffnete Transportgruppen**, **19 Wirtschafts-/Bau-/Transport-/Unterstützungsgruppen**.
Das sind redaktionelle Gruppen mit lokaler Produktionsdeklaration, keine vollständig aufgelöste Baubarkeitsprüfung.
Fraktionszuordnung folgt der Quelldatei, nicht exklusiver Verfügbarkeit. APC2, AMCV und HAR2 werden von GDI/Nod geteilt.
APCs, kämpfende Besatzungstransporter und Selbstmordfahrzeuge stehen unter Kampf; EMP/ECM/Radar unter Unterstützung.
Unterstützung bedeutet nicht zwingend unbewaffnet: insbesondere Soviet Miner und Outpost-Varianten sind Mischrollen.
Wracks, Projektile, Dummy-Akteure und angehängte Hilfsobjekte werden nicht als eigene Fahrzeuge gezählt.

## 1. Vollständig fehlende Boden-Kampffahrzeuge

### GDI (4)

- **Scout Drone** (`GDRN`)
- **Mini Drone** (`MDRN`)
- **Vulcan** (`VULC`)
- **X-O Powersuit** (`XO`)

### Nod (10)

- **Armored Personnel Carrier** (`APC2`)
- **Recon Bike** (`BIKE`)
- **Beam Cannon** (`Beam_Cannon`)
- **Devil's Tongue** (`FTNK`)
- **Heavy Flame Tank** (`HFTK`)
- **Howitzer** (`HOWI`)
- **Specter** (`SPEC`)
- **Stealth Tank** (`STNK`)
- **Chemical Truck** (`TTRK`)
- **Microwave Tank** (`WTNK`)

### China (4)

- **Bixi Dragon** (`Bixi`)
- **Inferno Cannon** (`charty`)
- **Heavy Troop Crawler** (`chcrawl2`)
- **Nuke Cannon** (`chnukecann`)

### Alliierte (9)

- **Scout Tank** (`1TNK`)
- **Armored Personnel Carrier** (`APC`)
- **Missile Fortress** (`BATF`)
- **Chrono Prison** (`CHPR`)
- **Cryo Launcher** (`CRYO`)
- **Chrono Tank** (`CTNK`)
- **Multi-Purpose Drone** (`IFV`)
- **Mirage Tank** (`RTNK`)
- **Tank Destroyer** (`TNKD`)

### Sowjets (21)

- **Akatsiya** (`2S3`)
- **Armored Personnel Carrier (BTR)** (`BTR`)
- **Chemical Sprayer** (`Chem_Sprayer`)
- **Demolition Truck** (`DTRK`)
- **Devil Tank** (`Devil_Tank`)
- **Gene Splicer** (`Gene_Splicer`)
- **HQ7 Missile Launcher** (`HQ7`)
- **HTK5 Missile Launcher** (`HTK5`)
- **Hyena Missile Launcher** (`Hyena`)
- **ISU-152** (`ISU`)
- **Katyusha** (`KATY`)
- **Nona SVK** (`NonaSVK`)
- **The People's Tank** (`Peoples_Tank`)
- **MAD Tank** (`QTNK`)
- **Kim's Giant Rice Cooker** (`Rice_Cooker`)
- **Source of Pollution** (`Source_of_Pollution`)
- **Tesla Tank** (`TTNK`)
- **Tsar Tank** (`Tsar_Tank`)
- **V3 Launcher** (`V3RL`)
- **Apocalypse Tank** (`apoc`)
- **Kim's Monowheel** (`kims_wheel`)

### Scrin (10)

- **Atomizer** (`ATMZ`)
- **Channeler Tank** (`Channeler`)
- **Hexapod Eradicator** (`Hexapod`)
- **Interloper** (`INTL`)
- **Lacerator** (`LACE`)
- **Leecher** (`LCHR`)
- **Heavy Tripod** (`RPTP`)
- **Ruiner** (`RUIN`)
- **Stormcrawler** (`STCR`)
- **Annihilator Tripod** (`TPOD`)

## 2. Vollständig fehlende andere Bodenfahrzeuge

### GDI (2)

- **Mobile E.M.P** (`MEMP`) — EMP-Unterstützung, nicht harmlos/unbewaffnet im taktischen Sinn
- **Mobile Sensor Array** (`MSAR`) — Sensoren / mobiles Radar

### Nod (4)

- **Mobile Construction Vehicle** (`AMCV`) — Baufahrzeug; gemeinsam GDI/Nod (~td)
- **Reinforcements Coordinator** (`COORDINATOR`) — Verstärkungs-/Produktionskoordination
- **Harvester** (`HAR2`) — Sammler; gemeinsam GDI/Nod (~td)
- **Mobile Stealth Generator** (`MSG`) — Tarnfeld-Unterstützung

### China (5)

- **Bulldozer (China)** (`CHNMCV`) — Baufahrzeug
- **Troop Crawler** (`chcrawl`) — Truppentransporter
- **ECM Tank** (`checm`) — Elektronische Kampfführung / Fahrzeugdeaktivierung
- **Harvester Truck** (`chharv`) — Sammler
- **Listening Outpost** (`choutpost`) — Aufklärung/Radar/Transport; bewaffnete Bunkervariante separat beachten

### Alliierte (4)

- **Ore Truck** (`HARV`) — Sammler inkl. Chrono Miner
- **Mobile Construction Vehicle** (`MCV`) — Baufahrzeug
- **Mobile Gap Generator** (`MGG`) — Sicht-/Gap-Unterstützung
- **Mobile Radar Jammer** (`MRJ`) — Radarstörung

### Sowjets (2)

- **MCV.Soviet** (`MCV.Soviet`) — Baufahrzeug
- **Soviet Miner** (`Soviet_Miner`) — Bewaffneter Sammler; primär Wirtschaft, nicht unbewaffnet

### Scrin (2)

- **Harvester** (`HARV.Scrin`) — Sammler
- **Colony Ship** (`SMCV`) — Baufahrzeug / Colony Ship

## 3. Fehlende Varianten bereits angebundener Familien

Hier fehlt nicht die ganze Familie. Eigenständige Varianten/Promotions sind nicht durch ihre Basiseinheit erledigt.
Bereits geerbte bedingte Waffen (z.B. T-34 Cluster, Dragon Black Napalm, Emperor-Zustand) sind dagegen kein zweites fehlendes Fahrzeug.

- **Alliierte / 2TNK** (vorhanden: Challenger_Tank): `Leclerc_Tank`, `Leopard_Tank`, `2TNK.Chrono`
- **Alliierte / JEEP** (vorhanden: JEEP): `Heavy_Jeep`, `Tow_Jeep`, `AA_Jeep`
- **Alliierte / Prismtank** (vorhanden: Prismtank): `PBLASTER`, `PCAN`
- **China / chbattle** (vorhanden: chbattle): `chbattle.Autoloader`, `chbattle.Autoloader.PDL`, `chbattle.Autoloader.Reflector`, `chbattle.Nuclear_Shells`, `chbattle.Nuclear_Shells.PDL`, `chbattle.Nuclear_Shells.Reflector`, `chbattle.Mass_Production`, `chbattle.Mass_Production.PDL`, `chbattle.Mass_Production.Reflector`
- **China / chdragon** (vorhanden: chdragon): `chdragon.PDL`, `chdragon.Reflector`
- **China / chgtnk** (vorhanden: chgtnk): `chgtnk.PDL`, `chgtnk.Reflector`
- **China / choverlord** (vorhanden: choverlord): `choverlord.Gatling`, `choverlord.Gatling.PDL`, `choverlord.Gatling.Reflector`, `choverlord.Bunker`, `choverlord.Bunker.PDL`, `choverlord.Bunker.Reflector`, `choverlord.Propaganda`, `choverlord.Propaganda.PDL`, `choverlord.Propaganda.Reflector`, `choverlord.Nuke_Shells`, `choverlord.Nuke_Shells.PDL`, `choverlord.Nuke_Shells.Reflector`, `choverlord.Plasma`, `choverlord.Plasma.PDL`, `choverlord.Plasma.Reflector`
- **GDI / DISR** (vorhanden: DISR): `DISR.PDL`, `DISR.Reflector`
- **GDI / Juggernaut** (vorhanden: Juggernaut): `Juggernaut.Emp`, `Juggernaut.Firerate`
- **GDI / MLRS** (vorhanden: MLRS): `MLRS.AA`, `MLRS.Hailstorm`
- **GDI / MTNK** (vorhanden: MTNK): `MTNK.PDL`, `MTNK.Reflector`
- **GDI / Mammoth** (vorhanden: Mammoth): `Mammoth.Ion`, `Mammoth.Ion.Reflector`, `Mammoth.Ion.PDL`, `Mammoth.Nanite`, `Mammoth.Nanite.Reflector`, `Mammoth.Nanite.PDL`, `Mammoth.Hover`, `Mammoth.Hover.Reflector`, `Mammoth.Hover.PDL`
- **GDI / TITN** (vorhanden: TITN): `TITN.Battle`, `TITN.Battle.PDL`, `TITN.Battle.Reflector`, `TITN.Railgun`, `TITN.Railgun.PDL`, `TITN.Railgun.Reflector`
- **GDI / hmlrs** (vorhanden: hmlrs): `hmlrs.Reflector`, `hmlrs.PDL`
- **Nod / BGGY** (vorhanden: BGGY): `BGGY.PDL`, `BGGY.AA`
- **Nod / LTNK** (vorhanden: LTNK): `LTNK.Laser`
- **Nod / SSM** (vorhanden: SSM): `SSM.Toxin`, `SSM.Multi`, `SSM.Bunkerbuster`
- **Scrin / CORR** (vorhanden: CORR): `CORR.Area`, `CORR.Area.PDL`, `CORR.Area.Reflector`, `CORR.Range`, `CORR.Range.PDL`, `CORR.Range.Reflector`
- **Scrin / DEVO** (vorhanden: DEVO): `DEVO.acid`, `DEVO.heavy`
- **Scrin / GUNW** (vorhanden: GUNW): `GUNW.sensor`
- **Scrin / SEEK** (vorhanden: SEEK): `SEEK.PDL`, `SEEK.Reflector`
- **Sowjets / FTRK** (vorhanden: FTRK): `FTRK.Barrage`
- **Sowjets / Heavy_Tank** (vorhanden: Heavy_Tank): `Heavy_Tank.AP`, `Heavy_Tank.AP.PDL`, `Heavy_Tank.AP.Reflector`, `Heavy_Tank.Tesla`, `Heavy_Tank.Tesla.PDL`, `Heavy_Tank.Tesla.Reflector`
- **Sowjets / TTNK.RA2** (vorhanden: TTNK.RA2): `TTNK.RA2.Arc`

## 4. Alle Varianten vollständig fehlender Bodenfamilien

Basisakteure und Varianten als Prüfliste; nicht als zusätzliche Familien zählen.

- **Alliierte / 1TNK**: `1TNK`, `1TNK.PDL`, `1TNK.Reflector`
- **Alliierte / APC**: `APC`
- **Alliierte / BATF**: `BATF`, `BATF.Bunker`, `BATF.Bunker.PDL`, `BATF.Bunker.Reflector`, `BATF.Prism`, `BATF.Prism.PDL`, `BATF.Prism.Reflector`, `BATF.Support`, `BATF.Support.PDL`, `BATF.Support.Reflector`, `BATF.Artillery`, `BATF.Artillery.PDL`, `BATF.Artillery.Reflector`, `BATF.AI`
- **Alliierte / CHPR**: `CHPR`, `CHPR.Range`, `CHPR.AA`
- **Alliierte / CRYO**: `CRYO`
- **Alliierte / CTNK**: `CTNK`, `CTNK.PDL`, `CTNK.Reflector`
- **Alliierte / HARV**: `HARV`, `HARV.Chrono`
- **Alliierte / IFV**: `IFV`, `IFV.AI`
- **Alliierte / MCV**: `MCV`, `MCV.Nukular`
- **Alliierte / MGG**: `MGG`
- **Alliierte / MRJ**: `MRJ`
- **Alliierte / RTNK**: `RTNK`, `RTNK.PDL`, `RTNK.Reflector`, `RTNK.PDL.Firerate`, `RTNK.Reflector.Firerate`, `RTNK.PDL.Toughness`, `RTNK.Reflector.Toughness`
- **Alliierte / TNKD**: `TNKD`, `TNKD.PDL`, `TNKD.Reflector`, `TNKD.PDL.Burstfire`, `TNKD.Reflector.Burstfire`, `TNKD.PDL.Tough`, `TNKD.Reflector.Tough`
- **China / Bixi**: `Bixi`
- **China / CHNMCV**: `CHNMCV`, `CHNMCV.Nukular`
- **China / charty**: `charty`
- **China / chcrawl**: `chcrawl`
- **China / chcrawl2**: `chcrawl2`, `chcrawl2.Hunter`, `chcrawl2.Hunter.PDL`, `chcrawl2.Hunter.Reflector`, `chcrawl2.Assault`, `chcrawl2.Assault.PDL`, `chcrawl2.Assault.Reflector`
- **China / checm**: `checm`, `checm.pulse`, `checm.focus`, `checm.chain`
- **China / chharv**: `chharv`
- **China / chnukecann**: `chnukecann`, `chnukecann.Range`, `chnukecann.Neutron`
- **China / choutpost**: `choutpost`, `choutpost.Propaganda`, `choutpost.Propaganda.PDL`, `choutpost.Propaganda.Reflector`, `choutpost.Bunker`, `choutpost.Bunker.PDL`, `choutpost.Bunker.Reflector`
- **GDI / GDRN**: `GDRN`
- **GDI / MDRN**: `MDRN`
- **GDI / MEMP**: `MEMP`, `MEMP.Volatile`, `MEMP.Ranged`, `MEMP.Ranged.Improved`
- **GDI / MSAR**: `MSAR`, `MSAR.PDL`
- **GDI / VULC**: `VULC`
- **GDI / XO**: `XO`
- **Nod / AMCV**: `AMCV`, `AMCV.Nukular`
- **Nod / APC2**: `APC2`, `APC2.Reinforce`
- **Nod / BIKE**: `BIKE`, `BIKE.Scrin`, `BIKE.Scrin.PDL`, `BIKE.Scrin.Reflector`, `BIKE.RocketHail`, `BIKE.RocketHail.PDL`, `BIKE.RocketHail.Reflector`, `BIKE.Explosive`, `BIKE.Explosive.PDL`, `BIKE.Explosive.Reflector`
- **Nod / Beam_Cannon**: `Beam_Cannon`
- **Nod / COORDINATOR**: `COORDINATOR`
- **Nod / FTNK**: `FTNK`, `FTNK.Reflector`, `FTNK.PDL`, `FTNK.PDL.Chem`, `FTNK.Reflector.Chem`
- **Nod / HAR2**: `HAR2`
- **Nod / HFTK**: `HFTK`, `HFTK.PDL`, `HFTK.Reflector`, `HFTK.PDL.Fireball`, `HFTK.Reflector.Fireball`
- **Nod / HOWI**: `HOWI`
- **Nod / MSG**: `MSG`
- **Nod / SPEC**: `SPEC`
- **Nod / STNK**: `STNK`, `STNK.Scrin`, `STNK.Scrin.PDL`, `STNK.Scrin.Reflector`, `STNK.HE`, `STNK.HE.PDL`, `STNK.HE.Reflector`, `STNK.AP`, `STNK.AP.PDL`, `STNK.AP.Reflector`
- **Nod / TTRK**: `TTRK`
- **Nod / WTNK**: `WTNK`
- **Scrin / ATMZ**: `ATMZ`, `ATMZ.AA`, `ATMZ.Range`
- **Scrin / Channeler**: `Channeler`, `Channeler.chain`, `Channeler.disc`, `Channeler.disc.reflector`, `Channeler.disc.pdl`, `Channeler.chain.reflector`, `Channeler.chain.pdl`
- **Scrin / HARV.Scrin**: `HARV.Scrin`
- **Scrin / Hexapod**: `Hexapod`
- **Scrin / INTL**: `INTL`, `INTL.AA`, `INTL.Teleport`
- **Scrin / LACE**: `LACE`, `LACE.PDL`, `LACE.Reflector`, `LACE.AP`
- **Scrin / LCHR**: `LCHR`, `LCHR.Slow`, `LCHR.Slow.PDL`, `LCHR.Slow.Reflector`, `LCHR.Drain`, `LCHR.Drain.PDL`, `LCHR.Drain.Reflector`
- **Scrin / RPTP**: `RPTP`, `RPTP.range`, `RPTP.range.reflector`, `RPTP.range.PDL`, `RPTP.acid`, `RPTP.acid.reflector`, `RPTP.acid.PDL`
- **Scrin / RUIN**: `RUIN`, `RUIN.barrage`, `RUIN.splash`
- **Scrin / SMCV**: `SMCV`, `SMCV.Nukular`
- **Scrin / STCR**: `STCR`, `STCR.Range`, `STCR.Range.PDL`, `STCR.Range.Reflector`, `STCR.Arc`, `STCR.Arc.PDL`, `STCR.Arc.Reflector`
- **Scrin / TPOD**: `TPOD`, `TPOD.chain`, `TPOD.chain.reflector`, `TPOD.chain.PDL`, `TPOD.acid`, `TPOD.acid.reflector`, `TPOD.acid.pdl`
- **Sowjets / 2S3**: `2S3`
- **Sowjets / BTR**: `BTR`, `BTR.Surveillance`
- **Sowjets / Chem_Sprayer**: `Chem_Sprayer`, `Chem_Sprayer.Range`, `Chem_Sprayer.Range.Metal_Acid`, `Chem_Sprayer.Range.Spread`, `Chem_Sprayer.Splash`, `Chem_Sprayer.Splash.Metal_Acid`, `Chem_Sprayer.Splash.Spread`, `Chem_Sprayer.Cloud`, `Chem_Sprayer.Cloud.Metal_Acid`, `Chem_Sprayer.Cloud.Spread`
- **Sowjets / DTRK**: `DTRK`
- **Sowjets / Devil_Tank**: `Devil_Tank`, `Devil_Tank.PDL`, `Devil_Tank.Reflector`
- **Sowjets / Gene_Splicer**: `Gene_Splicer`
- **Sowjets / HQ7**: `HQ7`
- **Sowjets / HTK5**: `HTK5`
- **Sowjets / Hyena**: `Hyena`
- **Sowjets / ISU**: `ISU`, `ISU.Concussion`, `ISU.Concussion.Reflector`, `ISU.Concussion.PDL`, `ISU.Cluster`, `ISU.Cluster.Reflector`, `ISU.Cluster.PDL`, `ISU.AP`, `ISU.AP.Reflector`, `ISU.AP.PDL`
- **Sowjets / KATY**: `KATY`
- **Sowjets / MCV.Soviet**: `MCV.Soviet`, `MCV.Nukular.Soviet`
- **Sowjets / NonaSVK**: `NonaSVK`
- **Sowjets / Peoples_Tank**: `Peoples_Tank`, `Peoples_Tank.Speaker`, `Peoples_Tank.Speaker.Reflector`, `Peoples_Tank.Speaker.PDL`, `Peoples_Tank.Mass_Production`, `Peoples_Tank.Mass_Production.Reflector`, `Peoples_Tank.Mass_Production.PDL`
- **Sowjets / QTNK**: `QTNK`
- **Sowjets / Rice_Cooker**: `Rice_Cooker`
- **Sowjets / Source_of_Pollution**: `Source_of_Pollution`, `Source_of_Pollution.Metal_Acid`, `Source_of_Pollution.Metal_Acid.Reflector`, `Source_of_Pollution.Metal_Acid.PDL`, `Source_of_Pollution.Chem_Spray`, `Source_of_Pollution.Chem_Spray.Reflector`, `Source_of_Pollution.Chem_Spray.PDL`, `Source_of_Pollution.Chem_Bomb`, `Source_of_Pollution.Chem_Bomb.Reflector`, `Source_of_Pollution.Chem_Bomb.PDL`
- **Sowjets / Soviet_Miner**: `Soviet_Miner`
- **Sowjets / TTNK**: `TTNK`, `TTNK.Arc`
- **Sowjets / Tsar_Tank**: `Tsar_Tank`
- **Sowjets / V3RL**: `V3RL`, `V3BRL`, `V4RL`
- **Sowjets / apoc**: `apoc`, `apoc.Speaker`, `apoc.Speaker.Reflector`, `apoc.Speaker.PDL`, `apoc.Nuke`, `apoc.Nuke.Reflector`, `apoc.Nuke.PDL`, `apoc.Drozd`, `apoc.Drozd.Reflector`, `apoc.Drozd.PDL`
- **Sowjets / kims_wheel**: `kims_wheel`

## 5. Sonder-/Prüffälle am Boden

- `SFTNK` — sowjetischer Flame Tank: Gruppe ohne bestätigte lokale Produktionsdeklaration; separat prüfen.
- `TRUK` — Supply Truck: Basis lokal `~disabled`; `TRUK.Test` besitzt eine Produktionsdeklaration mit `promotion.oil_pumps`. Fehlende Versorgungseinheit außerhalb der sechs Fraktionsdateien.
- `TRUK.DROP` — abgeworfene, nicht normal produzierbare Form; kein zusätzlicher Fahrzeugtyp.
- `CHTRUK` — chinesische Supply-Truck-Ausführung, erbt `TRUK` einschließlich dessen Einschränkungen; separat von regulären Sammlern prüfen.
- `CDRN` — Chaos Drone: lokal `~disabled`; fehlender deaktivierter Kampf-/Kontrollprototyp, nicht als regulär baubares Fahrzeug gezählt.

## 6. Luft- und Wasserfahrzeuge: ebenfalls vollständig außerhalb des Designers

Keine Flugzeug-/Schiffsbindung ist implementiert. Folgende vollständige Liste lokaler Produktionsdeklarationen
enthält bewusst Varianten, mit Actor-ID zur Unterscheidung. Keine Familien-Gesamtsumme daraus ableiten.
Baubarkeit kann zusätzlich von Promotion, Fraktion oder Spielmodus abhängen. Bewaffnete Transporte sind Mischrollen.

**Transport/Unterstützung statt primärem Kampffahrzeug:** Chinook (`TRAN`), Carryall (`OCAR`/`OCAR.Eagle`),
Dropship, Airship of the People (`BLIMP`) und Landungsboot (`LST`). Auch C-17-Versorgungsflugzeuge,
UAV und U-2 fehlen als Designer-Funktionen, sind aber keine normalen Produktionsfamilien.
**Bewaffnete Mischrollen:** Combat Chinook (`TRAN.Eagle`), Seahawk, Helix und Carrier samt Trägerdrohnen.
Die übrigen unten aufgeführten regulären Flugzeuge/Schiffe sind Kampfverbände; ihre Upgrades bleiben jeweils sichtbar.

### `mods/ca/rules/china/aircraft.yaml`

- MiG (`CHMIG`)
- MiG Fighter (`CHMIG.AA`)
- MiG Napalm Bomber (`CHMIG.Napalm`)
- MiG Tactical Nuclear Bomber (`CHMIG.Nuke`)
- Helix (`chhelix`)
- Gatling Helix (`chhelix.Gatling`)
- Napalm Helix (`chhelix.Napalm`)
- Nuke Helix (`chhelix.Nuke`)
- Bunker Helix (`chhelix.Bunker`)
- chhelix.MortarBunker (`chhelix.MortarBunker`)
- Torpedo Helix (`chhelix.Torpedo`)

### `mods/ca/rules/gdi/aircraft.yaml`

- A-10 Warthog (`A10`)
- Stealth Fighter (`Stealth_Fighter`)
- Stealth Fighter (Double Payload) (`Stealth_Fighter.payload`)
- Orca (`ORCA`)
- Orca (AP) (`ORCA.AP`)
- Orca (Sonic) (`ORCA.Sonic`)
- Orca (Fire Rate) (`ORCA.Payload`)
- Orca (Torpedo) (`ORCA.Torp`)
- Orca V2 (`OrcaV2`)
- Orca V2 (Swarm) (`OrcaV2.Swarm`)
- Orca V2 (EMP) (`OrcaV2.EMP`)
- Orca V2 (Torpedo) (`OrcaV2.Torp`)
- Orca Bomber (should not be visible to you) (`Orca_Bomber`) **lokal deaktiviert**
- Orca Bomber (EMP) (`orca_bomber.EMP`)
- Orca Bomber (HE) (`orca_bomber.HE`)
- Orca Bomber (AP) (`orca_bomber.AP`)
- Orca Ion Warship (`Orca.Warship`)

### `mods/ca/rules/nod/aircraft.yaml`

- Banshee (`SCRN`)
- Banshee Bomber (`SCRN.Bomber`)
- Banshee (Payload) (`SCRN.Payload`)
- Comanche (`RAH66`)
- Comanche Bomber (`RAH66.Bomber`)
- Harpy (`APCH`)
- Harpy Flyby (`APCH.Flyby`)
- Harpy (Torpedo) (`APCH.Torp`)
- Venom (`VENM`)
- Venom (Focus) (`VENM.Payload`)
- Venom (Heavy Laser) (`VENM.HeavyLaser`)

### `mods/ca/rules/allies/aircraft.yaml`

- Eurofighter (`Eurofighter`)
- Rafale (`Rafale`)
- Aurora (`AURORA`)
- Harrier (`HARR`)
- Harrier (Avionics) (`HARR.avionics`)
- Harrier (Payload) (`HARR.payload`)
- Seahawk (`Seahawk`)
- Seahawk (Bunker Busters) (`Seahawk.armed`)
- Eurocopter Tiger (`HELI`)
- Eurocopter (Cryo) (`HELI.Cryo`)
- Eurocopter (Artillery) (`HELI.Artillery`)
- Eurocopter (Fire Rate) (`HELI.Fire_Rate`)
- Eurocopter (Torpedo) (`HELI.Torp`)
- Huey UH-1 (`HUEY`)
- Recon Huey (`HUEY.recon`)
- Cryo Huey (`HUEY.cryo`)
- Grenade Huey (`HUEY.Grenade`)

### `mods/ca/rules/soviet/aircraft.yaml`

- Yak Attack Plane (`YAK`)
- Yak Toxin Plane (`CHEMYAK`)
- Yak Toxin Plane (Volatile) (`CHEMYAK.Volatile`)
- Hind (`Hind`)
- Tesla Hind (`Hind.Tesla`)
- Hind (Missiles) (`Hind.Missile`)
- Hind (Radcannon) (`Hind.Radcannon`)
- Hind (Torpedo) (`Hind.Torp`)
- MiG-31 (`MIG`)
- Nuclear Bomber (`HeavyBomber.Nuke`)
- Heavy Tesla Bomber (`HeavyBomber.Tesla`)
- SU-47 Fighter (`SU47`)
- SU-25 Frogfoot (`FROG`)
- SU-25 Frogfoot Dirty Bomber (`SU25.dirty`)
- Il-11B Bomber (`Il11b`)
- SU-27 Flanker (`SUK`)
- SU-27 Flanker (Concussion) (`SUK.Conc`)
- Kirov Airship (`Kirov`)
- Airship of the People (`BLIMP`)
- Kirov V3 System (`Kirov.V3`)
- Nuke Kirov (`Kirov.Demo`)
- Mecha Kirov (`Kirov.Mecha`)
- Mecha Kirov (Tesla) (`Kirov.Mecha.Tesla`)
- Kirov Carrier (`Kirov.Carrier`)

### `mods/ca/rules/scrin/aircraft.yaml`

- Stormrider (`STMR`)
- Stormhunter (`STMR.hunter`)
- Stormbomber (`STMR.bomber`)
- Stormbomber (Torpedo) (`STMR.torp`)
- Devastator Warship (`DEVA`)
- Devastator Shardlauncher (`DEVA.Shards`)
- Devastator Rift Opener (`DEVA.Rift`)
- Mothership (`MSHP`)
- Bastion (`Bastion`)
- Planetary Assault Carrier (`PAC`)
- Suicide Drone Carrier (`PAC.suicide`)
- Support Carrier (`PAC.support`)
- Planetary Assault Warship (`PAC.laser`)

### `mods/ca/rules/aircraft.yaml`

- Chinook (`TRAN`)
- Combat Chinook (`TRAN.Eagle`)
- Carryall (`OCAR`) **lokal deaktiviert**
- Carryall (Auto) (`OCAR.Eagle`)

### `mods/ca/rules/misc/aircraft.yaml`

- Dropship Transport (`Dropship`)

### `mods/ca/rules/misc/ships.yaml`

- Transport (`LST`)
- Subchaser (`Subchaser`)
- Hunter Submarine (`SS2`)
- SS2.ai (`SS2.ai`)
- Patrol Boat (`PT2`)
- Recon Boat (`SB`)
- Sea Scorpion (`SEAS`)
- Attack Submarine (`SS`)
- Destroyer (`DD`)
- Frigate (`DD2`)
- Aegis Cruiser (`Aegis_Cruiser`)
- Aegis_Cruiser.ai (`Aegis_Cruiser.ai`)
- Battlecruiser (`CA`)
- ICBM Submarine (`ISUB`)
- Drone Carrier (`CARR`)
- Missile Submarine (`MSUB`)
- MSUB.ai (`MSUB.ai`)
- Flame Boat (`chflameboat`)
- Atomic Submarine (`chasub`)

### Nicht reguläre Produktionsformen / Support-Power- und Trägerobjekte

Rohinventar ohne lokale Buildable-Deklaration: umfasst echte Einsatzflugzeuge, Transportformen,
Drohnen, aber auch Abwurfkapseln, Raketen und Dummy-Objekte. Nicht pauschal als fehlende Fahrzeugfamilien zählen.

- `xianh8` — Xian H-8 (`mods/ca/rules/china/aircraft.yaml`)
- `xianh8.carpet` — Xian H-8 Bomber (`mods/ca/rules/china/aircraft.yaml`)
- `xianh8.nuke` — Xian H-8 Bomber (`mods/ca/rules/china/aircraft.yaml`)
- `xianh8.emp` — Xian H-8 Bomber (`mods/ca/rules/china/aircraft.yaml`)
- `xianh8.minebomb` — Xian H-8 Bomber (`mods/ca/rules/china/aircraft.yaml`)
- `A10.bomber` — Warthog (`mods/ca/rules/gdi/aircraft.yaml`)
- `YF23.Bomber` — Black Widow (`mods/ca/rules/gdi/aircraft.yaml`)
- `POD` — Drop Pod (`mods/ca/rules/gdi/aircraft.yaml`)
- `POD2` — geerbt / ohne lokalen Namen (`mods/ca/rules/gdi/aircraft.yaml`)
- `POD3` — geerbt / ohne lokalen Namen (`mods/ca/rules/gdi/aircraft.yaml`)
- `Orca.Drone` — Ion Drone (`mods/ca/rules/gdi/aircraft.yaml`)
- `dummy.ionstrike` — geerbt / ohne lokalen Namen (`mods/ca/rules/gdi/aircraft.yaml`)
- `Tarantula` — Tarantula Attack Plane (`mods/ca/rules/allies/aircraft.yaml`)
- `AtomParabomb` — Nuclear Parabomb (`mods/ca/rules/soviet/aircraft.yaml`)
- `TeslaParabomb` — Nuclear Parabomb (`mods/ca/rules/soviet/aircraft.yaml`)
- `V3.Kirov` — geerbt / ohne lokalen Namen (`mods/ca/rules/soviet/aircraft.yaml`)
- `Yak41` — Yak-41 (`mods/ca/rules/soviet/aircraft.yaml`)
- `U2` — Spy Plane (`mods/ca/rules/soviet/aircraft.yaml`)
- `BorisMig` — Supersonic Bomber (`mods/ca/rules/soviet/aircraft.yaml`)
- `INVA` — Invader (`mods/ca/rules/scrin/aircraft.yaml`)
- `INVA.support` — Invader (`mods/ca/rules/scrin/aircraft.yaml`)
- `INVA.suicide` — geerbt / ohne lokalen Namen (`mods/ca/rules/scrin/aircraft.yaml`)
- `BADR` — C-130 (`mods/ca/rules/aircraft.yaml`)
- `BADR.Bomber` — C-130 (`mods/ca/rules/aircraft.yaml`)
- `BADR.CBomber` — geerbt / ohne lokalen Namen (`mods/ca/rules/aircraft.yaml`)
- `BADR.NBomber` — geerbt / ohne lokalen Namen (`mods/ca/rules/aircraft.yaml`)
- `BADR.MBomber` — geerbt / ohne lokalen Namen (`mods/ca/rules/aircraft.yaml`)
- `B2B` — B2 Stealth Bomber (`mods/ca/rules/aircraft.yaml`)
- `TRAN.paradrop` — Chinook (Paradrop) (`mods/ca/rules/aircraft.yaml`)
- `C17` — Supply Aircraft (`mods/ca/rules/aircraft.yaml`)
- `C17.Cargo` — geerbt / ohne lokalen Namen (`mods/ca/rules/aircraft.yaml`)
- `C17.No_Color` — geerbt / ohne lokalen Namen (`mods/ca/rules/aircraft.yaml`)
- `C17.Clustermines` — geerbt / ohne lokalen Namen (`mods/ca/rules/aircraft.yaml`)
- `UAV` — Recon Drone (`mods/ca/rules/aircraft.yaml`)
- `OCAR.Reinforce` — geerbt / ohne lokalen Namen (`mods/ca/rules/aircraft.yaml`)
- `ICBM` — ICBM (`mods/ca/rules/misc/ships.yaml`)
- `HORN` — Hornet (`mods/ca/rules/misc/ships.yaml`)

## Quellen und Grenzen

- `OpenRA.Mods.CA/Modular/CustomVehicleAssembly.cs`: tatsächliche Compiler-Bindungen, nicht Kalibrierungsreferenzen.
- `mods/ca/rules/{gdi,nod,china,allies,soviet,scrin}/vehicles.yaml`: Hauptinventar.
- `tools/modular/vehicle-family-policy.json`: redaktionelle Familiengruppierung.
- Die eingebundenen GLA-/Yuri-Fahrzeugdateien sind derzeit leer; USA hat ebenfalls keine Fahrzeugdefinitionen.
- Infanterie, Gebäude, zivile Kartendekorationen und Wracks sind nicht Teil dieses Fahrzeugdesigner-Backlogs.
- Familienstatus beruht auf lokalen Regeln. Kein allgemeiner MiniYaml-Vererbungsresolver und kein Beweis, dass jede Variante heute baubar ist.
