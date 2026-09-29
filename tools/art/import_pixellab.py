"""
Registra no manifesto os sprites em pixel art gerados no PixelLab (art/pixellab/*.png).
Calcula o pivô (pés do personagem) pelos pixels visíveis. Depois rode `node render.mjs`.
Uso: python3 import_pixellab.py
"""
import glob
import json
import os
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "art", "pixellab")
MANIFEST = os.path.join(ROOT, "art", "src", "manifest.json")
PPU = 54  # pixels por unidade: um personagem de ~60 px fica com ~1,1 célula de altura

manifest = {m["name"]: m for m in json.load(open(MANIFEST))["sprites"]}
for path in sorted(glob.glob(os.path.join(SRC, "*.png"))):
    name = os.path.splitext(os.path.basename(path))[0]
    im = Image.open(path).convert("RGBA")
    box = im.getchannel("A").point(lambda a: 255 if a > 20 else 0).getbbox()
    if not box:
        continue
    x0, y0, x1, y1 = box
    manifest[name] = {
        "name": name, "file": None, "png": os.path.basename(path), "width": im.width, "height": im.height,
        "pivotX": round((x0 + x1) / 2), "pivotY": im.height - y1 + 1, "ppu": PPU, "kind": "char", "filter": "point", "group": "pixellab",
    }
    print(f"{name}: {im.width}x{im.height}, pés em y={y1}")
json.dump({"sprites": sorted(manifest.values(), key=lambda m: m["name"])}, open(MANIFEST, "w"), indent=1, ensure_ascii=False)
