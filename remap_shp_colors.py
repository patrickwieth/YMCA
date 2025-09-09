#!/usr/bin/env python3
"""
SHP Color Index Remapper
Ändert Farbindizes in OpenRA .shp Dateien
"""

import struct
import os
import sys

def read_shp_header(file_handle):
    """Liest den SHP Header"""
    # SHP Header: 2 bytes frame count, dann für jeden Frame: 4 bytes offset, 1 byte format, 1 byte width, 1 byte height
    frame_count = struct.unpack('<H', file_handle.read(2))[0]
    print(f"Frames gefunden: {frame_count}")
    
    frames = []
    for i in range(frame_count):
        offset = struct.unpack('<I', file_handle.read(4))[0]
        format_byte = struct.unpack('<B', file_handle.read(1))[0]
        width = struct.unpack('<B', file_handle.read(1))[0] 
        height = struct.unpack('<B', file_handle.read(1))[0]
        frames.append({
            'offset': offset,
            'format': format_byte,
            'width': width,
            'height': height
        })
    
    return frame_count, frames

def remap_color_indices(data, color_map):
    """Ändert Farbindizes in den Bilddaten"""
    result = bytearray(data)
    changes = 0
    
    for i in range(len(result)):
        if result[i] in color_map:
            old_color = result[i]
            result[i] = color_map[old_color]
            changes += 1
    
    print(f"Farbindizes geändert: {changes}")
    return bytes(result)

def process_shp_file(input_path, output_path, color_map):
    """Verarbeitet eine .shp Datei und ändert die Farbindizes"""
    print(f"Verarbeite: {input_path}")
    
    # Backup erstellen
    backup_path = input_path + ".backup"
    if not os.path.exists(backup_path):
        print(f"Erstelle Backup: {backup_path}")
        with open(input_path, 'rb') as src, open(backup_path, 'wb') as dst:
            dst.write(src.read())
    
    with open(input_path, 'rb') as f:
        # Komplette Datei einlesen
        data = f.read()
    
    # Farbindizes ändern
    remapped_data = remap_color_indices(data, color_map)
    
    # Geänderte Datei speichern
    with open(output_path, 'wb') as f:
        f.write(remapped_data)
    
    print(f"Gespeichert: {output_path}")

def main():
    # Farbindex-Mapping: von 229-239 zu 80-90
    color_map = {}
    for i in range(11):  # 229-239 sind 11 Werte (239-229+1=11)
        old_index = 229 + i
        new_index = 80 + i
        color_map[old_index] = new_index
    
    print("Farbindex-Mapping:")
    for old, new in color_map.items():
        print(f"  {old} -> {new}")
    
    # Pfad zur parachute.shp
    shp_path = r"E:\src\YMCA - cameo engine integration - Kopie\mods\ca\bits\misc\parachute.shp"
    
    if not os.path.exists(shp_path):
        print(f"FEHLER: Datei nicht gefunden: {shp_path}")
        return 1
    
    # Verarbeiten
    process_shp_file(shp_path, shp_path, color_map)
    print("Fertig!")
    return 0

if __name__ == "__main__":
    sys.exit(main())