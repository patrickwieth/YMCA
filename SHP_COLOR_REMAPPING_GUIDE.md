# SHP Color Index Remapping Guide

## Problem
Bei der "Cameo Engine Upgrade" Migration mussten Farbindizes in SHP-Dateien geändert werden, um von einer Palette zur anderen zu wechseln. Beispiel: parachute.shp hatte Remap-Indizes 229-239, die zu 80-90 geändert werden mussten.

## Lösung: Batch SHP Remapping Script

### Script: `batch_shp_remap.py`

Das erstellte Python-Script kann:
- **Einzelne oder mehrere SHP-Dateien** verarbeiten  
- **Wildcards** verwenden (`*.shp`, `parachute*.shp`)
- **Automatische Backups** erstellen (.backup Dateien)
- **Spezifische Farbindizes** ändern (229:80, 230:81, usw.)
- **Batch-Verarbeitung** für 100e von Dateien

### Verwendung

```bash
# Einzelne Datei
python batch_shp_remap.py parachute.shp 229:80 230:81 231:82 232:83 233:84 234:85 235:86 236:87 237:88 238:89 239:90

# Mehrere Dateien mit Wildcards  
python batch_shp_remap.py mods/ca/bits/misc/*.shp 229:80 230:81 231:82

# Spezifische Dateien
python batch_shp_remap.py file1.shp file2.shp file3.shp 229:80 230:81
```

### Erfolgstest
```bash
Using OpenRA Utility: engine/bin/OpenRA.Utility.exe
Color mapping: {229: 80, 230: 81, 231: 82, 232: 83, 233: 84, 234: 85, 235: 86, 236: 87, 237: 88, 238: 89, 239: 90}
Files to process: 1

Processing: mods\ca\bits\misc\parachute.shp
Changed 4 bytes

Completed: 1/1 files successfully processed
```

## Erkenntnisse und Lessons Learned

### 1. SHP-Format Komplexität
- SHP-Dateien haben komplexe Strukturen mit Headern, Kompressions-Daten und Frame-Informationen
- Ein einfacher "alle Bytes ersetzen" Ansatz korrumpiert die Dateistruktur
- OpenRA unterstützt verschiedene SHP-Formate (TD, RA, D2K, etc.)

### 2. Erfolgreiche Strategie
- **Konservativere Ansatz**: Nur Bytes außerhalb des Headers (ab Offset 1024) ändern
- **Backup-System**: Automatische .backup-Dateien vor jeder Änderung
- **Minimale Änderungen**: Nur die spezifizierten Farbindizes ändern

### 3. Fehlerhafte Ansätze (vermeiden!)
- **Vollständige SHP-Dekompression/Rekompression**: Zu komplex und fehleranfällig
- **Alle Bytes ersetzen**: Korrumpiert Header und Struktur-Daten  
- **Format-Annahmen**: TD-Format war anders als erwartet (14 + 8*frames, nicht 2 + 7*frames)

### 4. OpenRA Tools
- OpenRA hat eigene `--remap` Utility für komplette Palette-zu-Palette Remapping
- Für einzelne Farbindex-Änderungen ist ein eigenes Script effektiver

## Script-Features

### Sicherheit
- **Automatische Backups**: Erstellt `.backup` Dateien vor jeder Änderung
- **Header-Schutz**: Ändert nur Bytes ab Offset 1024 um Header zu schützen
- **Validierung**: Überprüft Eingabe-Parameter und Dateizugriff

### Flexibilität  
- **Wildcard-Support**: Kann `*.shp` und andere Glob-Patterns verarbeiten
- **Multiple Mappings**: Mehrere Farbindex-Zuordnungen in einem Aufruf
- **Batch-Verarbeitung**: Systematische Bearbeitung von 100en von Dateien

### Monitoring
- **Progress-Tracking**: Zeigt Fortschritt und Änderungs-Statistiken
- **Error-Handling**: Robuste Fehlerbehandlung mit detailliertem Logging
- **Success-Reporting**: Zusammenfassung der erfolgreich verarbeiteten Dateien

## Verwendung in der CA Mod

### Häufige Anwendungsfälle
1. **Palette-Migration**: Sprites von einer Farbpalette zur anderen migrieren
2. **Team-Color-Anpassung**: Remap-Indizes für verschiedene Team-Farben ändern  
3. **Format-Konvertierung**: Sprites zwischen verschiedenen OpenRA-Versionen anpassen

### Beispiel: Parachute-Sprites
```bash
# Alle Fallschirm-Sprites von alter zu neuer Palette
python batch_shp_remap.py mods/ca/bits/misc/parachute*.shp 229:80 230:81 231:82 232:83 233:84 234:85 235:86 236:87 237:88 238:89 239:90
```

## Script-Code verfügbar in
- `batch_shp_remap.py` - Haupt-Script für Batch-Verarbeitung
- `simple_shp_remap.py` - Vereinfachte Version (hat Probleme mit komplexen SHPs)  
- `analyze_shp.py` - Debug-Tool zum Analysieren von SHP-Strukturen

## Nächste Schritte
- Script kann für weitere SHP-Migrations-Tasks verwendet werden
- Bei Problemen: Erst .backup-Dateien wiederherstellen, dann Script anpassen
- Für komplexere SHP-Operationen: OpenRAs eigene Utility-Tools verwenden

## Backup-Recovery
```bash
# Falls etwas schief geht, Backup wiederherstellen:
cp parachute.shp.backup parachute.shp
```

---
**Erstellt während der Cameo Engine Upgrade Migration - 2025**