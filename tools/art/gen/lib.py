"""Ferramentas para escrever SVG no estilo do Aldaria: contorno escuro, gradientes suaves e brilhos."""
import colorsys
import math
import random

INK = "#2b1a10"
PPU = 200  # pixels por unidade do mundo (1 célula = 200 px de largura)


def hex_to_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))


def rgb_to_hex(r, g, b):
    return "#%02x%02x%02x" % tuple(max(0, min(255, round(v * 255))) for v in (r, g, b))


def shade(color, amount):
    """amount > 0 clareia, < 0 escurece (em luminosidade HLS)."""
    r, g, b = hex_to_rgb(color)
    h, l, s = colorsys.rgb_to_hls(r, g, b)
    l = max(0, min(1, l + amount))
    return rgb_to_hex(*colorsys.hls_to_rgb(h, l, s))


def mix(a, b, t):
    ra, ga, ba = hex_to_rgb(a)
    rb, gb, bb = hex_to_rgb(b)
    return rgb_to_hex(ra + (rb - ra) * t, ga + (gb - ga) * t, ba + (bb - ba) * t)


def f(v):
    """Formata números de forma compacta."""
    return ("%.1f" % v).rstrip("0").rstrip(".")


class Svg:
    def __init__(self, name, width, height, pivot=None, ppu=PPU, border=None, kind="sprite"):
        self.name = name
        self.w = width
        self.h = height
        # pivot em pixels medido a partir do canto INFERIOR esquerdo (convenção da Unity)
        self.pivot = pivot if pivot is not None else (width / 2, 0)
        self.ppu = ppu
        self.border = border
        self.kind = kind
        self.defs = []
        self.body = []
        self._uid = 0

    def uid(self, prefix="g"):
        self._uid += 1
        return f"{prefix}{self._uid}"

    # ------------------------------------------------------------------ defs
    def lin(self, c1, c2, x1=0, y1=0, x2=0, y2=1, stops=None):
        gid = self.uid("lg")
        st = stops or [(0, c1), (1, c2)]
        s = "".join(f'<stop offset="{o}" stop-color="{c}"/>' for o, c in st)
        self.defs.append(f'<linearGradient id="{gid}" x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}">{s}</linearGradient>')
        return f"url(#{gid})"

    def rad(self, c1, c2, cx=0.35, cy=0.3, r=0.85, stops=None):
        gid = self.uid("rg")
        st = stops or [(0, c1), (1, c2)]
        s = "".join(f'<stop offset="{o}" stop-color="{c}"/>' for o, c in st)
        self.defs.append(f'<radialGradient id="{gid}" cx="{cx}" cy="{cy}" r="{r}">{s}</radialGradient>')
        return f"url(#{gid})"

    def clip(self, shape_svg):
        cid = self.uid("cp")
        self.defs.append(f'<clipPath id="{cid}">{shape_svg}</clipPath>')
        return cid

    def outline_filter(self, radius=3.0, color=INK):
        fid = self.uid("ol")
        self.defs.append(
            f'<filter id="{fid}" x="-20%" y="-20%" width="140%" height="140%">'
            f'<feMorphology in="SourceAlpha" operator="dilate" radius="{radius}" result="d"/>'
            f'<feFlood flood-color="{color}"/><feComposite in2="d" operator="in" result="o"/>'
            f'<feMerge><feMergeNode in="o"/><feMergeNode in="SourceGraphic"/></feMerge></filter>')
        return fid

    def noise_filter(self, freq=0.05, octaves=3, seed=1, amount=0.18, color="#000000"):
        """Textura de ruído (para grama, terra, pedra) aplicada por cima da forma."""
        fid = self.uid("nz")
        self.defs.append(
            f'<filter id="{fid}" x="0" y="0" width="100%" height="100%">'
            f'<feTurbulence type="fractalNoise" baseFrequency="{freq}" numOctaves="{octaves}" seed="{seed}" result="t"/>'
            f'<feColorMatrix in="t" type="matrix" values="0 0 0 0 {hex_to_rgb(color)[0]:.3f} 0 0 0 0 {hex_to_rgb(color)[1]:.3f} 0 0 0 0 {hex_to_rgb(color)[2]:.3f} 0 0 0 {amount * 4:.3f} {-amount * 1.6:.3f}" result="n"/>'
            f'<feComposite in="n" in2="SourceAlpha" operator="in"/></filter>')
        return fid

    def soft_filter(self, blur=4):
        fid = self.uid("bl")
        self.defs.append(f'<filter id="{fid}" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="{blur}"/></filter>')
        return fid

    # ------------------------------------------------------------------ desenho
    def add(self, s):
        self.body.append(s)

    def begin(self, attrs=""):
        self.body.append(f"<g {attrs}>")

    def end(self):
        self.body.append("</g>")

    def path(self, d, fill, stroke=INK, sw=2.5, extra=""):
        st = f' stroke="{stroke}" stroke-width="{f(sw)}" stroke-linejoin="round" stroke-linecap="round"' if stroke else ""
        self.add(f'<path d="{d}" fill="{fill}"{st} {extra}/>')

    def ellipse(self, cx, cy, rx, ry, fill, stroke=INK, sw=2.5, extra=""):
        st = f' stroke="{stroke}" stroke-width="{f(sw)}"' if stroke else ""
        self.add(f'<ellipse cx="{f(cx)}" cy="{f(cy)}" rx="{f(rx)}" ry="{f(ry)}" fill="{fill}"{st} {extra}/>')

    def circle(self, cx, cy, r, fill, stroke=INK, sw=2.5, extra=""):
        self.ellipse(cx, cy, r, r, fill, stroke, sw, extra)

    def limb(self, pts, width, color, outline=INK, ow=2.5, highlight=True):
        """Membro arredondado (braço, perna, cabo): linha grossa com contorno."""
        d = "M" + " L".join(f"{f(x)} {f(y)}" for x, y in pts)
        self.add(f'<path d="{d}" fill="none" stroke="{outline}" stroke-width="{f(width + ow * 2)}" stroke-linecap="round" stroke-linejoin="round"/>')
        self.add(f'<path d="{d}" fill="none" stroke="{color}" stroke-width="{f(width)}" stroke-linecap="round" stroke-linejoin="round"/>')
        if highlight and width > 6:
            self.add(f'<path d="{d}" fill="none" stroke="#ffffff" stroke-opacity="0.22" stroke-width="{f(width * 0.3)}" stroke-linecap="round" transform="translate({f(-width * 0.18)},{f(-width * 0.12)})"/>')

    def shaded(self, shape, base, light=0.12, dark=-0.16, stroke=INK, sw=2.5, gloss=True, angle="diag"):
        """Preenche uma forma com gradiente claro→escuro, contorno e um brilho recortado dentro dela."""
        if angle == "diag":
            fill = self.lin(shade(base, light), shade(base, dark), 0.15, 0.05, 0.85, 0.95)
        else:
            fill = self.lin(shade(base, light), shade(base, dark))
        shape_fill = shape.replace("FILL", fill)
        if stroke:
            shape_fill = shape_fill.replace("/>", f' stroke="{stroke}" stroke-width="{f(sw)}" stroke-linejoin="round"/>', 1)
        self.add(shape_fill)
        if gloss:
            cid = self.clip(shape.replace("FILL", "#fff"))
            self.add(f'<g clip-path="url(#{cid})">{gloss if isinstance(gloss, str) else ""}</g>')
        return shape

    def to_string(self):
        return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{self.w}" height="{self.h}" viewBox="0 0 {self.w} {self.h}">'
                f'<defs>{"".join(self.defs)}</defs>{"".join(self.body)}</svg>')

    def meta(self):
        m = {"name": self.name, "file": self.name + ".svg", "width": self.w, "height": self.h,
             "pivotX": self.pivot[0], "pivotY": self.pivot[1], "ppu": self.ppu, "kind": self.kind}
        if self.border:
            m["border"] = self.border
        return m


def blade_tuft(rng, x, y, h, color, n=3, spread=5, sw=2.2):
    """Tufo de grama: algumas folhas curvas."""
    parts = []
    for i in range(n):
        dx = (i - (n - 1) / 2) * spread + rng.uniform(-1.5, 1.5)
        bend = rng.uniform(-5, 5) + dx * 0.6
        hh = h * rng.uniform(0.7, 1.1)
        parts.append(f'<path d="M{f(x + dx * 0.4)} {f(y)} Q{f(x + dx + bend * 0.3)} {f(y - hh * 0.6)} {f(x + dx + bend)} {f(y - hh)}" '
                     f'fill="none" stroke="{color}" stroke-width="{f(sw)}" stroke-linecap="round"/>')
    return "".join(parts)


def diamond(cx, cy, w, h, grow=0.0):
    hw, hh = w / 2 + grow, h / 2 + grow / 2
    return f"M{f(cx - hw)} {f(cy)} L{f(cx)} {f(cy - hh)} L{f(cx + hw)} {f(cy)} L{f(cx)} {f(cy + hh)} Z"


def inside_diamond(px, py, cx, cy, w, h, margin=0.0):
    return abs(px - cx) / (w / 2) + abs(py - cy) / (h / 2) <= 1 - margin
