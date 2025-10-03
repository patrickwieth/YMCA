# Combined Arms (CA) Mod - Project Knowledge Base

## Project Overview

**Project Name:** YMCA - Cameo Engine Integration  
**Main Task:** "Cameo Engine Upgrade" - Migration von Support Powers aus Gebäude-Definitionen zu separaten Template-basierten Dateien  
**Branch:** `cameo-engine-upgrade`  
**Main Branch:** `master`  

### Project Structure
```
E:\src\YMCA - cameo engine integration - Kopie\
├── engine\                     # OpenRA Engine Code
├── mods\ca\                   # Combined Arms Mod
├── OpenRA.Mods.CA\           # CA Mod C# Code
├── OpenRA.Mods.Cameo\        # Cameo-spezifische Erweiterungen
└── Various scripts and tools
```

## Cameo Engine Upgrade - Hauptaufgabe

### Problem
Support Powers waren ursprünglich direkt in Gebäude-Definitionen (structures.yaml) eingebettet. Das neue System verwendet:
- **Separate supportpowers.yaml Dateien** pro Fraktion
- **Template-System** mit `^` Notation für Vererbung
- **Dummy Proxy Actors** die die Templates erben

### Template-System
```yaml
# Template Definition
^AlliesSupportPowers:
    # Support Power Definitionen hier

# Proxy Actor
allies.dummy:
    Inherits: ^AlliesSupportPowers
```

### Fraktionen und Files
- **Allies:** `mods/ca/rules/allies/supportpowers.yaml`
- **GDI:** `mods/ca/rules/gdi/supportpowers.yaml` 
- **Soviet:** `mods/ca/rules/soviet/supportpowers.yaml`
- **China:** `mods/ca/rules/china/supportpowers.yaml`
- **NOD:** `mods/ca/rules/nod/supportpowers.yaml`
- **Scrin:** `mods/ca/rules/scrin/supportpowers.yaml`

## Behobene Issues

### 1. Support Power Migration
**Status:** ✅ Completed
- Alle Support Powers erfolgreich von structures.yaml zu separaten Dateien migriert
- Template-System funktioniert korrekt
- Proxy Actors erstellt und funktional

### 2. Missing ChronoshiftAI Power
**Status:** ✅ Fixed
- ChronoshiftAI Power war bei der Migration verloren gegangen
- Hinzugefügt zu `allies/supportpowers.yaml` mit korrekten Parametern
- Verwendet `ChronoshiftPower` statt `RA2ChronoshiftPower` für Engine-Kompatibilität

### 3. ClassicAirstrikePower Compile Errors
**Status:** ✅ Fixed
**File:** `OpenRA.Mods.CA/Traits/SupportPowers/ClassicAirstrikePower.cs`
- **SelectDirectionalTarget Constructor:** Von 6 auf 4 Parameter geändert
- **Inheritance:** `ClassicAirstrikePowerInfo` erbt von `DirectionalSupportPowerInfo`
- **BeaconPoster → BeaconPosters:** Dictionary-basierte Zugriffe korrigiert

### 4. Soviet NukePower NullReference
**Status:** ✅ Fixed  
**File:** `mods/ca/rules/soviet/supportpowers.yaml`
- Geändert von `NukePowerCA@AtomBomb` zu `NukePower@AtomBomb`
- Problem: Custom NukePowerCA Trait war nicht kompatibel mit neuer Engine

### 5. InterceptorPower Missing/Broken
**Status:** ✅ Fixed
**File:** `mods/ca/rules/gdi/supportpowers.yaml`
- `InterceptorPower@EagleAirDef` war nicht übertragen worden
- Parameter korrigiert: `SquadSizes` → `SquadSize`, `UnitType` korrigiert
- Prerequisites von leer zu korrekten Werten

### 6. Eye 'Active' Sequence Problem
**Status:** ✅ Fixed
- Fehler: "Image `eye` does not have a sequence named `active`"
- Problem war bei GDI, nicht Allies
- Korrigierte ActiveSequence Parameter in Superweapon-Definitionen

### 7. Tarantula Strafing Run Crash
**Status:** ✅ Fixed
- Fehler: "Bounds of actor tarantula 192 are empty"
- Lösung: `AttackBomberCA` Trait zu Tarantula hinzugefügt
- RenderSprites-Problematik gelöst

## Support Power Types & Syntax

### Häufige Support Power Types
- `ChronoshiftPower` - Teleportation
- `NukePower` - Nuclear Strike  
- `AirstrikePower` - Bomber Attacks
- `ParatroopersPower` - Paratrooper Drops
- `InterceptorPower` - Air Defense
- `DetonateWeaponPower` - Area Effect Powers
- `GrantExternalConditionPowerCA` - Buff Powers

### Template Kategorien
- `^[Faction]SupportPowers` - Normale Support Powers
- `^[Faction]SuperweaponBig` - Große Superwaffen
- `^[Faction]SuperweaponSmall` - Kleine Superwaffen

### Wichtige Parameter
```yaml
OrderName: eindeutiger-name
Icons: { 1: icon-name }
ChargeInterval: millisekunden
Prerequisites: { 1: bedingungen }
Names: { 1: "Display Name" }
Descriptions: { 1: "Beschreibung" }
```

## Dateienstruktur & wichtige Files

### Support Power Dateien
- Alle in `mods/ca/rules/[faction]/supportpowers.yaml`
- Template-basiert mit `^` Vererbung
- Kategorisiert nach Power-Typ (normal/superweapon)

### Sequence Definitionen
- `mods/ca/sequences/misc.yaml` - Verschiedene Effekte und Sprites
- `mods/ca/sequences/structures.yaml` - Gebäude-Animationen
- Sprite-Dateien in `mods/ca/bits/misc/`

### C# Code
- `OpenRA.Mods.CA/Traits/SupportPowers/` - Custom Support Power Traits
- `OpenRA.Mods.CA/UtilityCommands/` - Utility Tools
- Engine code in `engine/` Ordner

## Tools und Scripts

### SHP Color Remapping
**File:** `batch_shp_remap.py`
- Ändert Farbindizes in .shp Sprite-Dateien
- Automatische Backups, Wildcard-Support
- **Beispiel:** `python batch_shp_remap.py *.shp 229:80 230:81`

### Build System
- **Compile:** `dotnet build`
- **Engine:** Separate OpenRA Engine in `engine/` Ordner
- **Mods:** CA und Cameo Mods in separaten Ordnern

## Git Workflow

### Current Branch: `cameo-engine-upgrade`
### Typical Git Status:
```
M OpenRA.Mods.CA/Widgets/Logic/Ingame/SelectionTooltipLogic.cs
M mods/ca/chrome.yaml
M mods/ca/rules/[faction]/supportpowers.yaml (various)
M mods/ca/sequences/misc.yaml
+ Neue supportpowers.yaml Dateien
```

### Recent Commits Pattern:
- `fix further bugs from merge`
- `fix spawnrandomactor`  
- `fix merge conflicts`
- `menu labels fixed`
- `first starting version of engine upgrade`

## Bekannte Fallstricke & Lessons Learned

### 1. Support Power Parameter Syntax
- **Dictionary Syntax:** `{ 1: value }` für Listen
- **Prerequisites:** Oft `{ 1: faction, tier }` Format
- **Icons/Names/Descriptions:** Immer mit `{ 1: ... }` wrapper

### 2. Engine Kompatibilität
- Manche CA-spezifische Traits (`NukePowerCA`) sind nicht Engine-kompatibel
- Verwende Standard-Engine Traits wo möglich
- Custom Traits nur wenn nötig und gut getestet

### 3. Template Inheritance
- `^` prefix für Templates
- Templates müssen vor Nutzung definiert werden
- Proxy Actors mit `Inherits:` für Template-Aktivierung

### 4. Sprite/Sequence Probleme
- Sequence-Namen müssen exakt mit Definitionen übereinstimmen
- `ActiveSequence` Parameter oft problematisch bei Migration
- SHP-Dateien haben komplexe Farbpaletten-Systeme

### 5. C# Compilation Issues
- Constructor-Parameter ändern sich zwischen Engine-Versionen
- `BeaconPoster` → `BeaconPosters` Dictionary Migration häufig
- Inheritance-Änderungen bei Support Power Info Classes

## Development Workflow

### Testing Support Powers
1. Kompilieren mit `dotnet build`
2. Spiel starten und Support Power testen
3. Bei Fehlern: Log-Dateien checken
4. Häufige Fehler: NullReference, Missing Sequences, Parameter Mismatch

### Debugging Issues
1. **Missing Support Powers:** Check `supportpowers.yaml` und Prerequisites
2. **Runtime Errors:** Check Engine-Kompatibilität von Custom Traits
3. **Sprite Issues:** Check sequence definitions und SHP-Dateien
4. **Compilation Errors:** Check Constructor-Parameter und Inheritance

### Systematic Approach
1. Eine Fraktion nach der anderen bearbeiten
2. Template-System verwenden für Code-Wiederverwendung
3. Backups von funktionierenden Zuständen
4. Schrittweise Migration mit Tests zwischen Schritten

## Nächste mögliche Aufgaben

### Noch zu erledigende Arbeiten
- Scrin Support Powers vollständig validieren
- Weitere SHP Color Remapping Tasks
- Performance-Optimierung der Templates
- Vollständige Test-Coverage aller Support Powers

### Wartung
- Regelmäßige Engine-Updates integrieren
- Support Power Balance-Änderungen
- Neue Support Powers nach Template-System hinzufügen

---
**Erstellt während der Cameo Engine Upgrade Migration - 2025**  
**Letzte Aktualisierung:** Nach SHP Remapping Implementation