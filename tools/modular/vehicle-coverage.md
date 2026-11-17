# Vehicle families and coverage

108 provisional vehicle families with at least one local production declaration.
15 of those families have at least one scalar reference design; NOT full family coverage.
1 further groups require prototype/availability review.
388 raw actor blocks remain in the audit appendix, NOT 388 different vehicles.
Grouping policy: vehicle-family-policy.json; named specializations remain visible within families.
Production candidate does NOT mean engine-validated availability. This tool does not resolve MiniYaml inheritance.
Wrecks, projectiles and attached helpers are excluded from family counts.
Faction means source file, not resolved availability. Base credits/HP/speed are local anchor values, not all variant values.
No row is declared gameplay-equivalent. Hints are incomplete, never proof of missing/present inherited behavior.

## Main family overview

| Faction | Family | Base local credits / HP / speed | Scalar-linked actors / members | Unmodeled actors | Inspect |
|---|---|---|---|---|---|
| allies | Scout Tank (`1TNK`) | 600 / 33000 / 113 | 0 / 3 | 1TNK, 1TNK.PDL, 1TNK.Reflector | inheritance, artwork and behavior |
| allies | Medium Tank (`2TNK`) | 800 / 45000 / 82 | 4 / 5 | 2TNK.Chrono | multiple/conditional armaments |
| allies | Armored Personnel Carrier (`APC`) | 600 / 30000 / 135 | 0 / 1 | APC | cargo |
| allies | Artillery (`ARTY`) | 550 / 10000 / 56 | 1 / 2 | ARTY.Chrono | inheritance, artwork and behavior |
| allies | Missile Fortress (`BATF`) | 2000 / 100000 / 48 | 0 / 14 | BATF, BATF.Bunker, BATF.Bunker.PDL, BATF.Bunker.Reflector, BATF.Prism, BATF.Prism.PDL, BATF.Prism.Reflector, BATF.Support, BATF.Support.PDL, BATF.Support.Reflector, BATF.Artillery, BATF.Artillery.PDL, BATF.Artillery.Reflector, BATF.AI | cargo, multiple/conditional armaments |
| allies | Chrono Prison (`CHPR`) | 1700 / 75000 / 52 | 0 / 3 | CHPR, CHPR.Range, CHPR.AA | multiple/conditional armaments |
| allies | Cryo Launcher (`CRYO`) | 1150 / 32000 / 82 | 0 / 1 | CRYO | inheritance, artwork and behavior |
| allies | Chrono Tank (`CTNK`) | 1350 / 50000 / 113 | 0 / 3 | CTNK, CTNK.PDL, CTNK.Reflector | multiple/conditional armaments |
| allies | Ore Truck (`HARV`) | 1400 / 75000 / 56 | 0 / 2 | HARV, HARV.Chrono | inheritance, artwork and behavior |
| allies | Multi-Purpose Drone (`IFV`) | 750 / 30000 / 113 | 0 / 2 | IFV, IFV.AI | cargo, hover/amphibious, multiple/conditional armaments |
| allies | Ranger (`JEEP`) | 400 / 15000 / 157 | 0 / 4 | JEEP, Heavy_Jeep, Tow_Jeep, AA_Jeep | sensor |
| allies | Mobile Construction Vehicle (`MCV`) | 3000 / 75000 / 56 | 0 / 2 | MCV, MCV.Nukular | transformation/deploy |
| allies | Mobile Gap Generator (`MGG`) | 1000 / 22000 / 82 | 0 / 1 | MGG | inheritance, artwork and behavior |
| allies | Mobile Radar Jammer (`MRJ`) | 1000 / 22000 / 82 | 0 / 1 | MRJ | inheritance, artwork and behavior |
| allies | Prism Tank (`Prismtank`) | 1350 / 22000 / 82 | 0 / 3 | Prismtank, PBLASTER, PCAN | inheritance, artwork and behavior |
| allies | Mirage Tank (`RTNK`) | 800 / 43000 / 82 | 0 / 7 | RTNK, RTNK.PDL, RTNK.Reflector, RTNK.PDL.Firerate, RTNK.Reflector.Firerate, RTNK.PDL.Toughness, RTNK.Reflector.Toughness | inheritance, artwork and behavior |
| allies | Tank Destroyer (`TNKD`) | 750 / 55000 / 68 | 0 / 7 | TNKD, TNKD.PDL, TNKD.Reflector, TNKD.PDL.Burstfire, TNKD.Reflector.Burstfire, TNKD.PDL.Tough, TNKD.Reflector.Tough | inheritance, artwork and behavior |
| china | Bixi Dragon (`Bixi`) | 900 / 16000 / 56 | 0 / 1 | Bixi | inheritance, artwork and behavior |
| china | Bulldozer (China) (`CHNMCV`) | 1000 / 60000 / 56 | 0 / 2 | CHNMCV, CHNMCV.Nukular | transformation/deploy |
| china | Inferno Cannon (`charty`) | 900 / 12000 / 90 | 0 / 1 | charty | multiple/conditional armaments |
| china | Battlemaster (`chbattle`) | 950 / 40000 / 100 | 10 / 10 | - | inheritance, artwork and behavior |
| china | Troop Crawler (`chcrawl`) | 1350 / 27500 / 110 | 0 / 1 | chcrawl | cargo |
| china | Heavy Troop Crawler (`chcrawl2`) | 1000 / 45000 / 125 | 0 / 7 | chcrawl2, chcrawl2.Hunter, chcrawl2.Hunter.PDL, chcrawl2.Hunter.Reflector, chcrawl2.Assault, chcrawl2.Assault.PDL, chcrawl2.Assault.Reflector | cargo |
| china | Dragon Tank (`chdragon`) | 600 / 28000 / 103 | 3 / 3 | - | multiple/conditional armaments, transformation/deploy |
| china | ECM Tank (`checm`) | 800 / 24000 / 90 | 0 / 4 | checm, checm.pulse, checm.focus, checm.chain | inheritance, artwork and behavior |
| china | Gatling Tank (`chgtnk`) | 800 / 30000 / 108 | 3 / 3 | - | special attacks |
| china | Harvester Truck (`chharv`) | 700 / 30000 / 66 | 0 / 1 | chharv | inheritance, artwork and behavior |
| china | Nuke Cannon (`chnukecann`) | 2400 / 24000 / 55 | 0 / 3 | chnukecann, chnukecann.Range, chnukecann.Neutron | inheritance, artwork and behavior |
| china | Listening Outpost (`choutpost`) | 600 / 24000 / 85 | 0 / 7 | choutpost, choutpost.Propaganda, choutpost.Propaganda.PDL, choutpost.Propaganda.Reflector, choutpost.Bunker, choutpost.Bunker.PDL, choutpost.Bunker.Reflector | cargo, sensor, special attacks |
| china | Overlord Tank (`choverlord`) | 2000 / 95000 / 56 | 13 / 16 | choverlord.Plasma, choverlord.Plasma.PDL, choverlord.Plasma.Reflector | cargo, multiple/conditional armaments, sensor, special attacks |
| gdi | Disruptor (`DISR`) | 1500 / 75000 / 56 | 0 / 3 | DISR, DISR.PDL, DISR.Reflector | multiple/conditional armaments |
| gdi | Scout Drone (`GDRN`) | 300 / 17000 / 134 | 0 / 1 | GDRN | sensor |
| gdi | Hum-Vee (`HMMV`) | 400 / 15000 / 157 | 1 / 1 | - | sensor |
| gdi | Juggernaut (`Juggernaut`) | 2000 / 60000 / 50 | 2 / 3 | Juggernaut.Firerate | multiple/conditional armaments, walker |
| gdi | Mammoth Mk. II (`MAMMOTHMK2`) | 10000 / 350000 / 35 | 0 / 1 | MAMMOTHMK2 | multiple/conditional armaments, walker |
| gdi | M.A.R.V. (`MARV`) | 10000 / 200000 / 40 | 0 / 1 | MARV | inheritance, artwork and behavior |
| gdi | Mini Drone (`MDRN`) | 300 / 15000 / 80 | 0 / 1 | MDRN | hover/amphibious |
| gdi | Mobile E.M.P (`MEMP`) | 1050 / 75000 / 135 | 0 / 4 | MEMP, MEMP.Volatile, MEMP.Ranged, MEMP.Ranged.Improved | inheritance, artwork and behavior |
| gdi | MLRS (`MLRS`) | 950 / 16000 / 82 | 0 / 3 | MLRS, MLRS.AA, MLRS.Hailstorm | multiple/conditional armaments |
| gdi | Mobile Sensor Array (`MSAR`) | 1000 / 22000 / 82 | 0 / 2 | MSAR, MSAR.PDL | sensor |
| gdi | Battle Tank (`MTNK`) | 900 / 52000 / 82 | 1 / 3 | MTNK.PDL, MTNK.Reflector | inheritance, artwork and behavior |
| gdi | Mammoth Tank (`Mammoth`) | 1700 / 78000 / 52 | 0 / 10 | Mammoth, Mammoth.Ion, Mammoth.Ion.Reflector, Mammoth.Ion.PDL, Mammoth.Nanite, Mammoth.Nanite.Reflector, Mammoth.Nanite.PDL, Mammoth.Hover, Mammoth.Hover.Reflector, Mammoth.Hover.PDL | hover/amphibious, multiple/conditional armaments |
| gdi | Slingshot (`SLNG`) | 550 / 13500 / 133 | 0 / 1 | SLNG | inheritance, artwork and behavior |
| gdi | Titan (`TITN`) | 2000 / 100000 / 50 | 0 / 7 | TITN, TITN.Battle, TITN.Battle.PDL, TITN.Battle.Reflector, TITN.Railgun, TITN.Railgun.PDL, TITN.Railgun.Reflector | multiple/conditional armaments, walker |
| gdi | Vulcan (`VULC`) | 800 / 60000 / 100 | 0 / 1 | VULC | cargo, multiple/conditional armaments |
| gdi | X-O Powersuit (`XO`) | 800 / 22000 / 95 | 0 / 1 | XO | multiple/conditional armaments, sensor, walker |
| gdi | Hover MLRS (`hmlrs`) | 1150 / 18000 / 113 | 0 / 3 | hmlrs, hmlrs.Reflector, hmlrs.PDL | hover/amphibious, multiple/conditional armaments |
| nod | Mobile Construction Vehicle (`AMCV`) | 3000 / 75000 / 56 | 0 / 2 | AMCV, AMCV.Nukular | transformation/deploy |
| nod | Armored Personnel Carrier (`APC2`) | 600 / 30000 / 135 | 0 / 2 | APC2, APC2.Reinforce | cargo |
| nod | ARTY.nod (`ARTY.nod`) | ? / ? / ? | 0 / 1 | ARTY.nod | inheritance, artwork and behavior |
| nod | Buggy (`BGGY`) | 350 / 14000 / 157 | 1 / 3 | BGGY.PDL, BGGY.AA | sensor, special attacks |
| nod | Recon Bike (`BIKE`) | 500 / 11000 / 180 | 0 / 10 | BIKE, BIKE.Scrin, BIKE.Scrin.PDL, BIKE.Scrin.Reflector, BIKE.RocketHail, BIKE.RocketHail.PDL, BIKE.RocketHail.Reflector, BIKE.Explosive, BIKE.Explosive.PDL, BIKE.Explosive.Reflector | multiple/conditional armaments |
| nod | Beam Cannon (`Beam_Cannon`) | 1250 / 24000 / 100 | 0 / 1 | Beam_Cannon | multiple/conditional armaments, special attacks |
| nod | Reinforcements Coordinator (`COORDINATOR`) | 1000 / 22000 / 82 | 0 / 1 | COORDINATOR | inheritance, artwork and behavior |
| nod | Devil's Tongue (`FTNK`) | 700 / 40000 / 82 | 0 / 5 | FTNK, FTNK.Reflector, FTNK.PDL, FTNK.PDL.Chem, FTNK.Reflector.Chem | inheritance, artwork and behavior |
| nod | Harvester (`HAR2`) | 1400 / 75000 / 56 | 0 / 1 | HAR2 | inheritance, artwork and behavior |
| nod | Heavy Flame Tank (`HFTK`) | 1000 / 75000 / 68 | 0 / 5 | HFTK, HFTK.PDL, HFTK.Reflector, HFTK.PDL.Fireball, HFTK.Reflector.Fireball | multiple/conditional armaments |
| nod | Howitzer (`HOWI`) | ? / 15000 / 68 | 0 / 1 | HOWI | inheritance, artwork and behavior |
| nod | Light Tank (`LTNK`) | 625 / 41250 / 100 | 1 / 2 | LTNK.Laser | hover/amphibious, special attacks |
| nod | Mobile Stealth Generator (`MSG`) | 1250 / 25000 / 56 | 0 / 1 | MSG | transformation/deploy |
| nod | Specter (`SPEC`) | 1100 / 11000 / 100 | 0 / 1 | SPEC | transformation/deploy |
| nod | SSM Napalm Launcher (`SSM`) | 1050 / 15000 / 82 | 0 / 4 | SSM, SSM.Toxin, SSM.Multi, SSM.Bunkerbuster | inheritance, artwork and behavior |
| nod | Stealth Tank (`STNK`) | 1200 / 20000 / 135 | 0 / 10 | STNK, STNK.Scrin, STNK.Scrin.PDL, STNK.Scrin.Reflector, STNK.HE, STNK.HE.PDL, STNK.HE.Reflector, STNK.AP, STNK.AP.PDL, STNK.AP.Reflector | multiple/conditional armaments |
| nod | Chemical Truck (`TTRK`) | 1200 / 10000 / 92 | 0 / 1 | TTRK | inheritance, artwork and behavior |
| nod | Microwave Tank (`WTNK`) | 1250 / 35000 / 100 | 0 / 1 | WTNK | multiple/conditional armaments |
| scrin | Atomizer (`ATMZ`) | 1250 / 22000 / 75 | 0 / 3 | ATMZ, ATMZ.AA, ATMZ.Range | hover/amphibious |
| scrin | Corrupter (`CORR`) | 700 / 45000 / 82 | 0 / 7 | CORR, CORR.Area, CORR.Area.PDL, CORR.Area.Reflector, CORR.Range, CORR.Range.PDL, CORR.Range.Reflector | multiple/conditional armaments |
| scrin | Channeler Tank (`Channeler`) | 1350 / 45000 / 61 | 0 / 7 | Channeler, Channeler.chain, Channeler.disc, Channeler.disc.reflector, Channeler.disc.pdl, Channeler.chain.reflector, Channeler.chain.pdl | hover/amphibious |
| scrin | Devourer Tank (`DEVO`) | 1250 / 35000 / 90 | 0 / 3 | DEVO, DEVO.acid, DEVO.heavy | multiple/conditional armaments |
| scrin | Gun Walker (`GUNW`) | 650 / 30000 / 113 | 0 / 2 | GUNW, GUNW.sensor | multiple/conditional armaments, sensor |
| scrin | Harvester (`HARV.Scrin`) | 1400 / 75000 / 56 | 0 / 1 | HARV.Scrin | inheritance, artwork and behavior |
| scrin | Hexapod Eradicator (`Hexapod`) | 5000 / 150000 / 42 | 0 / 1 | Hexapod | multiple/conditional armaments |
| scrin | Interloper (`INTL`) | 700 / 40000 / 135 | 0 / 3 | INTL, INTL.AA, INTL.Teleport | cargo |
| scrin | Lacerator (`LACE`) | 500 / 25000 / 157 | 0 / 4 | LACE, LACE.PDL, LACE.Reflector, LACE.AP | inheritance, artwork and behavior |
| scrin | Leecher (`LCHR`) | 900 / 44000 / 80 | 0 / 7 | LCHR, LCHR.Slow, LCHR.Slow.PDL, LCHR.Slow.Reflector, LCHR.Drain, LCHR.Drain.PDL, LCHR.Drain.Reflector | inheritance, artwork and behavior |
| scrin | Heavy Tripod (`RPTP`) | 2200 / 98000 / ? | 0 / 7 | RPTP, RPTP.range, RPTP.range.reflector, RPTP.range.PDL, RPTP.acid, RPTP.acid.reflector, RPTP.acid.PDL | multiple/conditional armaments |
| scrin | Ruiner (`RUIN`) | 800 / 15000 / 80 | 0 / 3 | RUIN, RUIN.barrage, RUIN.splash | inheritance, artwork and behavior |
| scrin | Seeker (`SEEK`) | 800 / 20000 / 135 | 0 / 3 | SEEK, SEEK.PDL, SEEK.Reflector | inheritance, artwork and behavior |
| scrin | Colony Ship (`SMCV`) | 3000 / 75000 / 56 | 0 / 2 | SMCV, SMCV.Nukular | hover/amphibious, transformation/deploy |
| scrin | Stormcrawler (`STCR`) | 1000 / 60000 / 71 | 0 / 7 | STCR, STCR.Range, STCR.Range.PDL, STCR.Range.Reflector, STCR.Arc, STCR.Arc.PDL, STCR.Arc.Reflector | hover/amphibious |
| scrin | Annihilator Tripod (`TPOD`) | 1200 / 45000 / 66 | 0 / 7 | TPOD, TPOD.chain, TPOD.chain.reflector, TPOD.chain.PDL, TPOD.acid, TPOD.acid.reflector, TPOD.acid.pdl | multiple/conditional armaments |
| soviet | Akatsiya (`2S3`) | 1350 / 30000 / 68 | 0 / 1 | 2S3 | multiple/conditional armaments |
| soviet | Armored Personnel Carrier (BTR) (`BTR`) | 850 / 33000 / 100 | 0 / 2 | BTR, BTR.Surveillance | cargo, multiple/conditional armaments, sensor |
| soviet | Chemical Sprayer (`Chem_Sprayer`) | 600 / 32000 / 95 | 0 / 10 | Chem_Sprayer, Chem_Sprayer.Range, Chem_Sprayer.Range.Metal_Acid, Chem_Sprayer.Range.Spread, Chem_Sprayer.Splash, Chem_Sprayer.Splash.Metal_Acid, Chem_Sprayer.Splash.Spread, Chem_Sprayer.Cloud, Chem_Sprayer.Cloud.Metal_Acid, Chem_Sprayer.Cloud.Spread | inheritance, artwork and behavior |
| soviet | Demolition Truck (`DTRK`) | 2500 / 5000 / 82 | 0 / 1 | DTRK | transformation/deploy |
| soviet | Devil Tank (`Devil_Tank`) | 700 / 45000 / 90 | 1 / 3 | Devil_Tank.PDL, Devil_Tank.Reflector | inheritance, artwork and behavior |
| soviet | Mobile Flak (`FTRK`) | 500 / 15000 / 118 | 0 / 2 | FTRK, FTRK.Barrage | multiple/conditional armaments |
| soviet | Gene Splicer (`Gene_Splicer`) | 5000 / 150000 / 50 | 0 / 1 | Gene_Splicer | multiple/conditional armaments |
| soviet | HQ7 Missile Launcher (`HQ7`) | 1000 / 20000 / 80 | 0 / 1 | HQ7 | multiple/conditional armaments |
| soviet | HTK5 Missile Launcher (`HTK5`) | 1000 / 20000 / 80 | 0 / 1 | HTK5 | multiple/conditional armaments |
| soviet | Heavy Tank (`Heavy_Tank`) | 1100 / 65000 / 68 | 1 / 7 | Heavy_Tank.AP, Heavy_Tank.AP.PDL, Heavy_Tank.AP.Reflector, Heavy_Tank.Tesla, Heavy_Tank.Tesla.PDL, Heavy_Tank.Tesla.Reflector | special attacks |
| soviet | Hyena Missile Launcher (`Hyena`) | 1000 / 14000 / 125 | 0 / 1 | Hyena | multiple/conditional armaments |
| soviet | ISU-152 (`ISU`) | 1600 / 65000 / 56 | 0 / 10 | ISU, ISU.Concussion, ISU.Concussion.Reflector, ISU.Concussion.PDL, ISU.Cluster, ISU.Cluster.Reflector, ISU.Cluster.PDL, ISU.AP, ISU.AP.Reflector, ISU.AP.PDL | multiple/conditional armaments |
| soviet | Katyusha (`KATY`) | 1200 / 15000 / 68 | 0 / 1 | KATY | multiple/conditional armaments |
| soviet | MCV.Soviet (`MCV.Soviet`) | ? / ? / ? | 0 / 2 | MCV.Soviet, MCV.Nukular.Soviet | transformation/deploy |
| soviet | Nona SVK (`NonaSVK`) | 700 / 15000 / 110 | 0 / 1 | NonaSVK | inheritance, artwork and behavior |
| soviet | The People's Tank (`Peoples_Tank`) | 1000 / 60000 / 75 | 0 / 7 | Peoples_Tank, Peoples_Tank.Speaker, Peoples_Tank.Speaker.Reflector, Peoples_Tank.Speaker.PDL, Peoples_Tank.Mass_Production, Peoples_Tank.Mass_Production.Reflector, Peoples_Tank.Mass_Production.PDL | multiple/conditional armaments, special attacks |
| soviet | MAD Tank (`QTNK`) | 1500 / 90000 / 56 | 0 / 1 | QTNK | transformation/deploy |
| soviet | Kim's Giant Rice Cooker (`Rice_Cooker`) | 3000 / 35000 / 40 | 0 / 1 | Rice_Cooker | cargo, multiple/conditional armaments |
| soviet | Source of Pollution (`Source_of_Pollution`) | 1600 / 88000 / 52 | 0 / 10 | Source_of_Pollution, Source_of_Pollution.Metal_Acid, Source_of_Pollution.Metal_Acid.Reflector, Source_of_Pollution.Metal_Acid.PDL, Source_of_Pollution.Chem_Spray, Source_of_Pollution.Chem_Spray.Reflector, Source_of_Pollution.Chem_Spray.PDL, Source_of_Pollution.Chem_Bomb, Source_of_Pollution.Chem_Bomb.Reflector, Source_of_Pollution.Chem_Bomb.PDL | inheritance, artwork and behavior |
| soviet | Soviet Miner (`Soviet_Miner`) | 1400 / 75000 / 56 | 0 / 1 | Soviet_Miner | special attacks |
| soviet | T-34 Tank (`T-34`) | 600 / 42000 / 100 | 1 / 1 | - | multiple/conditional armaments |
| soviet | Tesla Tank (`TTNK`) | 1150 / 38000 / 113 | 0 / 2 | TTNK, TTNK.Arc | special attacks |
| soviet | Heavy Tesla Tank (`TTNK.RA2`) | 1350 / 48000 / 100 | 1 / 2 | TTNK.RA2.Arc | special attacks |
| soviet | Tsar Tank (`Tsar_Tank`) | 8000 / 150000 / 45 | 0 / 1 | Tsar_Tank | multiple/conditional armaments, special attacks, transformation/deploy |
| soviet | V3 Launcher (`V3RL`) | 1200 / 16000 / 56 | 0 / 3 | V3RL, V3BRL, V4RL | inheritance, artwork and behavior |
| soviet | Apocalypse Tank (`apoc`) | 1700 / 78000 / 52 | 0 / 10 | apoc, apoc.Speaker, apoc.Speaker.Reflector, apoc.Speaker.PDL, apoc.Nuke, apoc.Nuke.Reflector, apoc.Nuke.PDL, apoc.Drozd, apoc.Drozd.Reflector, apoc.Drozd.PDL | multiple/conditional armaments, special attacks |
| soviet | Kim's Monowheel (`kims_wheel`) | 300 / 12000 / 157 | 0 / 1 | kims_wheel | sensor |

## Prototypes / availability review

- soviet / SFTNK: SFTNK

## Separated raw actor categories

- base: 106
- variant: 252
- non-production form: 8
  - china / CHNMCV.Nukular
  - nod / AMCV.Nukular
  - nod / APC2.Reinforce
  - allies / MCV.Nukular
  - allies / 2TNK
  - allies / ARTY.Chrono
  - soviet / MCV.Nukular.Soviet
  - scrin / SMCV.Nukular
- prototype: 4
  - allies / BATF
  - soviet / SFTNK
  - soviet / V3BRL
  - soviet / V4RL
- wreck: 7
  - china / charty.Husk
  - china / choverlord.Husk
  - china / chdragon.Husk
  - china / chgtnk.Husk
  - china / chharv.Husk
  - china / chcrawl.Husk
  - china / chnukecann.Husk
- projectile: 9
  - china / chnukecannon.shell
  - china / chnukecannon.shell.range
  - china / chnukecannon.shell.neutron
  - china / Bixi.Missile
  - soviet / V3
  - soviet / V3B
  - soviet / V4
  - soviet / Rice_Shell
  - soviet / Rice_PassengerShell
- attachment: 1
  - gdi / MDRN.Attached
- review: 1
  - china / CHTRUK

## Raw audit appendix

Values are LOCAL overrides only: blank/? means inherited/unresolved, never zero.

| Faction | Actor | Category / family | Local credits / HP / speed | Local locomotor | Status | Inspect |
|---|---|---|---|---|---|---|
| china | CHNMCV | base / CHNMCV | 1000 / 60000 / 56 | heavywheeled | not modeled; inspection required | transformation/deploy |
| china | CHNMCV.Nukular | non-production form / CHNMCV | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | chbattle | base / chbattle | 950 / 40000 / 100 | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | chbattle.Autoloader | variant / chbattle | ? / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | chbattle.Autoloader.PDL | variant / chbattle | 1600 / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | chbattle.Autoloader.Reflector | variant / chbattle | ? / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | chbattle.Nuclear_Shells | variant / chbattle | ? / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | chbattle.Nuclear_Shells.PDL | variant / chbattle | 1600 / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | chbattle.Nuclear_Shells.Reflector | variant / chbattle | ? / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | chbattle.Mass_Production | variant / chbattle | 600 / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | chbattle.Mass_Production.PDL | variant / chbattle | 1000 / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | chbattle.Mass_Production.Reflector | variant / chbattle | ? / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | charty | base / charty | 900 / 12000 / 90 | ? | not modeled; inspection required | multiple/conditional armaments |
| china | charty.Husk | wreck / - | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | choverlord | base / choverlord | 2000 / 95000 / 56 | sheavytracked | scalar design; runtime pending | inheritance, weapons, artwork |
| china | choverlord.Gatling | variant / choverlord | 2000 / ? / ? | ? | scalar design; runtime pending | sensor, special attacks |
| china | choverlord.Gatling.PDL | variant / choverlord | 2800 / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | choverlord.Gatling.Reflector | variant / choverlord | ? / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | choverlord.Bunker | variant / choverlord | 3000 / ? / ? | ? | scalar design; runtime pending | cargo |
| china | choverlord.Bunker.PDL | variant / choverlord | 3600 / ? / ? | ? | scalar design; runtime pending | cargo |
| china | choverlord.Bunker.Reflector | variant / choverlord | ? / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | choverlord.Propaganda | variant / choverlord | 2200 / ? / ? | ? | scalar design; runtime pending | special attacks |
| china | choverlord.Propaganda.PDL | variant / choverlord | 2800 / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | choverlord.Propaganda.Reflector | variant / choverlord | ? / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | choverlord.Nuke_Shells | variant / choverlord | 2500 / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | choverlord.Nuke_Shells.PDL | variant / choverlord | 3000 / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | choverlord.Nuke_Shells.Reflector | variant / choverlord | ? / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | choverlord.Plasma | variant / choverlord | ? / ? / ? | ? | not modeled; inspection required | multiple/conditional armaments |
| china | choverlord.Plasma.PDL | variant / choverlord | 2500 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | choverlord.Plasma.Reflector | variant / choverlord | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | choverlord.Husk | wreck / - | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | chdragon | base / chdragon | 600 / 28000 / 103 | ? | scalar design; runtime pending | transformation/deploy, multiple/conditional armaments |
| china | chdragon.PDL | variant / chdragon | 1200 / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | chdragon.Reflector | variant / chdragon | ? / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | chdragon.Husk | wreck / - | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | chgtnk | base / chgtnk | 800 / 30000 / 108 | ? | scalar design; runtime pending | special attacks |
| china | chgtnk.PDL | variant / chgtnk | 1400 / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | chgtnk.Reflector | variant / chgtnk | ? / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| china | chgtnk.Husk | wreck / - | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | chharv | base / chharv | 700 / 30000 / 66 | wheeled | not modeled; inspection required | inheritance, weapons, artwork |
| china | chharv.Husk | wreck / - | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | choutpost | base / choutpost | 600 / 24000 / 85 | ? | not modeled; inspection required | cargo, sensor |
| china | choutpost.Propaganda | variant / choutpost | 800 / 35000 / ? | ? | not modeled; inspection required | special attacks |
| china | choutpost.Propaganda.PDL | variant / choutpost | 1300 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | choutpost.Propaganda.Reflector | variant / choutpost | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | choutpost.Bunker | variant / choutpost | 1500 / 35000 / ? | ? | not modeled; inspection required | cargo |
| china | choutpost.Bunker.PDL | variant / choutpost | 1600 / ? / ? | ? | not modeled; inspection required | cargo |
| china | choutpost.Bunker.Reflector | variant / choutpost | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | checm | base / checm | 800 / 24000 / 90 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | checm.pulse | variant / checm | 1000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | checm.focus | variant / checm | 1000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | checm.chain | variant / checm | 1000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | CHTRUK | review / - | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | chcrawl | base / chcrawl | 1350 / 27500 / 110 | ? | not modeled; inspection required | cargo |
| china | chcrawl2 | base / chcrawl2 | 1000 / 45000 / 125 | ? | not modeled; inspection required | cargo |
| china | chcrawl2.Hunter | variant / chcrawl2 | 2500 / ? / ? | ? | not modeled; inspection required | cargo |
| china | chcrawl2.Hunter.PDL | variant / chcrawl2 | 3200 / ? / ? | ? | not modeled; inspection required | cargo |
| china | chcrawl2.Hunter.Reflector | variant / chcrawl2 | 2500 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | chcrawl2.Assault | variant / chcrawl2 | 2000 / ? / ? | ? | not modeled; inspection required | cargo |
| china | chcrawl2.Assault.PDL | variant / chcrawl2 | 2600 / ? / ? | ? | not modeled; inspection required | cargo |
| china | chcrawl2.Assault.Reflector | variant / chcrawl2 | 2000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | chcrawl.Husk | wreck / - | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | chnukecann | base / chnukecann | 2400 / 24000 / 55 | tracked | not modeled; inspection required | inheritance, weapons, artwork |
| china | chnukecann.Range | variant / chnukecann | 2700 / 30000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | chnukecann.Neutron | variant / chnukecann | 2700 / 30000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | chnukecannon.shell | projectile / - | 50 / 15000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | chnukecannon.shell.range | projectile / - | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | chnukecannon.shell.neutron | projectile / - | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | chnukecann.Husk | wreck / - | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | Bixi | base / Bixi | 900 / 16000 / 56 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| china | Bixi.Missile | projectile / - | ? / 8000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | HMMV | base / HMMV | 400 / 15000 / 157 | ? | scalar design; runtime pending | sensor |
| gdi | MTNK | base / MTNK | 900 / 52000 / 82 | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| gdi | MTNK.PDL | variant / MTNK | 1500 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | MTNK.Reflector | variant / MTNK | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | Mammoth | base / Mammoth | 1700 / 78000 / 52 | heavytracked | not modeled; inspection required | multiple/conditional armaments |
| gdi | Mammoth.Ion | variant / Mammoth | 2000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | Mammoth.Ion.Reflector | variant / Mammoth | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | Mammoth.Ion.PDL | variant / Mammoth | 2500 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | Mammoth.Nanite | variant / Mammoth | 2500 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | Mammoth.Nanite.Reflector | variant / Mammoth | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | Mammoth.Nanite.PDL | variant / Mammoth | 3000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | Mammoth.Hover | variant / Mammoth | 1600 / ? / 92 | ? | not modeled; inspection required | hover/amphibious |
| gdi | Mammoth.Hover.Reflector | variant / Mammoth | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | Mammoth.Hover.PDL | variant / Mammoth | 2200 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | MEMP | base / MEMP | 1050 / 75000 / 135 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | MEMP.Volatile | variant / MEMP | 1150 / ? / 115 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | MEMP.Ranged | variant / MEMP | 1250 / 75000 / 135 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | MEMP.Ranged.Improved | variant / MEMP | 1500 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | MSAR | base / MSAR | 1000 / 22000 / 82 | ? | not modeled; inspection required | sensor |
| gdi | MSAR.PDL | variant / MSAR | 1500 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | MLRS | base / MLRS | 950 / 16000 / 82 | ? | not modeled; inspection required | multiple/conditional armaments |
| gdi | MLRS.AA | variant / MLRS | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | hmlrs | base / hmlrs | 1150 / 18000 / 113 | ? | not modeled; inspection required | hover/amphibious, multiple/conditional armaments |
| gdi | hmlrs.Reflector | variant / hmlrs | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | hmlrs.PDL | variant / hmlrs | 1750 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | MLRS.Hailstorm | variant / MLRS | 1200 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | TITN | base / TITN | 2000 / 100000 / 50 | sheavytracked | not modeled; inspection required | walker, multiple/conditional armaments |
| gdi | TITN.Battle | variant / TITN | 2200 / 110000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | TITN.Battle.PDL | variant / TITN | 2800 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | TITN.Battle.Reflector | variant / TITN | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | TITN.Railgun | variant / TITN | 3000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | TITN.Railgun.PDL | variant / TITN | 3500 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | TITN.Railgun.Reflector | variant / TITN | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | Juggernaut | base / Juggernaut | 2000 / 60000 / 50 | sheavytracked | scalar design; runtime pending | walker, multiple/conditional armaments |
| gdi | Juggernaut.Emp | variant / Juggernaut | ? / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| gdi | Juggernaut.Firerate | variant / Juggernaut | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | DISR | base / DISR | 1500 / 75000 / 56 | ? | not modeled; inspection required | multiple/conditional armaments |
| gdi | DISR.PDL | variant / DISR | 2100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | DISR.Reflector | variant / DISR | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | MARV | base / MARV | 10000 / 200000 / 40 | sheavytracked | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | MAMMOTHMK2 | base / MAMMOTHMK2 | 10000 / 350000 / 35 | heavytracked | not modeled; inspection required | walker, multiple/conditional armaments |
| gdi | SLNG | base / SLNG | 550 / 13500 / 133 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| gdi | VULC | base / VULC | 800 / 60000 / 100 | ? | not modeled; inspection required | cargo, multiple/conditional armaments |
| gdi | XO | base / XO | 800 / 22000 / 95 | ? | not modeled; inspection required | walker, sensor, multiple/conditional armaments |
| gdi | MDRN | base / MDRN | 300 / 15000 / 80 | seal | not modeled; inspection required | hover/amphibious |
| gdi | MDRN.Attached | attachment / - | ? / ? / ? | ? | not modeled; inspection required | hover/amphibious |
| gdi | GDRN | base / GDRN | 300 / 17000 / 134 | ? | not modeled; inspection required | sensor |
| nod | AMCV | base / AMCV | 3000 / 75000 / 56 | heavywheeled | not modeled; inspection required | transformation/deploy |
| nod | AMCV.Nukular | non-production form / AMCV | ? / ? / ? | ? | not modeled; inspection required | transformation/deploy |
| nod | BGGY | base / BGGY | 350 / 14000 / 157 | ? | scalar design; runtime pending | sensor |
| nod | BGGY.PDL | variant / BGGY | 900 / 18000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | BGGY.AA | variant / BGGY | 500 / 21000 / ? | ? | not modeled; inspection required | special attacks |
| nod | APC2 | base / APC2 | 600 / 30000 / 135 | ? | not modeled; inspection required | cargo |
| nod | APC2.Reinforce | non-production form / APC2 | ? / ? / ? | ? | not modeled; inspection required | cargo |
| nod | ARTY.nod | base / ARTY.nod | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | HOWI | base / HOWI | ? / 15000 / 68 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | SPEC | base / SPEC | 1100 / 11000 / 100 | ? | not modeled; inspection required | transformation/deploy |
| nod | LTNK | base / LTNK | 625 / 41250 / 100 | ? | scalar design; runtime pending | special attacks |
| nod | LTNK.Laser | variant / LTNK | 750 / 41250 / 100 | ? | not modeled; inspection required | hover/amphibious |
| nod | SSM | base / SSM | 1050 / 15000 / 82 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | SSM.Toxin | variant / SSM | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | SSM.Multi | variant / SSM | 1200 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | SSM.Bunkerbuster | variant / SSM | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | STNK | base / STNK | 1200 / 20000 / 135 | ? | not modeled; inspection required | multiple/conditional armaments |
| nod | STNK.Scrin | variant / STNK | 1200 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | STNK.Scrin.PDL | variant / STNK | 1800 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | STNK.Scrin.Reflector | variant / STNK | 1200 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | STNK.HE | variant / STNK | 1200 / ? / ? | ? | not modeled; inspection required | multiple/conditional armaments |
| nod | STNK.HE.PDL | variant / STNK | 1800 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | STNK.HE.Reflector | variant / STNK | 1200 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | STNK.AP | variant / STNK | 1400 / ? / ? | ? | not modeled; inspection required | multiple/conditional armaments |
| nod | STNK.AP.PDL | variant / STNK | 2000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | STNK.AP.Reflector | variant / STNK | 1400 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | BIKE | base / BIKE | 500 / 11000 / 180 | ? | not modeled; inspection required | multiple/conditional armaments |
| nod | BIKE.Scrin | variant / BIKE | 900 / 18000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | BIKE.Scrin.PDL | variant / BIKE | 1400 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | BIKE.Scrin.Reflector | variant / BIKE | 900 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | BIKE.RocketHail | variant / BIKE | 900 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | BIKE.RocketHail.PDL | variant / BIKE | 1400 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | BIKE.RocketHail.Reflector | variant / BIKE | 900 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | BIKE.Explosive | variant / BIKE | 500 / ? / ? | ? | not modeled; inspection required | multiple/conditional armaments |
| nod | BIKE.Explosive.PDL | variant / BIKE | 900 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | BIKE.Explosive.Reflector | variant / BIKE | 600 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | HAR2 | base / HAR2 | 1400 / 75000 / 56 | heavywheeled | not modeled; inspection required | inheritance, weapons, artwork |
| nod | WTNK | base / WTNK | 1250 / 35000 / 100 | ? | not modeled; inspection required | multiple/conditional armaments |
| nod | FTNK | base / FTNK | 700 / 40000 / 82 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | FTNK.Reflector | variant / FTNK | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | FTNK.PDL | variant / FTNK | 1100 / 50000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | FTNK.PDL.Chem | variant / FTNK | 1300 / 60000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | FTNK.Reflector.Chem | variant / FTNK | 900 / 60000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | HFTK | base / HFTK | 1000 / 75000 / 68 | heavytracked | not modeled; inspection required | multiple/conditional armaments |
| nod | HFTK.PDL | variant / HFTK | 1400 / 50000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | HFTK.Reflector | variant / HFTK | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | HFTK.PDL.Fireball | variant / HFTK | 1400 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | HFTK.Reflector.Fireball | variant / HFTK | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | TTRK | base / TTRK | 1200 / 10000 / 92 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | COORDINATOR | base / COORDINATOR | 1000 / 22000 / 82 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| nod | Beam_Cannon | base / Beam_Cannon | 1250 / 24000 / 100 | ? | not modeled; inspection required | special attacks, multiple/conditional armaments |
| nod | MSG | base / MSG | 1250 / 25000 / 56 | ? | not modeled; inspection required | transformation/deploy |
| allies | HARV | base / HARV | 1400 / 75000 / 56 | heavywheeled | not modeled; inspection required | inheritance, weapons, artwork |
| allies | HARV.Chrono | variant / HARV | 1400 / ? / 56 | heavywheeled | not modeled; inspection required | inheritance, weapons, artwork |
| allies | MCV | base / MCV | 3000 / 75000 / 56 | heavywheeled | not modeled; inspection required | transformation/deploy |
| allies | MCV.Nukular | non-production form / MCV | ? / ? / ? | ? | not modeled; inspection required | transformation/deploy |
| allies | MGG | base / MGG | 1000 / 22000 / 82 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | MRJ | base / MRJ | 1000 / 22000 / 82 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | APC | base / APC | 600 / 30000 / 135 | ? | not modeled; inspection required | cargo |
| allies | JEEP | base / JEEP | 400 / 15000 / 157 | ? | not modeled; inspection required | sensor |
| allies | Heavy_Jeep | variant / JEEP | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | Tow_Jeep | variant / JEEP | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | AA_Jeep | variant / JEEP | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | 1TNK | base / 1TNK | 600 / 33000 / 113 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | 1TNK.PDL | variant / 1TNK | 1200 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | 1TNK.Reflector | variant / 1TNK | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | 2TNK | non-production form / 2TNK | 800 / 45000 / 82 | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| allies | Challenger_Tank | variant / 2TNK | ? / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| allies | Leclerc_Tank | variant / 2TNK | ? / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| allies | Leopard_Tank | variant / 2TNK | ? / ? / ? | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| allies | 2TNK.Chrono | variant / 2TNK | ? / ? / ? | ? | not modeled; inspection required | multiple/conditional armaments |
| allies | ARTY | base / ARTY | 550 / 10000 / 56 | lighttracked | scalar design; runtime pending | inheritance, weapons, artwork |
| allies | ARTY.Chrono | non-production form / ARTY | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | Prismtank | base / Prismtank | 1350 / 22000 / 82 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | PBLASTER | variant / Prismtank | ? / 33000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | PCAN | variant / Prismtank | 1350 / 22000 / 82 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | CRYO | base / CRYO | 1150 / 32000 / 82 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | CTNK | base / CTNK | 1350 / 50000 / 113 | heavywheeled | not modeled; inspection required | multiple/conditional armaments |
| allies | CTNK.PDL | variant / CTNK | 2000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | CTNK.Reflector | variant / CTNK | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | CHPR | base / CHPR | 1700 / 75000 / 52 | heavytracked | not modeled; inspection required | multiple/conditional armaments |
| allies | CHPR.Range | variant / CHPR | 2250 / ? / ? | ? | not modeled; inspection required | multiple/conditional armaments |
| allies | CHPR.AA | variant / CHPR | 2250 / ? / ? | ? | not modeled; inspection required | multiple/conditional armaments |
| allies | BATF | prototype / BATF | 2000 / 100000 / 48 | sheavytracked | not modeled; inspection required | multiple/conditional armaments |
| allies | BATF.Bunker | variant / BATF | 3000 / 100000 / 48 | sheavytracked | not modeled; inspection required | cargo, multiple/conditional armaments |
| allies | BATF.Bunker.PDL | variant / BATF | 3600 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | BATF.Bunker.Reflector | variant / BATF | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | BATF.Prism | variant / BATF | 2000 / 100000 / 48 | sheavytracked | not modeled; inspection required | inheritance, weapons, artwork |
| allies | BATF.Prism.PDL | variant / BATF | 3000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | BATF.Prism.Reflector | variant / BATF | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | BATF.Support | variant / BATF | 2000 / 125000 / ? | ? | not modeled; inspection required | cargo |
| allies | BATF.Support.PDL | variant / BATF | 2000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | BATF.Support.Reflector | variant / BATF | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | BATF.Artillery | variant / BATF | 2000 / ? / ? | ? | not modeled; inspection required | multiple/conditional armaments |
| allies | BATF.Artillery.PDL | variant / BATF | 3000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | BATF.Artillery.Reflector | variant / BATF | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | BATF.AI | variant / BATF | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | IFV | base / IFV | 750 / 30000 / 113 | ? | not modeled; inspection required | cargo, hover/amphibious, multiple/conditional armaments |
| allies | IFV.AI | variant / IFV | 650 / 27000 / 113 | ? | not modeled; inspection required | multiple/conditional armaments |
| allies | RTNK | base / RTNK | 800 / 43000 / 82 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | RTNK.PDL | variant / RTNK | 1400 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | RTNK.Reflector | variant / RTNK | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | RTNK.PDL.Firerate | variant / RTNK | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | RTNK.Reflector.Firerate | variant / RTNK | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | RTNK.PDL.Toughness | variant / RTNK | ? / 63000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | RTNK.Reflector.Toughness | variant / RTNK | ? / 63000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | TNKD | base / TNKD | 750 / 55000 / 68 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | TNKD.PDL | variant / TNKD | 1350 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | TNKD.Reflector | variant / TNKD | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | TNKD.PDL.Burstfire | variant / TNKD | 1350 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | TNKD.Reflector.Burstfire | variant / TNKD | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | TNKD.PDL.Tough | variant / TNKD | 1350 / 80000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| allies | TNKD.Reflector.Tough | variant / TNKD | ? / 80000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | kims_wheel | base / kims_wheel | 300 / 12000 / 157 | ? | not modeled; inspection required | sensor |
| soviet | T-34 | base / T-34 | 600 / 42000 / 100 | ? | scalar design; runtime pending | multiple/conditional armaments |
| soviet | Devil_Tank | base / Devil_Tank | 700 / 45000 / 90 | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| soviet | Devil_Tank.PDL | variant / Devil_Tank | 1100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Devil_Tank.Reflector | variant / Devil_Tank | 900 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | SFTNK | prototype / SFTNK | 700 / 40000 / 82 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | BTR | base / BTR | 850 / 33000 / 100 | ? | not modeled; inspection required | cargo, multiple/conditional armaments |
| soviet | BTR.Surveillance | variant / BTR | 750 / 28000 / 100 | ? | not modeled; inspection required | cargo, sensor |
| soviet | NonaSVK | base / NonaSVK | 700 / 15000 / 110 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Chem_Sprayer | base / Chem_Sprayer | 600 / 32000 / 95 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Chem_Sprayer.Range | variant / Chem_Sprayer | 900 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Chem_Sprayer.Range.Metal_Acid | variant / Chem_Sprayer | 1100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Chem_Sprayer.Range.Spread | variant / Chem_Sprayer | 1000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Chem_Sprayer.Splash | variant / Chem_Sprayer | 800 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Chem_Sprayer.Splash.Metal_Acid | variant / Chem_Sprayer | 1100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Chem_Sprayer.Splash.Spread | variant / Chem_Sprayer | 1000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Chem_Sprayer.Cloud | variant / Chem_Sprayer | 850 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Chem_Sprayer.Cloud.Metal_Acid | variant / Chem_Sprayer | 1100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Chem_Sprayer.Cloud.Spread | variant / Chem_Sprayer | 1000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | DTRK | base / DTRK | 2500 / 5000 / 82 | ? | not modeled; inspection required | transformation/deploy |
| soviet | FTRK | base / FTRK | 500 / 15000 / 118 | ? | not modeled; inspection required | multiple/conditional armaments |
| soviet | FTRK.Barrage | variant / FTRK | 800 / ? / ? | ? | not modeled; inspection required | multiple/conditional armaments |
| soviet | HQ7 | base / HQ7 | 1000 / 20000 / 80 | ? | not modeled; inspection required | multiple/conditional armaments |
| soviet | HTK5 | base / HTK5 | 1000 / 20000 / 80 | ? | not modeled; inspection required | multiple/conditional armaments |
| soviet | Hyena | base / Hyena | 1000 / 14000 / 125 | ? | not modeled; inspection required | multiple/conditional armaments |
| soviet | V3RL | base / V3RL | 1200 / 16000 / 56 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | V3BRL | prototype / V3RL | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | V4RL | prototype / V3RL | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | V3 | projectile / - | 50 / 12000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | V3B | projectile / - | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | V4 | projectile / - | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | KATY | base / KATY | 1200 / 15000 / 68 | ? | not modeled; inspection required | multiple/conditional armaments |
| soviet | 2S3 | base / 2S3 | 1350 / 30000 / 68 | ? | not modeled; inspection required | multiple/conditional armaments |
| soviet | Peoples_Tank | base / Peoples_Tank | 1000 / 60000 / 75 | ? | not modeled; inspection required | multiple/conditional armaments |
| soviet | Peoples_Tank.Speaker | variant / Peoples_Tank | 1400 / 66000 / ? | ? | not modeled; inspection required | special attacks |
| soviet | Peoples_Tank.Speaker.Reflector | variant / Peoples_Tank | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Peoples_Tank.Speaker.PDL | variant / Peoples_Tank | 2000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Peoples_Tank.Mass_Production | variant / Peoples_Tank | 750 / 55000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Peoples_Tank.Mass_Production.Reflector | variant / Peoples_Tank | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Peoples_Tank.Mass_Production.PDL | variant / Peoples_Tank | 1000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Heavy_Tank | base / Heavy_Tank | 1100 / 65000 / 68 | ? | scalar design; runtime pending | inheritance, weapons, artwork |
| soviet | Heavy_Tank.AP | variant / Heavy_Tank | 1300 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Heavy_Tank.AP.PDL | variant / Heavy_Tank | 1650 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Heavy_Tank.AP.Reflector | variant / Heavy_Tank | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Heavy_Tank.Tesla | variant / Heavy_Tank | 1300 / ? / ? | ? | not modeled; inspection required | special attacks |
| soviet | Heavy_Tank.Tesla.PDL | variant / Heavy_Tank | 1650 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Heavy_Tank.Tesla.Reflector | variant / Heavy_Tank | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | TTNK | base / TTNK | 1150 / 38000 / 113 | ? | not modeled; inspection required | special attacks |
| soviet | TTNK.Arc | variant / TTNK | 1200 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | TTNK.RA2 | base / TTNK.RA2 | 1350 / 48000 / 100 | ? | scalar design; runtime pending | special attacks |
| soviet | TTNK.RA2.Arc | variant / TTNK.RA2 | 1400 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Source_of_Pollution | base / Source_of_Pollution | 1600 / 88000 / 52 | heavytracked | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Source_of_Pollution.Metal_Acid | variant / Source_of_Pollution | 1600 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Source_of_Pollution.Metal_Acid.Reflector | variant / Source_of_Pollution | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Source_of_Pollution.Metal_Acid.PDL | variant / Source_of_Pollution | 2100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Source_of_Pollution.Chem_Spray | variant / Source_of_Pollution | 1600 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Source_of_Pollution.Chem_Spray.Reflector | variant / Source_of_Pollution | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Source_of_Pollution.Chem_Spray.PDL | variant / Source_of_Pollution | 2100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Source_of_Pollution.Chem_Bomb | variant / Source_of_Pollution | 1600 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Source_of_Pollution.Chem_Bomb.Reflector | variant / Source_of_Pollution | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Source_of_Pollution.Chem_Bomb.PDL | variant / Source_of_Pollution | 2100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | ISU | base / ISU | 1600 / 65000 / 56 | heavytracked | not modeled; inspection required | multiple/conditional armaments |
| soviet | ISU.Concussion | variant / ISU | ? / ? / ? | ? | not modeled; inspection required | multiple/conditional armaments |
| soviet | ISU.Concussion.Reflector | variant / ISU | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | ISU.Concussion.PDL | variant / ISU | 2100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | ISU.Cluster | variant / ISU | ? / ? / ? | ? | not modeled; inspection required | multiple/conditional armaments |
| soviet | ISU.Cluster.Reflector | variant / ISU | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | ISU.Cluster.PDL | variant / ISU | 2100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | ISU.AP | variant / ISU | ? / ? / ? | ? | not modeled; inspection required | multiple/conditional armaments |
| soviet | ISU.AP.Reflector | variant / ISU | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | ISU.AP.PDL | variant / ISU | 2100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | apoc | base / apoc | 1700 / 78000 / 52 | heavytracked | not modeled; inspection required | multiple/conditional armaments |
| soviet | apoc.Speaker | variant / apoc | 2000 / 88000 / ? | ? | not modeled; inspection required | special attacks |
| soviet | apoc.Speaker.Reflector | variant / apoc | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | apoc.Speaker.PDL | variant / apoc | 2200 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | apoc.Nuke | variant / apoc | 2500 / 88000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | apoc.Nuke.Reflector | variant / apoc | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | apoc.Nuke.PDL | variant / apoc | 3000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | apoc.Drozd | variant / apoc | 2000 / 95000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | apoc.Drozd.Reflector | variant / apoc | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | apoc.Drozd.PDL | variant / apoc | 2500 / 88000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Soviet_Miner | base / Soviet_Miner | 1400 / 75000 / 56 | heavywheeled | not modeled; inspection required | special attacks |
| soviet | QTNK | base / QTNK | 1500 / 90000 / 56 | ? | not modeled; inspection required | transformation/deploy |
| soviet | Rice_Cooker | base / Rice_Cooker | 3000 / 35000 / 40 | ? | not modeled; inspection required | cargo, multiple/conditional armaments |
| soviet | Rice_Shell | projectile / - | 50 / 25000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Rice_PassengerShell | projectile / - | 0 / 25000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| soviet | Gene_Splicer | base / Gene_Splicer | 5000 / 150000 / 50 | sheavytracked | not modeled; inspection required | multiple/conditional armaments |
| soviet | Tsar_Tank | base / Tsar_Tank | 8000 / 150000 / 45 | sheavytracked | not modeled; inspection required | transformation/deploy, special attacks, multiple/conditional armaments |
| soviet | MCV.Soviet | base / MCV.Soviet | ? / ? / ? | ? | not modeled; inspection required | transformation/deploy |
| soviet | MCV.Nukular.Soviet | non-production form / MCV.Soviet | ? / ? / ? | ? | not modeled; inspection required | transformation/deploy |
| scrin | Channeler | base / Channeler | 1350 / 45000 / 61 | ? | not modeled; inspection required | hover/amphibious |
| scrin | Channeler.chain | variant / Channeler | 1500 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | Channeler.disc | variant / Channeler | 1350 / 55000 / 75 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | Channeler.disc.reflector | variant / Channeler | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | Channeler.disc.pdl | variant / Channeler | 1850 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | Channeler.chain.reflector | variant / Channeler | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | Channeler.chain.pdl | variant / Channeler | 2000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | HARV.Scrin | base / HARV.Scrin | 1400 / 75000 / 56 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | GUNW | base / GUNW | 650 / 30000 / 113 | ? | not modeled; inspection required | multiple/conditional armaments |
| scrin | GUNW.sensor | variant / GUNW | 800 / ? / ? | ? | not modeled; inspection required | sensor |
| scrin | SEEK | base / SEEK | 800 / 20000 / 135 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | SEEK.PDL | variant / SEEK | 1200 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | SEEK.Reflector | variant / SEEK | 800 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | LACE | base / LACE | 500 / 25000 / 157 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | LACE.PDL | variant / LACE | 1100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | LACE.Reflector | variant / LACE | 700 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | LACE.AP | variant / LACE | 700 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | INTL | base / INTL | 700 / 40000 / 135 | ? | not modeled; inspection required | cargo |
| scrin | INTL.AA | variant / INTL | 900 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | INTL.Teleport | variant / INTL | 800 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | CORR | base / CORR | 700 / 45000 / 82 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | CORR.Area | variant / CORR | 800 / 55000 / ? | ? | not modeled; inspection required | multiple/conditional armaments |
| scrin | CORR.Area.PDL | variant / CORR | 1300 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | CORR.Area.Reflector | variant / CORR | 800 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | CORR.Range | variant / CORR | 800 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | CORR.Range.PDL | variant / CORR | 1300 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | CORR.Range.Reflector | variant / CORR | 800 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | LCHR | base / LCHR | 900 / 44000 / 80 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | LCHR.Slow | variant / LCHR | 1100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | LCHR.Slow.PDL | variant / LCHR | 1800 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | LCHR.Slow.Reflector | variant / LCHR | 1100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | LCHR.Drain | variant / LCHR | 1100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | LCHR.Drain.PDL | variant / LCHR | 1800 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | LCHR.Drain.Reflector | variant / LCHR | 1100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | STCR | base / STCR | 1000 / 60000 / 71 | ? | not modeled; inspection required | hover/amphibious |
| scrin | STCR.Range | variant / STCR | 1100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | STCR.Range.PDL | variant / STCR | 1800 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | STCR.Range.Reflector | variant / STCR | 1100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | STCR.Arc | variant / STCR | 1100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | STCR.Arc.PDL | variant / STCR | 1800 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | STCR.Arc.Reflector | variant / STCR | 1100 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | DEVO | base / DEVO | 1250 / 35000 / 90 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | DEVO.acid | variant / DEVO | ? / ? / ? | ? | not modeled; inspection required | multiple/conditional armaments |
| scrin | DEVO.heavy | variant / DEVO | 1500 / 48000 / 65 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | RUIN | base / RUIN | 800 / 15000 / 80 | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | RUIN.barrage | variant / RUIN | 1400 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | RUIN.splash | variant / RUIN | 1200 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | ATMZ | base / ATMZ | 1250 / 22000 / 75 | lighthover | not modeled; inspection required | hover/amphibious |
| scrin | ATMZ.AA | variant / ATMZ | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | ATMZ.Range | variant / ATMZ | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | TPOD | base / TPOD | 1200 / 45000 / 66 | heavytracked | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | TPOD.chain | variant / TPOD | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | TPOD.chain.reflector | variant / TPOD | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | TPOD.chain.PDL | variant / TPOD | 1700 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | TPOD.acid | variant / TPOD | ? / ? / ? | ? | not modeled; inspection required | multiple/conditional armaments |
| scrin | TPOD.acid.reflector | variant / TPOD | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | TPOD.acid.pdl | variant / TPOD | 1700 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | RPTP | base / RPTP | 2200 / 98000 / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | RPTP.range | variant / RPTP | 2500 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | RPTP.range.reflector | variant / RPTP | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | RPTP.range.PDL | variant / RPTP | 3000 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | RPTP.acid | variant / RPTP | ? / ? / ? | ? | not modeled; inspection required | multiple/conditional armaments |
| scrin | RPTP.acid.reflector | variant / RPTP | ? / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | RPTP.acid.PDL | variant / RPTP | 2300 / ? / ? | ? | not modeled; inspection required | inheritance, weapons, artwork |
| scrin | Hexapod | base / Hexapod | 5000 / 150000 / 42 | sheavytracked | not modeled; inspection required | multiple/conditional armaments |
| scrin | SMCV | base / SMCV | 3000 / 75000 / 56 | ? | not modeled; inspection required | transformation/deploy, hover/amphibious |
| scrin | SMCV.Nukular | non-production form / SMCV | ? / ? / ? | ? | not modeled; inspection required | transformation/deploy |
