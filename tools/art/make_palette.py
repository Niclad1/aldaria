"""Cria a paleta fixa do jogo (art/palette.gpl, 64 cores) a partir de toda a arte em baixa resolução."""
import sys
from PIL import Image

out, files = sys.argv[1], sys.argv[2:]
pixels = []
for f in files:
    im = Image.open(f).convert("RGBA")
    pixels.extend(p[:3] for p in im.getdata() if p[3] >= 110)
sheet = Image.new("RGB", (len(pixels), 1))
sheet.putdata(pixels)
pal = sheet.quantize(colors=63, method=Image.Quantize.MEDIANCUT).getpalette()[: 63 * 3]
with open(out, "w") as fh:
    fh.write("GIMP Palette\nName: Aldaria\nColumns: 8\n#\n")
    fh.write("  0   0   0\ttransparente\n")
    for i in range(63):
        r, g, b = pal[i * 3: i * 3 + 3]
        fh.write(f"{r:3d} {g:3d} {b:3d}\tcor{i + 1}\n")
print(f"paleta com 64 cores salva em {out}")
