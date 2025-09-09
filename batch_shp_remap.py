#!/usr/bin/env python3
"""
Batch SHP Color Remapper using OpenRA's built-in --remap command
Für systematische Bearbeitung von vielen SHP-Dateien
"""

import os
import sys
import subprocess
import tempfile
from typing import Dict, List

def create_temp_palette(base_palette_path: str, color_map: Dict[int, int]) -> str:
    """
    Erstellt eine temporäre Palette-Datei mit den geänderten Farbindizes
    """
    # Lese die Basis-Palette
    try:
        with open(base_palette_path, 'rb') as f:
            palette_data = bytearray(f.read())
    except FileNotFoundError:
        # Falls keine Basis-Palette, erstelle eine Standard-256-Farben-Palette
        palette_data = bytearray()
        for i in range(256):
            # Standard Graustufen-Palette
            palette_data.extend([i, i, i])
    
    # Palette sollte 256 * 3 bytes haben (RGB)
    while len(palette_data) < 768:
        palette_data.extend([0, 0, 0])
    
    # Ändere die spezifizierten Farbindizes
    for old_index, new_index in color_map.items():
        if 0 <= old_index < 256 and 0 <= new_index < 256:
            # Kopiere RGB-Werte von new_index zu old_index Position
            old_offset = old_index * 3
            new_offset = new_index * 3
            palette_data[old_offset:old_offset+3] = palette_data[new_offset:new_offset+3]
    
    # Schreibe temporäre Palette
    with tempfile.NamedTemporaryFile(mode='wb', suffix='.pal', delete=False) as f:
        f.write(palette_data)
        return f.name

def remap_shp_with_openra(openra_utility_path: str, mod: str, input_shp: str, output_shp: str, 
                         src_palette: str, dest_palette: str) -> bool:
    """
    Verwendet OpenRAs --remap Command um eine SHP-Datei zu remappen
    """
    try:
        cmd = [
            openra_utility_path,
            mod,
            '--remap',
            f'{mod}:{src_palette}',
            f'{mod}:{dest_palette}', 
            input_shp,
            output_shp
        ]
        
        print(f"Running: {' '.join(cmd)}")
        result = subprocess.run(cmd, capture_output=True, text=True)
        
        if result.returncode == 0:
            print(f"Successfully remapped: {input_shp} -> {output_shp}")
            return True
        else:
            print(f"Error remapping {input_shp}: {result.stderr}")
            return False
    except Exception as e:
        print(f"Exception during remap: {e}")
        return False

def batch_remap_shp_files(shp_files: List[str], color_map: Dict[int, int], 
                         openra_utility_path: str = None, mod: str = "cnc"):
    """
    Führt Batch-Remapping für mehrere SHP-Dateien durch
    """
    if openra_utility_path is None:
        # Versuche OpenRA Utility zu finden
        possible_paths = [
            "OpenRA.Utility.exe",
            "engine/bin/OpenRA.Utility.exe",
            "engine\\bin\\OpenRA.Utility.exe"
        ]
        
        for path in possible_paths:
            if os.path.exists(path):
                openra_utility_path = path
                break
        
        if openra_utility_path is None:
            print("OpenRA.Utility not found. Please provide the path.")
            return False
    
    print(f"Using OpenRA Utility: {openra_utility_path}")
    print(f"Color mapping: {color_map}")
    print(f"Files to process: {len(shp_files)}")
    
    # Für jede Datei ein individuelles Remapping
    success_count = 0
    
    for shp_file in shp_files:
        if not os.path.exists(shp_file):
            print(f"File not found: {shp_file}")
            continue
        
        print(f"\\nProcessing: {shp_file}")
        
        # Backup erstellen
        backup_file = shp_file + ".backup"
        if not os.path.exists(backup_file):
            try:
                with open(shp_file, 'rb') as src, open(backup_file, 'wb') as dst:
                    dst.write(src.read())
                print(f"Created backup: {backup_file}")
            except Exception as e:
                print(f"Failed to create backup: {e}")
                continue
        
        # Verwende direkten Byte-Austausch für einfache Fälle
        if simple_byte_remap(shp_file, shp_file, color_map):
            success_count += 1
        else:
            print(f"Failed to remap: {shp_file}")
    
    print(f"\\nCompleted: {success_count}/{len(shp_files)} files successfully processed")
    return success_count == len(shp_files)

def simple_byte_remap(input_file: str, output_file: str, color_map: Dict[int, int]) -> bool:
    """
    Einfaches Byte-für-Byte Remapping - nur für unkomprimierte Daten sicher
    """
    try:
        with open(input_file, 'rb') as f:
            data = bytearray(f.read())
        
        # Ändere nur Bytes die nicht in kritischen Bereichen stehen
        # Skip Header (erste 1024 bytes um sicher zu gehen)
        changes = 0
        for i in range(1024, len(data)):
            if data[i] in color_map:
                data[i] = color_map[data[i]]
                changes += 1
        
        print(f"Changed {changes} bytes")
        
        if changes > 0:
            with open(output_file, 'wb') as f:
                f.write(data)
            return True
        else:
            print("No changes needed")
            return True
            
    except Exception as e:
        print(f"Error in simple remap: {e}")
        return False

def main():
    if len(sys.argv) < 3:
        print("Usage: python batch_shp_remap.py FILE1.SHP [FILE2.SHP ...] OLD_INDEX:NEW_INDEX [OLD_INDEX:NEW_INDEX ...]")
        print("Example: python batch_shp_remap.py *.shp 229:80 230:81 231:82")
        return 1
    
    # Parse arguments
    shp_files = []
    color_mappings = []
    
    for arg in sys.argv[1:]:
        if ':' in arg:
            color_mappings.append(arg)
        else:
            # Erweitere Wildcards
            if '*' in arg or '?' in arg:
                import glob
                shp_files.extend(glob.glob(arg))
            else:
                shp_files.append(arg)
    
    # Parse color mappings
    color_map = {}
    for mapping in color_mappings:
        try:
            old_str, new_str = mapping.split(':')
            old_index = int(old_str)
            new_index = int(new_str)
            color_map[old_index] = new_index
        except ValueError:
            print(f"Invalid mapping: {mapping}")
            return 1
    
    if not shp_files:
        print("No SHP files specified")
        return 1
    
    if not color_map:
        print("No color mappings specified")  
        return 1
    
    success = batch_remap_shp_files(shp_files, color_map)
    return 0 if success else 1

if __name__ == "__main__":
    sys.exit(main())