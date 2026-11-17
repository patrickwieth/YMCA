# Bestehende Luft- und Wasserfahrzeuge: aufgelöste Aufbauten

**160 konkrete Actor-Deklarationen** aus neun Flugzeug-/Schiffsdateien, einschließlich Varianten und Hilfsobjekten.
Keine Familienzahl: eine SHP-/Voxelvariante, ein Support-Power-Flugzeug und eine Trägerdrohne sind nicht automatisch unabhängige Fahrzeugfamilien.

## Strukturelle Einordnung

| Kategorie | Anzahl |
|---|---:|
| bot-gated-vehicle | 3 |
| carrier/drone-slave | 6 |
| disabled-vehicle | 2 |
| non-mobile/review | 4 |
| non-production/support/review | 24 |
| production-candidate | 114 |
| spawned-missile | 4 |
| wreck | 3 |

Der Produktionsstatus kommt aus **aufgelösten Buildable-Traits**, nicht nur lokalen Deklarationen.
`production-candidate` heißt nicht überall sofort baubar: Tech, Nation, Promotions und Spielmodus gelten weiterhin.
**Geerbte Buildable-Kandidaten ohne lokale Buildable-Deklaration:** `B2B`, `YF23.Bomber`.

Daten: `tools/modular/air-naval-loadouts.json`. Identische Trait-Dokumentationen sind über `trait_definitions` dedupliziert.
Enthalten: Waffen/Conditions, Aircraft-Flug-/Landeparameter, Mobile-Locomotor, Munition/Nachladen, Rearmable-Basen,
Cargo, Carryall, Träger-/Drohnen-/Raketen-Spawner, Grafik und PDL/Reflector. Geschwindigkeiten kommen bei Aircraft
aus AircraftInfo – nicht aus einem fälschlich vorausgesetzten Mobile-Trait. Werte sind Rohwerte, keine effektiven Upgrade-Werte.

Die Felder sind Dokumentationsstrings, kein verlustfreies MiniYaml. Helpers/Raketen/Wracks werden nicht als reguläre
Produktionsfahrzeuge gezählt; `non-mobile/review` enthält beispielsweise Helix-Ausrüstungs-Upgrades.
Unklare Nichtproduktionsobjekte bleiben bewusst review statt automatisch als Support-Flugzeuge klassifiziert zu werden.

**Keine neue Designer-Freigabe und keine neuen Strom-/Balancewerte.** Die bisherige Boden-Baukastenabdeckung bleibt unverändert.

## Träger, Transport und Wiederbewaffnung

| Actor | Rolle | Mechaniken | Spawner-Akteure / Rearm-Basen |
|---|---|---|---|
| `BADR` | non-production/support/review | Cargo |  |
| `C17` | non-production/support/review | Cargo |  |
| `C17.Cargo` | non-production/support/review | Cargo |  |
| `C17.Clustermines` | non-production/support/review | Cargo |  |
| `C17.No_Color` | non-production/support/review | Cargo |  |
| `OCAR` | disabled-vehicle | Carryall, Cargo |  |
| `OCAR.Eagle` | production-candidate | AutoCarryall, Cargo |  |
| `OCAR.Reinforce` | non-production/support/review | Carryall, Cargo |  |
| `TRAN` | production-candidate | Cargo |  |
| `TRAN.Eagle` | production-candidate | Cargo |  |
| `TRAN.paradrop` | non-production/support/review | Cargo |  |
| `AURORA` | production-candidate | Rearm | `afld, afld.gdi, afld.allies` |
| `Eurofighter` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `HARR` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `HARR.avionics` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `HARR.payload` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `HUEY.recon` | production-candidate | Cargo |  |
| `Rafale` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `Seahawk` | production-candidate | Cargo |  |
| `Seahawk.armed` | production-candidate | Cargo, Rearm | `hpad, hpad.soviet, hpad.td, hpad.td.nod` |
| `CHMIG` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `CHMIG.AA` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `CHMIG.Napalm` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `CHMIG.Nuke` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `chhelix` | production-candidate | Cargo |  |
| `chhelix.Bunker` | production-candidate | Cargo |  |
| `chhelix.Gatling` | production-candidate | Cargo |  |
| `chhelix.MortarBunker` | production-candidate | Cargo |  |
| `chhelix.Napalm` | production-candidate | Cargo |  |
| `chhelix.Nuke` | production-candidate | Cargo |  |
| `chhelix.Torpedo` | production-candidate | Cargo |  |
| `xianh8` | non-production/support/review | Cargo |  |
| `xianh8.carpet` | non-production/support/review | Cargo |  |
| `xianh8.emp` | non-production/support/review | Cargo |  |
| `xianh8.minebomb` | non-production/support/review | Cargo |  |
| `xianh8.nuke` | non-production/support/review | Cargo |  |
| `A10` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `A10.bomber` | non-production/support/review | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `Orca.Drone` | carrier/drone-slave | Rearm | `Orca.Warship` |
| `Orca.Warship` | production-candidate | CarrierMaster | `Orca.Drone` |
| `Stealth_Fighter` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `Stealth_Fighter.payload` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `Dropship` | production-candidate | Cargo |  |
| `CARR` | production-candidate | CarrierMaster | `horn, horn, horn, horn, horn, horn, horn, horn` |
| `HORN` | carrier/drone-slave | Rearm | `carr` |
| `ISUB` | production-candidate | MissileSpawner | `ICBM` |
| `LST` | production-candidate | Cargo |  |
| `SCRN` | production-candidate | Rearm | `hpad, hpad.soviet, hpad.td, hpad.td.nod` |
| `SCRN.Bomber` | production-candidate | Rearm | `hpad, hpad.soviet, hpad.td, hpad.td.nod` |
| `SCRN.Payload` | production-candidate | Rearm | `hpad, hpad.soviet, hpad.td, hpad.td.nod` |
| `INVA` | carrier/drone-slave | Rearm | `pac` |
| `INVA.suicide` | carrier/drone-slave | Rearm | `pac` |
| `INVA.support` | carrier/drone-slave | Rearm | `pac` |
| `PAC` | production-candidate | CarrierMaster | `inva, inva, inva, inva` |
| `PAC.suicide` | production-candidate | CarrierMaster | `inva.suicide, inva.suicide, inva.suicide, inva.suicide` |
| `PAC.support` | production-candidate | CarrierMaster | `inva.support, inva.support, inva.support, inva.support` |
| `BorisMig` | non-production/support/review | Rearm | `bori` |
| `CHEMYAK` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `CHEMYAK.Volatile` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `FROG` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `HeavyBomber.Nuke` | production-candidate | Rearm, MissileSpawner | `AtomParabomb`; `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `HeavyBomber.Tesla` | production-candidate | Rearm, MissileSpawner | `TeslaParabomb`; `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `Il11b` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `Kirov.Carrier` | production-candidate | CarrierMaster | `Yak41, Yak41, Yak41, Yak41` |
| `Kirov.V3` | production-candidate | MissileSpawner | `V3.Kirov` |
| `MIG` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `SU25.dirty` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `SU47` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `SUK` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `SUK.Conc` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `YAK` | production-candidate | Rearm | `afld, afld.gdi, afld.allies, chairport, airfield.dockhelper` |
| `Yak41` | carrier/drone-slave | Rearm | `kirov.carrier` |

## `mods/ca/rules/china/aircraft.yaml`

| Actor | Einordnung | Bewegung / Tempo | Produktion | Waffenkanäle / Waffen | Mechaniken |
|---|---|---|---|---|---|
| `CHMIG` | production-candidate | aircraft / 215 | gated-candidate | 1 / `MaverickAP` | Rearm |
| `CHMIG.AA` | production-candidate | aircraft / 225 | gated-candidate | 1 / `WidowAA.Mig` | Rearm |
| `CHMIG.Napalm` | production-candidate | aircraft / 205 | gated-candidate | 2 / `NapalmThrow`, `NapalmThrow.Black` | Rearm |
| `CHMIG.Nuke` | production-candidate | aircraft / 205 | gated-candidate | 1 / `AtomBomb.Paradrop` | Rearm |
| `chhelix` | production-candidate | aircraft / 112 | gated-candidate | 1 / `CHHelixMG` | Cargo |
| `chhelix.Bunker` | production-candidate | aircraft / 112 | gated-candidate | 1 / `CHHelixMG` | Cargo |
| `chhelix.Gatling` | production-candidate | aircraft / 112 | gated-candidate | 8 / `ChinaMGatt.0`, `ChinaMGatt.0G`, `ChinaMGatt.1`, `ChinaMGatt.1G`, `ChinaMGatt.2`, `ChinaMGatt.2G`, `ChinaMGatt.3`, `ChinaMGatt.3G` | Cargo |
| `chhelix.MortarBunker` | production-candidate | aircraft / 112 | gated-candidate | 1 / `CHHelixMG` | Cargo |
| `chhelix.Napalm` | production-candidate | aircraft / 112 | gated-candidate | 2 / `NapalmThrow`, `NapalmThrow.Black` | Cargo |
| `chhelix.Nuke` | production-candidate | aircraft / 112 | gated-candidate | 1 / `AtomBomb.Paradrop` | Cargo |
| `chhelix.Torpedo` | production-candidate | aircraft / 112 | gated-candidate | 1 / `TorpBomb` | Cargo |
| `chhelix.husk` | wreck | aircraft / 112 | no-buildable | 0 / — | — |
| `chmig.husk` | wreck | aircraft / 223 | no-buildable | 0 / — | — |
| `helix.armorupgrade` | non-mobile/review | non-mobile / — | gated-candidate | 0 / — | — |
| `helix.reconupgrade` | non-mobile/review | non-mobile / — | gated-candidate | 0 / — | — |
| `helix.speakerupgrade` | non-mobile/review | non-mobile / — | gated-candidate | 0 / — | — |
| `xianh8` | non-production/support/review | aircraft / 180 | no-buildable | 0 / — | Cargo |
| `xianh8.carpet` | non-production/support/review | aircraft / 180 | no-buildable | 1 / `CarpetBomb` | Cargo |
| `xianh8.emp` | non-production/support/review | aircraft / 180 | no-buildable | 1 / `CHEMPBomb` | Cargo |
| `xianh8.husk` | wreck | aircraft / 200 | no-buildable | 0 / — | — |
| `xianh8.minebomb` | non-production/support/review | aircraft / 180 | no-buildable | 1 / `ClusterMineSpawner` | Cargo |
| `xianh8.nuke` | non-production/support/review | aircraft / 180 | no-buildable | 1 / `AtomBomb` | Cargo |

## `mods/ca/rules/gdi/aircraft.yaml`

| Actor | Einordnung | Bewegung / Tempo | Produktion | Waffenkanäle / Waffen | Mechaniken |
|---|---|---|---|---|---|
| `A10` | production-candidate | aircraft / 225 | gated-candidate | 1 / `NapalmTD` | Rearm |
| `A10.bomber` | non-production/support/review | aircraft / 225 | no-buildable | 1 / `NapalmTD` | Rearm |
| `ORCA` | production-candidate | aircraft / 120 | gated-candidate | 2 / `HellfireAA.Orca`, `HellfireAG.Orca` | — |
| `ORCA.AP` | production-candidate | aircraft / 120 | gated-candidate | 2 / `HellfireAA.Orca.AP`, `HellfireAG.Orca.AP` | — |
| `ORCA.Payload` | production-candidate | aircraft / 120 | gated-candidate | 2 / `HellfireAA.Orca`, `HellfireAG.Orca` | — |
| `ORCA.Sonic` | production-candidate | aircraft / 120 | gated-candidate | 1 / `SonicPulse.Orca` | — |
| `ORCA.Torp` | production-candidate | aircraft / 120 | gated-candidate | 1 / `TorpBomb` | — |
| `Orca.Drone` | carrier/drone-slave | aircraft / 221 | no-buildable | 1 / `IonStrike.Orca.init` | Rearm |
| `Orca.Warship` | production-candidate | aircraft / 49 | gated-candidate | 2 / `OrcaLauncher`, `RedEye.MKII` | CarrierMaster |
| `OrcaV2` | production-candidate | aircraft / 120 | gated-candidate | 2 / `HellfireAA.Orca`, `HellfireAG.Orca` | — |
| `OrcaV2.EMP` | production-candidate | aircraft / 120 | gated-candidate | 2 / `HellfireAA.Orca.EMP`, `HellfireAG.Orca.EMP` | — |
| `OrcaV2.Swarm` | production-candidate | aircraft / 120 | gated-candidate | 2 / `Dragon.MKII`, `RedEye.MKII` | — |
| `OrcaV2.Torp` | production-candidate | aircraft / 120 | gated-candidate | 1 / `TorpBomb` | — |
| `Orca_Bomber` | disabled-vehicle | aircraft / 150 | disabled | 1 / `OrcaBomb.EMP` | — |
| `POD` | non-production/support/review | aircraft / 300 | no-buildable | 0 / — | — |
| `POD2` | non-production/support/review | aircraft / 300 | no-buildable | 0 / — | — |
| `POD3` | non-production/support/review | aircraft / 300 | no-buildable | 0 / — | — |
| `Stealth_Fighter` | production-candidate | aircraft / 195 | gated-candidate | 1 / `APBomb` | Rearm |
| `Stealth_Fighter.payload` | production-candidate | aircraft / 195 | gated-candidate | 1 / `APBomb` | Rearm |
| `YF23.Bomber` | production-candidate | aircraft / 240 | gated-candidate | 1 / `WidowAA` | — |
| `dummy.ionstrike` | non-mobile/review | non-mobile / — | no-buildable | 1 / `IonStrike.Orca.Charge` | — |
| `orca_bomber.AP` | production-candidate | aircraft / 150 | gated-candidate | 1 / `APBomb` | — |
| `orca_bomber.EMP` | production-candidate | aircraft / 150 | gated-candidate | 1 / `OrcaBomb.EMP` | — |
| `orca_bomber.HE` | production-candidate | aircraft / 150 | gated-candidate | 1 / `Orcabomb.HE` | — |

## `mods/ca/rules/nod/aircraft.yaml`

| Actor | Einordnung | Bewegung / Tempo | Produktion | Waffenkanäle / Waffen | Mechaniken |
|---|---|---|---|---|---|
| `APCH` | production-candidate | aircraft / 150 | gated-candidate | 1 / `HeliGunAG` | — |
| `APCH.Flyby` | production-candidate | aircraft / 150 | gated-candidate | 1 / `HeliGunAG` | — |
| `APCH.Torp` | production-candidate | aircraft / 150 | gated-candidate | 1 / `TorpBomb` | — |
| `RAH66` | production-candidate | aircraft / 120 | gated-candidate | 1 / `Rah66AG` | — |
| `RAH66.Bomber` | production-candidate | aircraft / 120 | gated-candidate | 1 / `Napalm.Comanche` | — |
| `SCRN` | production-candidate | aircraft / 210 | gated-candidate | 2 / `ScrinTorp`, `ScrinTorpAA` | Rearm |
| `SCRN.Bomber` | production-candidate | aircraft / 210 | gated-candidate | 1 / `ScrinBomb` | Rearm |
| `SCRN.Payload` | production-candidate | aircraft / 190 | gated-candidate | 2 / `ScrinTorp`, `ScrinTorpAA` | Rearm |
| `VENM` | production-candidate | aircraft / 150 | gated-candidate | 2 / `VenomLaser`, `VenomLaserAA` | — |
| `VENM.HeavyLaser` | production-candidate | aircraft / 120 | gated-candidate | 2 / `Laser.Heavy`, `Laser.Heavy.AA` | — |
| `VENM.Payload` | production-candidate | aircraft / 150 | gated-candidate | 2 / `VenomLaser`, `VenomLaserAA` | — |

## `mods/ca/rules/allies/aircraft.yaml`

| Actor | Einordnung | Bewegung / Tempo | Produktion | Waffenkanäle / Waffen | Mechaniken |
|---|---|---|---|---|---|
| `AURORA` | production-candidate | aircraft / 175 | gated-candidate | 1 / `AuroraRocket` | Rearm |
| `Eurofighter` | production-candidate | aircraft / 290 | gated-candidate | 1 / `Nike.Raptor` | Rearm |
| `HARR` | production-candidate | aircraft / 210 | gated-candidate | 2 / `HellfireAA`, `HellfireAG.Harrier` | Rearm |
| `HARR.avionics` | production-candidate | aircraft / 210 | gated-candidate | 2 / `HellfireAA`, `HellfireAG.Harrier.Avionics` | Rearm |
| `HARR.payload` | production-candidate | aircraft / 210 | gated-candidate | 2 / `HellfireAA`, `HellfireAG.Harrier` | Rearm |
| `HELI` | production-candidate | aircraft / 120 | gated-candidate | 2 / `HellfireAA`, `HellfireAG` | — |
| `HELI.Artillery` | production-candidate | aircraft / 120 | gated-candidate | 1 / `HellfireAG.Arty` | — |
| `HELI.Cryo` | production-candidate | aircraft / 120 | gated-candidate | 2 / `HellfireAA.Cryo`, `HellfireAG.Cryo` | — |
| `HELI.Fire_Rate` | production-candidate | aircraft / 120 | gated-candidate | 2 / `HellfireAA`, `HellfireAG` | — |
| `HELI.Torp` | production-candidate | aircraft / 120 | gated-candidate | 1 / `TorpBomb` | — |
| `HUEY` | production-candidate | aircraft / 135 | gated-candidate | 2 / `ChainGun` | — |
| `HUEY.Grenade` | production-candidate | aircraft / 135 | gated-candidate | 1 / `Grenade.Huey` | — |
| `HUEY.cryo` | production-candidate | aircraft / 135 | gated-candidate | 1 / `CryoMissile` | — |
| `HUEY.recon` | production-candidate | aircraft / 135 | gated-candidate | 3 / `Flare`, `relayMarker`, `sniperE` | Cargo |
| `Rafale` | production-candidate | aircraft / 280 | gated-candidate | 1 / `Nike.Raptor` | Rearm |
| `Seahawk` | production-candidate | aircraft / 155 | gated-candidate | 2 / `DropDummy`, `M60mgJJ` | Cargo |
| `Seahawk.armed` | production-candidate | aircraft / 155 | gated-candidate | 3 / `DropDummy`, `HellfireAG.Bunkerbuster`, `M60mgJJ` | Cargo, Rearm |
| `Tarantula` | non-production/support/review | aircraft / 190 | no-buildable | 4 / `ChainGun.Tarantula.L`, `ChainGun.Tarantula.R`, `Rocket.Tarantula.L`, `Rocket.Tarantula.R` | — |

## `mods/ca/rules/soviet/aircraft.yaml`

| Actor | Einordnung | Bewegung / Tempo | Produktion | Waffenkanäle / Waffen | Mechaniken |
|---|---|---|---|---|---|
| `AtomParabomb` | spawned-missile | non-mobile / — | no-buildable | 0 / — | — |
| `BLIMP` | production-candidate | aircraft / 56 | gated-candidate | 0 / — | — |
| `BorisMig` | non-production/support/review | aircraft / 373 | no-buildable | 1 / `U2Bomb` | Rearm |
| `CHEMYAK` | production-candidate | aircraft / 180 | gated-candidate | 2 / `BigChemspray.Yak` | Rearm |
| `CHEMYAK.Volatile` | production-candidate | aircraft / 180 | gated-candidate | 2 / `BigChemspray.Yak` | Rearm |
| `FROG` | production-candidate | aircraft / 225 | gated-candidate | 1 / `FrogBomb` | Rearm |
| `HeavyBomber.Nuke` | production-candidate | aircraft / 180 | gated-candidate | 1 / `AtomParabombLauncher` | Rearm, MissileSpawner |
| `HeavyBomber.Tesla` | production-candidate | aircraft / 180 | gated-candidate | 1 / `TeslaGlidebombLauncher` | Rearm, MissileSpawner |
| `Hind` | production-candidate | aircraft / 120 | gated-candidate | 2 / `ChainGun` | — |
| `Hind.Missile` | production-candidate | aircraft / 120 | gated-candidate | 2 / `HellfireAA`, `HellfireAG` | — |
| `Hind.Radcannon` | production-candidate | aircraft / 120 | gated-candidate | 2 / `RadBeamWeaponE` | — |
| `Hind.Tesla` | production-candidate | aircraft / 120 | gated-candidate | 4 / `Tesla.Hind`, `Tesla.Hind.Arc` | — |
| `Hind.Torp` | production-candidate | aircraft / 120 | gated-candidate | 1 / `TorpBomb` | — |
| `Il11b` | production-candidate | aircraft / 215 | gated-candidate | 1 / `FrogBomb` | Rearm |
| `Kirov` | production-candidate | aircraft / 56 | gated-candidate | 4 / `KirovBomb`, `KirovClusterBomb`, `KirovNukeBomb`, `KirovTeslaBomb` | — |
| `Kirov.Carrier` | production-candidate | aircraft / 40 | gated-candidate | 1 / `InvaderLauncher` | CarrierMaster |
| `Kirov.Demo` | production-candidate | aircraft / 56 | gated-candidate | 1 / `DemoTruckTargeting` | — |
| `Kirov.Mecha` | production-candidate | aircraft / 45 | gated-candidate | 1 / `FireballLauncher.Kirov` | — |
| `Kirov.Mecha.Tesla` | production-candidate | aircraft / 45 | gated-candidate | 2 / `Tesla.Kirov`, `Tesla.Kirov.Arc` | — |
| `Kirov.V3` | production-candidate | aircraft / 45 | gated-candidate | 1 / `V3Launcher` | MissileSpawner |
| `MIG` | production-candidate | aircraft / 235 | gated-candidate | 1 / `MaverickAP` | Rearm |
| `SU25.dirty` | production-candidate | aircraft / 225 | gated-candidate | 1 / `RadBomb` | Rearm |
| `SU47` | production-candidate | aircraft / 250 | gated-candidate | 1 / `WidowAA.Mig` | Rearm |
| `SUK` | production-candidate | aircraft / 250 | gated-candidate | 2 / `MaverickSU`, `WidowAA.SU` | Rearm |
| `SUK.Conc` | production-candidate | aircraft / 250 | gated-candidate | 2 / `MaverickSU.Concussion`, `WidowAA.SU` | Rearm |
| `TeslaParabomb` | spawned-missile | non-mobile / — | no-buildable | 0 / — | — |
| `U2` | non-production/support/review | aircraft / 350 | no-buildable | 0 / — | — |
| `V3.Kirov` | spawned-missile | non-mobile / — | no-buildable | 0 / — | — |
| `YAK` | production-candidate | aircraft / 180 | gated-candidate | 2 / `ChainGun.Yak.L`, `ChainGun.Yak.R` | Rearm |
| `Yak41` | carrier/drone-slave | aircraft / 220 | no-buildable | 2 / `HellfireAA.Yak41`, `HellfireAG.Yak41` | Rearm |

## `mods/ca/rules/scrin/aircraft.yaml`

| Actor | Einordnung | Bewegung / Tempo | Produktion | Waffenkanäle / Waffen | Mechaniken |
|---|---|---|---|---|---|
| `Bastion` | production-candidate | aircraft / 32 | gated-candidate | 1 / `BastionZap` | — |
| `DEVA` | production-candidate | aircraft / 56 | gated-candidate | 1 / `DevastatorDiscs` | — |
| `DEVA.Rift` | production-candidate | aircraft / 56 | gated-candidate | 1 / `DevastatorRift` | — |
| `DEVA.Shards` | production-candidate | aircraft / 56 | gated-candidate | 1 / `DevastatorShards` | — |
| `INVA` | carrier/drone-slave | aircraft / 220 | no-buildable | 2 / `InvaderZap`, `InvaderZapAA` | Rearm |
| `INVA.suicide` | carrier/drone-slave | aircraft / 240 | no-buildable | 2 / `InvaderSuicide`, `InvaderZapAA` | Rearm |
| `INVA.support` | carrier/drone-slave | aircraft / 220 | no-buildable | 1 / `InvaderZap.Repair` | Rearm |
| `MSHP` | production-candidate | aircraft / 38 | gated-candidate | 2 / `MothershipBeam`, `MothershipChargeBeam` | — |
| `PAC` | production-candidate | aircraft / 49 | gated-candidate | 1 / `InvaderLauncher` | CarrierMaster |
| `PAC.laser` | production-candidate | aircraft / 40 | gated-candidate | 3 / `WarshipLaser`, `WarshipLaser.2`, `WarshipLaser.3` | — |
| `PAC.suicide` | production-candidate | aircraft / 49 | gated-candidate | 1 / `InvaderLauncher.Suicide` | CarrierMaster |
| `PAC.support` | production-candidate | aircraft / 49 | gated-candidate | 2 / `InvaderLauncher.Support`, `Repair.Dummy` | CarrierMaster |
| `STMR` | production-candidate | aircraft / 170 | gated-candidate | 2 / `StormriderZap`, `StormriderZapAA` | — |
| `STMR.bomber` | production-candidate | aircraft / 170 | gated-candidate | 1 / `Scrinbombs` | — |
| `STMR.hunter` | production-candidate | aircraft / 210 | gated-candidate | 1 / `StormriderZapAA` | — |
| `STMR.torp` | production-candidate | aircraft / 170 | gated-candidate | 1 / `DepthCharge.Scrin` | — |

## `mods/ca/rules/aircraft.yaml`

| Actor | Einordnung | Bewegung / Tempo | Produktion | Waffenkanäle / Waffen | Mechaniken |
|---|---|---|---|---|---|
| `B2B` | production-candidate | aircraft / 180 | gated-candidate | 1 / `InfernoBomb` | — |
| `BADR` | non-production/support/review | aircraft / 180 | no-buildable | 0 / — | Cargo |
| `BADR.Bomber` | non-production/support/review | aircraft / 180 | no-buildable | 1 / `ParaBomb` | — |
| `BADR.CBomber` | non-production/support/review | aircraft / 180 | no-buildable | 1 / `CarpetBomb` | — |
| `BADR.MBomber` | non-production/support/review | aircraft / 180 | no-buildable | 1 / `GeneticMutationBomb` | — |
| `BADR.NBomber` | non-production/support/review | aircraft / 180 | no-buildable | 1 / `AtomBomb` | — |
| `C17` | non-production/support/review | aircraft / 236 | no-buildable | 0 / — | Cargo |
| `C17.Cargo` | non-production/support/review | aircraft / 266 | no-buildable | 0 / — | Cargo |
| `C17.Clustermines` | non-production/support/review | aircraft / 236 | no-buildable | 1 / `ClusterMineSpawner` | Cargo |
| `C17.No_Color` | non-production/support/review | aircraft / 236 | no-buildable | 0 / — | Cargo |
| `OCAR` | disabled-vehicle | aircraft / 195 | disabled | 0 / — | Carryall, Cargo |
| `OCAR.Eagle` | production-candidate | aircraft / 144 | gated-candidate | 0 / — | AutoCarryall, Cargo |
| `OCAR.Reinforce` | non-production/support/review | aircraft / 195 | no-buildable | 0 / — | Carryall, Cargo |
| `TRAN` | production-candidate | aircraft / 135 | gated-candidate | 0 / — | Cargo |
| `TRAN.Eagle` | production-candidate | aircraft / 155 | gated-candidate | 0 / — | Cargo |
| `TRAN.paradrop` | non-production/support/review | aircraft / 135 | no-buildable | 0 / — | Cargo |
| `UAV` | non-production/support/review | aircraft / 113 | no-buildable | 0 / — | — |

## `mods/ca/rules/misc/aircraft.yaml`

| Actor | Einordnung | Bewegung / Tempo | Produktion | Waffenkanäle / Waffen | Mechaniken |
|---|---|---|---|---|---|
| `Dropship` | production-candidate | aircraft / 156 | gated-candidate | 1 / `DropDummy` | Cargo |

## `mods/ca/rules/misc/ships.yaml`

| Actor | Einordnung | Bewegung / Tempo | Produktion | Waffenkanäle / Waffen | Mechaniken |
|---|---|---|---|---|---|
| `Aegis_Cruiser` | production-candidate | naval / 40 | gated-candidate | 3 / `AdvancedPointLaser`, `Aegis_Missile`, `Aegis_Missile.AA` | PDL |
| `Aegis_Cruiser.ai` | bot-gated-vehicle | naval / 40 | bot-gated | 3 / `AdvancedPointLaser`, `Aegis_Missile`, `Aegis_Missile.AA` | PDL |
| `CA` | production-candidate | naval / 28 | gated-candidate | 3 / `8Inch`, `8Inch.NoReport`, `AdvancedPointLaser` | PDL |
| `CARR` | production-candidate | naval / 25 | gated-candidate | 1 / `HornetLauncher` | CarrierMaster |
| `DD` | production-candidate | naval / 52 | gated-candidate | 3 / `DepthCharge.destroyer`, `Stinger`, `StingerAA` | — |
| `DD2` | production-candidate | naval / 56 | gated-candidate | 1 / `Railgun.Ship` | — |
| `HORN` | carrier/drone-slave | aircraft / 240 | gated-candidate | 1 / `HellfireAG.Horn` | Rearm |
| `ICBM` | spawned-missile | non-mobile / — | no-buildable | 0 / — | — |
| `ISUB` | production-candidate | naval / 30 | gated-candidate | 1 / `ICBMLauncher` | MissileSpawner |
| `LST` | production-candidate | lcraft / 100 | gated-candidate | 0 / — | Cargo |
| `MSUB` | production-candidate | naval / 40 | gated-candidate | 1 / `SubMissile` | — |
| `MSUB.ai` | bot-gated-vehicle | naval / 40 | bot-gated | 1 / `SubMissile` | — |
| `PT2` | production-candidate | naval / 68 | gated-candidate | 3 / `AdvancedPointLaser`, `BoatMissileAA`, `boatmissile` | PDL |
| `SB` | production-candidate | naval / 100 | gated-candidate | 2 / `BikeRocketsAA`, `SBRockets` | — |
| `SEAS` | production-candidate | naval / 75 | gated-candidate | 3 / `AdvancedPointLaser`, `FLAK-SEAS-AA`, `FLAK-SEAS-AG` | PDL |
| `SS` | production-candidate | naval / 40 | gated-candidate | 1 / `TorpTube` | — |
| `SS2` | production-candidate | naval / 80 | gated-candidate | 1 / `TorpTube.Hunter` | — |
| `SS2.ai` | bot-gated-vehicle | naval / 80 | bot-gated | 1 / `TorpTube.Hunter` | — |
| `Subchaser` | production-candidate | naval / 90 | gated-candidate | 2 / `2Inch`, `DepthCharge` | — |
| `chasub` | production-candidate | naval / 50 | gated-candidate | 2 / `CHAtomicTorpTube`, `CHSubMortarShell` | — |
| `chflameboat` | production-candidate | naval / 70 | gated-candidate | 1 / `CHBoatFlamer` | — |

## Grenzen

- Strukturelle Erfassung, kein Match-/Flug-/Landungs-/Trägertest.
- Keine neue Familienpolitik für Luft/See, keine ungeprüften Modul-Kombinationen.
- Das separate [Bodeninventar](existing-vehicle-loadouts.md) enthält weiterhin seine vollständigen 388 Deklarationen.
- Alle Klassifikationen lassen sich auf die gespeicherten Traits und Quellpfade zurückführen.

