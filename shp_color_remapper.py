#!/usr/bin/env python3
"""
SHP Color Index Remapper - Korrekte Version
Ändert nur Pixel-Farbindizes in OpenRA .shp Dateien, ohne Header/Struktur zu beschädigen
"""

import struct
import os
import sys
from typing import List, Tuple, Dict

class SHPFrame:
    def __init__(self):
        self.x = 0
        self.y = 0
        self.width = 0
        self.height = 0
        self.data = b''

def read_uint16(data, offset):
    return struct.unpack('<H', data[offset:offset+2])[0], offset + 2

def read_uint32(data, offset):
    return struct.unpack('<I', data[offset:offset+4])[0], offset + 4

def read_int16(data, offset):
    return struct.unpack('<h', data[offset:offset+2])[0], offset + 2

def write_uint16(value):
    return struct.pack('<H', value)

def write_uint32(value):
    return struct.pack('<I', value)

def write_int16(value):
    return struct.pack('<h', value)

def decode_shp_frame(data, offset, width, height):
    """Dekodiert einen SHP Frame (Format 80h - RLE komprimiert)"""
    pixels = bytearray(width * height)
    pos = 0
    data_pos = offset
    
    # Zeilen-Offsets lesen
    line_offsets = []
    for _ in range(height):
        if data_pos + 2 > len(data):
            return pixels
        line_offset, data_pos = read_uint16(data, data_pos)
        line_offsets.append(line_offset + offset)
    
    # Jede Zeile dekodieren
    for y in range(height):
        if line_offsets[y] >= len(data):
            continue
            
        line_pos = line_offsets[y]
        x = 0
        
        while x < width and line_pos < len(data):
            cmd = data[line_pos]
            line_pos += 1
            
            if cmd == 0:  # End of line
                break
            elif cmd & 0x80:  # Skip transparent pixels
                x += cmd & 0x7F
            elif cmd & 0x40:  # RLE compressed pixels
                if line_pos >= len(data):
                    break
                count = cmd & 0x3F
                color = data[line_pos]
                line_pos += 1
                
                for _ in range(count):
                    if x < width:
                        pixels[y * width + x] = color
                        x += 1
            else:  # Raw pixels
                count = cmd
                for _ in range(count):
                    if line_pos >= len(data) or x >= width:
                        break
                    pixels[y * width + x] = data[line_pos]
                    line_pos += 1
                    x += 1
    
    return pixels

def encode_shp_frame(pixels, width, height):
    """Enkodiert Pixel-Daten zurück ins SHP RLE Format"""
    lines_data = []
    
    for y in range(height):
        line_data = bytearray()
        x = 0
        
        while x < width:
            # Transparent pixels zählen (Index 0)
            transparent_count = 0
            while x + transparent_count < width and pixels[y * width + x + transparent_count] == 0:
                transparent_count += 1
            
            if transparent_count > 0:
                # Skip transparent pixels (max 127 per command)
                while transparent_count > 0:
                    skip = min(127, transparent_count)
                    line_data.append(0x80 | skip)
                    transparent_count -= skip
                    x += skip
                continue
            
            # Nicht-transparente Pixels
            start_x = x
            while x < width and pixels[y * width + x] != 0:
                x += 1
            
            pixel_count = x - start_x
            
            # RLE oder Raw encoding wählen
            if pixel_count > 1:
                # Schauen ob RLE lohnt sich
                current_color = pixels[y * width + start_x]
                rle_count = 1
                while start_x + rle_count < x and pixels[y * width + start_x + rle_count] == current_color:
                    rle_count += 1
                
                if rle_count >= 3:  # RLE lohnt sich
                    rle_count = min(63, rle_count)  # Max 63 per RLE command
                    line_data.append(0x40 | rle_count)
                    line_data.append(current_color)
                    start_x += rle_count
                    x = start_x
                    continue
            
            # Raw pixels (max 63 per command)
            raw_count = min(63, pixel_count)
            line_data.append(raw_count)
            for i in range(raw_count):
                line_data.append(pixels[y * width + start_x + i])
            start_x += raw_count
            x = start_x
        
        line_data.append(0)  # End of line
        lines_data.append(bytes(line_data))
    
    # Zeilen-Offsets berechnen
    offset_table_size = height * 2
    current_offset = offset_table_size
    offsets = []
    
    for line in lines_data:
        offsets.append(current_offset)
        current_offset += len(line)
    
    # Zusammenbauen
    result = bytearray()
    
    # Offset-Tabelle
    for offset in offsets:
        result.extend(write_uint16(offset))
    
    # Zeilen-Daten
    for line in lines_data:
        result.extend(line)
    
    return bytes(result)

def parse_shp_file(data):
    """Parsed eine SHP-Datei und gibt Frames zurück"""
    if len(data) < 2:
        return []
    
    frame_count, pos = read_uint16(data, 0)
    frames = []
    
    for i in range(frame_count):
        if pos + 6 > len(data):
            break
        
        frame_offset, pos = read_uint32(data, pos)
        format_byte = data[pos]
        pos += 1
        width = data[pos]
        pos += 1
        height = data[pos]  
        pos += 1
        
        if format_byte != 0x80:
            print(f"Warnung: Unbekanntes Format {format_byte:02x} in Frame {i}")
            continue
        
        if frame_offset >= len(data):
            print(f"Warnung: Ungültiger Offset {frame_offset} in Frame {i}")
            continue
        
        frame = SHPFrame()
        frame.width = width
        frame.height = height
        
        # Frame dekodieren
        pixels = decode_shp_frame(data, frame_offset, width, height)
        frame.data = bytes(pixels)
        frames.append((frame_offset, format_byte, frame))
    
    return frames

def remap_colors_in_pixels(pixels, color_map):
    """Ändert Farbindizes in Pixel-Daten"""
    result = bytearray(pixels)
    changes = 0
    
    for i in range(len(result)):
        if result[i] in color_map:
            result[i] = color_map[result[i]]
            changes += 1
    
    return bytes(result), changes

def rebuild_shp_file(frames, original_data):
    """Baut SHP-Datei mit geänderten Frames neu auf"""
    if not frames:
        return original_data
    
    frame_count = len(frames)
    
    # Header aufbauen
    result = bytearray()
    result.extend(write_uint16(frame_count))
    
    # Platz für Frame-Einträge reservieren
    header_size = 2 + frame_count * 7  # 2 für count + 7 bytes per frame
    frame_data_start = header_size
    
    frame_entries = []
    current_offset = frame_data_start
    
    # Frame-Daten enkodieren
    encoded_frames = []
    for original_offset, format_byte, frame in frames:
        # Pixel-Daten zu RLE enkodieren
        encoded_data = encode_shp_frame(frame.data, frame.width, frame.height)
        encoded_frames.append(encoded_data)
        
        # Frame-Eintrag
        frame_entries.append({
            'offset': current_offset,
            'format': format_byte,
            'width': frame.width,
            'height': frame.height
        })
        
        current_offset += len(encoded_data)
    
    # Frame-Einträge schreiben
    for entry in frame_entries:
        result.extend(write_uint32(entry['offset']))
        result.append(entry['format'])
        result.append(entry['width'])
        result.append(entry['height'])
    
    # Frame-Daten anhängen
    for encoded_data in encoded_frames:
        result.extend(encoded_data)
    
    return bytes(result)

def process_shp_file(file_path, color_map):
    """Verarbeitet eine SHP-Datei"""
    print(f"Verarbeite: {file_path}")
    
    # Backup erstellen
    backup_path = file_path + ".backup"
    if not os.path.exists(backup_path):
        print(f"Erstelle Backup: {backup_path}")
        with open(file_path, 'rb') as src, open(backup_path, 'wb') as dst:
            dst.write(src.read())
    
    # Datei laden
    with open(file_path, 'rb') as f:
        data = f.read()
    
    # SHP parsen
    frames = parse_shp_file(data)
    if not frames:
        print("Keine gültigen Frames gefunden")
        return False
    
    print(f"Gefundene Frames: {len(frames)}")
    
    # Farbindizes in jedem Frame ändern
    total_changes = 0
    modified_frames = []
    
    for original_offset, format_byte, frame in frames:
        new_pixels, changes = remap_colors_in_pixels(frame.data, color_map)
        frame.data = new_pixels
        total_changes += changes
        modified_frames.append((original_offset, format_byte, frame))
    
    print(f"Farbindizes geändert: {total_changes}")
    
    if total_changes > 0:
        # SHP-Datei neu aufbauen
        new_data = rebuild_shp_file(modified_frames, data)
        
        # Speichern
        with open(file_path, 'wb') as f:
            f.write(new_data)
        
        print("Datei gespeichert")
        return True
    else:
        print("Keine Änderungen nötig")
        return False

def main():
    # Farbindex-Mapping: 229-239 -> 80-90
    color_map = {}
    for i in range(11):
        old_index = 229 + i
        new_index = 80 + i
        color_map[old_index] = new_index
    
    print("Farbindex-Mapping:")
    for old, new in color_map.items():
        print(f"  {old} -> {new}")
    
    shp_path = r"E:\src\YMCA - cameo engine integration - Kopie\mods\ca\bits\misc\parachute.shp"
    
    if not os.path.exists(shp_path):
        print(f"FEHLER: Datei nicht gefunden: {shp_path}")
        return 1
    
    success = process_shp_file(shp_path, color_map)
    
    if success:
        print("Erfolgreich abgeschlossen!")
    else:
        print("Verarbeitung fehlgeschlagen")
    
    return 0

if __name__ == "__main__":
    sys.exit(main())