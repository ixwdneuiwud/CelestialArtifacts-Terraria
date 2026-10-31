"""Restore unredistributed Minecraft inputs from your local game files.
Install Pillow with: python -m pip install -r tools/requirements.txt
"""
from pathlib import Path
import argparse
import hashlib
import io
import json
import zipfile
from PIL import Image

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('minecraft', type=Path, help='Minecraft 1.20.1 client JAR, or its extracted asset root')
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    manifest = json.loads((root / 'external-assets.json').read_text(encoding='utf-8'))
    archive = zipfile.ZipFile(args.minecraft) if args.minecraft.is_file() else None
    try:
        prepared = []
        for row in manifest:
            data = archive.read(row['entry']) if archive else (args.minecraft / row['entry']).read_bytes()
            image = Image.open(io.BytesIO(data)).convert('RGBA').resize(tuple(row['size']), Image.Resampling.NEAREST)
            if row.get('liftPalette'):
                image.putdata([(max(192,r),max(192,g),max(192,b),a) for r,g,b,a in image.getdata()])
            if hashlib.sha256(image.tobytes()).hexdigest() != row['rgbaSha256']:
                raise ValueError('Unexpected texture pixels; use Minecraft 1.20.1: ' + row['entry'])
            target = (root / row['path']).resolve()
            if not target.is_relative_to(root.resolve()):
                raise ValueError('Asset target leaves repository')
            prepared.append((target,image))
        for target,image in prepared:
            target.parent.mkdir(parents=True, exist_ok=True)
            image.save(target)
            print('Restored', target.relative_to(root))
    finally:
        if archive: archive.close()

if __name__ == '__main__': main()
