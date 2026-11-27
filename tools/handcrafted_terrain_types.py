"""Unused-by-default legacy movement classes for faithful handcrafted map overrides."""
from pathlib import Path

TYPES = {'Rock': '777064', 'Rough': '66543D', 'Beach': 'B09C78', 'River': '5C74A4', 'Tree': '1C4024'}


def add_types(text):
    for name, color in TYPES.items():
        if '\n\t\tType: '+name+'\n' in text:
            continue
        if '\nTemplates:' not in text:
            raise ValueError('Missing Templates section')
        block = '\n\tTerrainType@Handcrafted'+name+':\n\t\tType: '+name+'\n\t\tTargetTypes: Ground\n\t\tColor: '+color+'\n'
        text = text.replace('\nTemplates:', block+'\nTemplates:', 1)
    return text


if __name__ == '__main__':
    root = Path(__file__).resolve().parents[1]
    for name in ['rubberduck-temperate.yaml', 'rubberduck-temperate-shaded.yaml']:
        p = root/'mods/ca/tilesets'/name
        p.write_text(add_types(p.read_text(encoding='utf-8')),encoding='utf-8',newline='\n')
