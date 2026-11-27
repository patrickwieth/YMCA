"""Turn an isolated Dune review package into the native combat/exit fixture.
Usage: python tools/prepare_factory_airship_probe.py INPUT_REVIEW.oramap OUTPUT.oramap
Never install the output in a normal map folder.
"""
import re
import sys
import zipfile
from pathlib import Path

if __name__=='__main__':
    source,destination=map(Path,sys.argv[1:])
    if destination.exists():raise SystemExit('Refusing to overwrite an existing package.')
    with zipfile.ZipFile(source) as archive:
        yaml=archive.read('map.yaml').decode()
        if '[REVIEW ONLY]' not in yaml or 'Dune Patrol' not in yaml:raise SystemExit('Expected an isolated Dune review package.')
        yaml,count=re.subn(r'([\t ]+)HandcraftedRuntimeProbe:\r?\n[\t ]+ExpectedOilGroundAccess: (?:true|false)',
            lambda m:m[1]+'FactoryAirshipProbe:',yaml)
        if count!=1:raise SystemExit('Expected exactly one movement probe to replace.')
        with zipfile.ZipFile(destination,'w',zipfile.ZIP_DEFLATED) as output:
            for name in archive.namelist():output.writestr(name,yaml.encode() if name=='map.yaml' else archive.read(name))
