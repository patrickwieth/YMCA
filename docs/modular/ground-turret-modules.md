# Bodenfahrzeuge: benannte Turmbausteine

Vier **native Bausteine** ersetzen die bisherigen fahrzeugspezifischen/generischen Mount-IDs auf ihren vorhandenen Chassis.
Das sind keine vier neuen Fahrzeugfamilien und noch keine frei mischbare Battle-Fortress-Bestückung.

| Baustein | Originalbindung | Platzbedarf | Waffenkanäle | Grafik |
|---|---|---:|---|---|
| Prism Turret (`prism-turret`) | `Prismtank` | 1 | `PrisTLaser` | Voxel |
| Artillery Turret (`artillery-turret`) | `HOWI` | 1 | `155mmTDM` | Sprite |
| Missile Turret (`missile-turret`) | `STNK` | 1 | `StnkMissile`, `StnkMissile.AA` | Voxel |
| Sonic Turret (`sonic-turret`) | `DISR` | 1 | `SonicZap`, `SonicZapVisual`, `SonicZap.UPG`, `SonicZapVisual.UPG` | Sprite |

## Originalwerte und Speicherung

- Preise, Masse, elektrische Rechenwerte und Grundfahrzeugwerte sind unverändert. Keine neue Stromkalibrierung.
- Vorhandene elektrische Werte (insbesondere Disruptor) bleiben experimentelle Baukastenwerte, keine neu behauptete Originalphysik.
- `turret_modules` im Designer-Katalog speichert quellengebundene Turm-/Waffen-/Grafikrezepte mit Bedingungen, Offsets und Drehgeschwindigkeiten.
- Die Engine erbt weiterhin die vollständigen Originalwaffen und Spezialfunktionen. Rezepte erzeugen keine vereinfachten Waffen.
- Bestehende Einzelprofile und Fraktionsroster migrieren beim Laden: alte Prism-/Disruptor-Mount-IDs sowie Integrated Mount auf HOWI/STNK.
- Integrated Mount anderer Fahrzeuge bleibt unverändert. IDs/Namen gespeicherter Entwürfe bleiben erhalten.
- Die Oberfläche zeigt Modulname und `Turret slots: 1/1 occupied`; Battle Fortress behält Bunker `3/3`.

## Keine falsche Zusammenlegung von Waffenprofilen

- Prismtank: `PrisTLaser`; Prism Fortress: `BattlePrisLaser`. Die Prism Fortress verwendet ein vollständiges Rumpfbild und Frontangriff, keinen separat drehbaren Turm.
- Howitzer: `155mmTDM`; Artillery Fortress: `155mm`. Unterschiedliche Profile, keine pauschale Gleichsetzung mit einem Infanteriemörser.
- Stealth Tank: `StnkMissile` und `StnkMissile.AA`; Missile Fortress: `BATFRockets`. Zielkanäle/Projektile bleiben getrennt.
- Disruptor: vier bedingte Waffen-/Visual-Kanäle, aber **ein** physischer Turmplatz. Upgrade- und Visual-Kanäle sind keine zusätzlichen Türme.

## Battle Fortress: vorhandene Anschlusspunkte

Die drei benannten Turmpositionen aus den Originalregeln; Werte sind Engine-Weltkoordinaten, keine Pixel.
Ein zusätzlich geerbter unbenannter `Turreted`-Trait wird nicht als vierter Baukastenplatz gewertet.

| Original | Turm | Offset | Drehrate | Pause |
|---|---|---|---|---|
| `BATF` | primary | `200,-300,0` | 24 | `` |
| `BATF` | secondary | `200,300,0` | 24 | `` |
| `BATF` | tertiary | `-300,0,358` | 24 | `` |
| `BATF.Artillery` | primary | `200,-300,0` | 24 | `` |
| `BATF.Artillery` | secondary | `200,300,0` | 24 | `` |
| `BATF.Artillery` | tertiary | `-300,0,458` | 24 | `!cargo` |

### Grafikquellen / Prüfbedarf

- Artillery Fortress: `artytur.shp`; Missile Fortress Raketen: `stnk.shp`, Start 38, 32 Richtungen.
- HOWI-Turm: `howitzer.shp`, Start 32, 32 Richtungen (`sequences/vehicles.yaml`). Nicht automatisch derselbe Grafikbaustein wie `artytur.shp`.
- Sonic-Turm: `disruptorstygs.shp`, Start 32, 32 Richtungen (`sequences/gdi.yaml`). Als separate Turmgrafik vorhanden.
- Prism-/Stealth-Türme der aktuellen Bindungen sind Voxelmodelle. Übernahme auf einen Sprite-Rumpf braucht explizite Maßstabs-/Offset-/Palette-Prüfung.
- BATF ist im Original deaktiviert. BATF.Artillery bindet den dritten Turm/Schuss an `cargo`, besitzt aber keinen Cargo-Trait. Keine stille Reparatur oder Freischaltung.
- Bunker Module bleibt Super-Heavy-only und belegt alle drei Plätze. Originale Bunker+PDL/Reflector-Varianten werden nicht durch erfundene Stromgrenzen verworfen.

## Stand / nächster Implementierungsschritt

Die vier benannten Module sind auf ihren Originalfahrzeugen im Designer auswählbar und validiert.
Battle Fortress akzeptiert weiterhin nur seine geprüfte Bunker-Kombination. Noch nicht implementiert:
drei unabhängige Mount-Auswahlen, neue gemischte Geometrien, Übernahme der vier Türme auf diesen Rumpf und deren Live-Test.
Die Kopplung an den Originalakteur wird nativ geprüft; ein gemeinsamer Familienname ist keine beliebige Grafikfreigabe.

Referenz: [aufgelöste Bodenaufbauten](existing-vehicle-loadouts.md). Keine Änderungen an Luft-/Seefahrzeugen.

