#!/usr/bin/env python3
"""
Simple SHP Color Index Remapper
Basierend auf OpenRAs ShpTDLoader Logic - ändert nur spezifische Farbindizes
"""

import struct
import os
import sys
from typing import Dict, List, Tuple

def read_uint16(data: bytes, offset: int) -> Tuple[int, int]:
    """Liest uint16 little-endian"""
    return struct.unpack('<H', data[offset:offset+2])[0], offset + 2

def read_uint32(data: bytes, offset: int) -> Tuple[int, int]:
    """Liest uint32 little-endian"""  
    return struct.unpack('<I', data[offset:offset+4])[0], offset + 4

def write_uint16(value: int) -> bytes:
    """Schreibt uint16 little-endian"""
    return struct.pack('<H', value)

def write_uint32(value: int) -> bytes:
    """Schreibt uint32 little-endian"""
    return struct.pack('<I', value)

def decode_frame_format80(data: bytes, offset: int, width: int, height: int) -> bytes:
    """
    Dekodiert ein SHP Frame im Format 80h (wie in OpenRA ShpTDLoader)
    Simplified version - extrahiert nur die Pixel-Daten
    """
    pixels = bytearray(width * height)
    
    # Skip frame header - gehe direkt zu den Scan-Line Offsets  
    scan_line_offsets = []
    pos = offset
    
    # Lese die Scan-Line Offsets (ein uint16 pro Zeile)
    for y in range(height):
        if pos + 2 > len(data):
            break
        line_offset, pos = read_uint16(data, pos)
        scan_line_offsets.append(line_offset + offset)
    
    # Dekodiere jede Scan-Line
    for y in range(height):
        if y >= len(scan_line_offsets) or scan_line_offsets[y] >= len(data):
            continue
            
        line_pos = scan_line_offsets[y]
        x = 0
        
        while x < width and line_pos < len(data):
            cmd = data[line_pos]
            line_pos += 1
            
            if cmd == 0:  # End of line marker
                break
            elif cmd & 0x80:  # Skip pixels (transparent)
                skip_count = cmd & 0x7F
                x += skip_count
            elif cmd & 0x40:  # RLE run
                if line_pos >= len(data):
                    break
                count = cmd & 0x3F
                color = data[line_pos]
                line_pos += 1
                
                for i in range(count):
                    if x + i < width:
                        pixels[y * width + x + i] = color
                x += count
            else:  # Raw pixels
                count = cmd
                for i in range(count):
                    if line_pos >= len(data) or x + i >= width:
                        break
                    pixels[y * width + x + i] = data[line_pos]
                    line_pos += 1
                x += count
    
    return bytes(pixels)

def encode_frame_format80(pixels: bytes, width: int, height: int) -> bytes:
    """
    Enkodiert Pixel-Daten zurück ins Format 80h
    Simplified version - RLE komprimierung
    """
    if len(pixels) != width * height:
        raise ValueError("Pixel data size mismatch")
        
    # Enkodiere jede Zeile
    scan_lines = []
    
    for y in range(height):
        line_data = bytearray()
        x = 0
        
        while x < width:
            pixel = pixels[y * width + x]
            
            if pixel == 0:  # Transparente Pixel
                # Zähle transparente Pixel
                transparent_count = 0
                while x + transparent_count < width and pixels[y * width + x + transparent_count] == 0:
                    transparent_count += 1
                
                # Schreibe Skip-Commands (max 127 per command)
                while transparent_count > 0:
                    skip = min(127, transparent_count)
                    line_data.append(0x80 | skip)
                    transparent_count -= skip
                    x += skip
            else:
                # Nicht-transparente Pixel - verwende RLE oder Raw
                start_x = x
                current_pixel = pixel
                
                # Schaue wie viele gleiche Pixel folgen
                rle_count = 1
                while x + rle_count < width and pixels[y * width + x + rle_count] == current_pixel:
                    rle_count += 1
                
                if rle_count >= 3:  # RLE lohnt sich
                    # RLE Command (max 63 per command)
                    while rle_count > 0:
                        count = min(63, rle_count)
                        line_data.append(0x40 | count)
                        line_data.append(current_pixel)
                        rle_count -= count
                        x += count
                else:
                    # Raw pixels - sammle nicht-wiederholende Pixel
                    raw_pixels = [current_pixel]
                    x += 1
                    
                    while (x < width and 
                           len(raw_pixels) < 63 and 
                           pixels[y * width + x] != 0 and
                           (x + 1 >= width or pixels[y * width + x] != pixels[y * width + x + 1])):
                        raw_pixels.append(pixels[y * width + x])
                        x += 1
                    
                    # Schreibe Raw Command
                    line_data.append(len(raw_pixels))
                    line_data.extend(raw_pixels)
        
        line_data.append(0)  # End of line
        scan_lines.append(bytes(line_data))
    
    # Baue Frame zusammen
    # Zuerst die Offset-Tabelle
    offset_table_size = height * 2
    current_offset = offset_table_size
    offsets = []
    
    for scan_line in scan_lines:
        offsets.append(current_offset)
        current_offset += len(scan_line)
    
    # Schreibe Frame
    frame_data = bytearray()
    
    # Offset-Tabelle
    for offset in offsets:
        frame_data.extend(write_uint16(offset))
    
    # Scan-Line Daten
    for scan_line in scan_lines:
        frame_data.extend(scan_line)
    
    return bytes(frame_data)

def parse_shp_file(data: bytes) -> List[Tuple[int, int, int, int, bytes]]:
    """
    Parsed SHP-Datei und gibt Frame-Informationen zurück
    Returns: List of (offset, width, height, format, frame_data)
    """
    if len(data) < 2:
        return []
    
    frame_count, pos = read_uint16(data, 0)
    frames = []
    
    print(f"Parsing {frame_count} frames...")
    
    for i in range(frame_count):
        if pos + 6 > len(data):
            print(f"Warning: Incomplete frame header {i}")
            break
        
        frame_offset, pos = read_uint32(data, pos)
        format_id = data[pos]
        pos += 1
        width = data[pos]
        pos += 1  
        height = data[pos]
        pos += 1
        
        if format_id != 0x80:
            print(f"Warning: Unsupported format {format_id:02x} in frame {i}")
            continue
            
        if frame_offset + 2 >= len(data):
            print(f"Warning: Invalid offset {frame_offset} for frame {i}")
            continue
        
        print(f"Frame {i}: {width}x{height} at offset {frame_offset}")
        
        # Dekodiere Frame
        try:
            pixel_data = decode_frame_format80(data, frame_offset, width, height)
            frames.append((frame_offset, width, height, format_id, pixel_data))
        except Exception as e:
            print(f"Error decoding frame {i}: {e}")
            continue
    
    return frames

def rebuild_shp_file(frames: List[Tuple[int, int, int, int, bytes]]) -> bytes:
    """Baut SHP-Datei aus Frame-Daten neu auf"""
    if not frames:
        return b''
    
    frame_count = len(frames)
    
    # Header: Frame count
    result = bytearray()
    result.extend(write_uint16(frame_count))
    
    # Frame entries (7 bytes each: 4 bytes offset + 1 byte format + 1 byte width + 1 byte height)
    header_size = 2 + frame_count * 7
    current_offset = header_size
    
    frame_entries = []
    encoded_frames = []
    
    # Enkodiere alle Frames
    for orig_offset, width, height, format_id, pixel_data in frames:
        try:
            encoded_data = encode_frame_format80(pixel_data, width, height)
            encoded_frames.append(encoded_data)
            
            frame_entries.append({
                'offset': current_offset,
                'format': format_id,
                'width': width,
                'height': height
            })
            
            current_offset += len(encoded_data)
            print(f"Encoded frame: {width}x{height}, {len(encoded_data)} bytes")
        except Exception as e:
            print(f"Error encoding frame {width}x{height}: {e}")
            return b''
    
    # Schreibe Frame-Einträge
    for entry in frame_entries:
        result.extend(write_uint32(entry['offset']))
        result.append(entry['format'])
        result.append(entry['width'])  
        result.append(entry['height'])
    
    # Schreibe Frame-Daten
    for encoded_data in encoded_frames:
        result.extend(encoded_data)
    
    return bytes(result)

def remap_colors(pixel_data: bytes, color_map: Dict[int, int]) -> Tuple[bytes, int]:
    """Ändert Farbindizes in Pixel-Daten"""
    result = bytearray(pixel_data)
    changes = 0
    
    for i in range(len(result)):
        if result[i] in color_map:
            result[i] = color_map[result[i]]
            changes += 1
    
    return bytes(result), changes

def process_shp_file(input_path: str, output_path: str, color_map: Dict[int, int]) -> bool:
    """Verarbeitet eine SHP-Datei und ändert Farbindizes"""
    print(f"Processing: {input_path}")
    print(f"Color mapping: {color_map}")
    
    # Backup erstellen
    backup_path = input_path + ".backup"
    if not os.path.exists(backup_path):
        print(f"Creating backup: {backup_path}")
        with open(input_path, 'rb') as src, open(backup_path, 'wb') as dst:
            dst.write(src.read())
    
    # Datei laden
    try:
        with open(input_path, 'rb') as f:
            data = f.read()
    except Exception as e:
        print(f"Error reading file: {e}")
        return False
    
    # SHP parsen
    frames = parse_shp_file(data)
    if not frames:
        print("No valid frames found")
        return False
    
    print(f"Found {len(frames)} frames")
    
    # Farbindizes in allen Frames ändern
    total_changes = 0
    modified_frames = []
    
    for orig_offset, width, height, format_id, pixel_data in frames:
        new_pixel_data, changes = remap_colors(pixel_data, color_map)
        total_changes += changes
        modified_frames.append((orig_offset, width, height, format_id, new_pixel_data))
    
    print(f"Total color indices changed: {total_changes}")
    
    if total_changes > 0:
        # SHP-Datei neu aufbauen
        try:
            new_data = rebuild_shp_file(modified_frames)
            if not new_data:
                print("Failed to rebuild SHP file")
                return False
            
            # Speichern
            with open(output_path, 'wb') as f:
                f.write(new_data)
            
            print(f"Successfully saved: {output_path}")
            return True
        except Exception as e:
            print(f"Error rebuilding/saving file: {e}")
            return False
    else:
        print("No changes needed")
        return True

def main():
    if len(sys.argv) < 4:
        print("Usage: python simple_shp_remap.py INPUT.SHP OUTPUT.SHP OLD_INDEX:NEW_INDEX [OLD_INDEX:NEW_INDEX ...]")
        print("Example: python simple_shp_remap.py parachute.shp parachute_new.shp 229:80 230:81 231:82")
        return 1
    
    input_path = sys.argv[1] 
    output_path = sys.argv[2]
    
    # Parse color mappings
    color_map = {}
    for arg in sys.argv[3:]:
        try:
            old_str, new_str = arg.split(':')
            old_index = int(old_str)
            new_index = int(new_str)
            color_map[old_index] = new_index
        except ValueError:
            print(f"Invalid mapping: {arg} (use OLD_INDEX:NEW_INDEX)")
            return 1
    
    if not color_map:
        print("No color mappings specified")
        return 1
    
    if not os.path.exists(input_path):
        print(f"Input file not found: {input_path}")
        return 1
    
    success = process_shp_file(input_path, output_path, color_map)
    return 0 if success else 1

if __name__ == "__main__":
    sys.exit(main())