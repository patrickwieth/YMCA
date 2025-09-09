#!/usr/bin/env python3
"""
SHP Format Analyzer
Analysiert die Struktur einer SHP-Datei
"""

import struct
import sys

def analyze_shp(file_path):
    with open(file_path, 'rb') as f:
        data = f.read()
    
    print(f"File size: {len(data)} bytes")
    print(f"First 32 bytes (hex): {data[:32].hex()}")
    print(f"First 32 bytes (ascii): {repr(data[:32])}")
    
    if len(data) < 2:
        return
    
    # Try different interpretations
    frame_count = struct.unpack('<H', data[0:2])[0]
    print(f"Possible frame count (little-endian): {frame_count}")
    
    frame_count_be = struct.unpack('>H', data[0:2])[0] 
    print(f"Possible frame count (big-endian): {frame_count_be}")
    
    # Show first few frame entries if they exist
    pos = 2
    print("\nFirst few frame entries (assuming TD format):")
    for i in range(min(10, frame_count if frame_count < 1000 else 10)):
        if pos + 6 > len(data):
            break
        offset = struct.unpack('<I', data[pos:pos+4])[0]
        format_id = data[pos+4]
        width = data[pos+5] 
        height = data[pos+6]
        print(f"Frame {i}: offset={offset:08x}, format={format_id:02x}, size={width}x{height}")
        pos += 7
    
    print(f"\nData at different positions:")
    for offset in [0, 100, 200, 500, 1000]:
        if offset + 16 <= len(data):
            chunk = data[offset:offset+16]
            print(f"Offset {offset:04x}: {chunk.hex()}")

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python analyze_shp.py FILE.SHP")
        sys.exit(1)
    
    analyze_shp(sys.argv[1])