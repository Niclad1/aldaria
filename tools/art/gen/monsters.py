"""Monstros: cada um desenhado à parte, com personalidade própria."""
import math
import random
from .lib import Svg, INK, shade, f
from .characters import cel, ell, rrect, eyes
from .props import cloud


def base(name, w=240, h=240, pivot_y=18):
    s = Svg(name, w, h, pivot=(w / 2, pivot_y), kind="char")
    ol = s.outline_filter(2.2)
    s.begin(f'filter="url(#{ol})"')
    return s, h - pivot_y


def lanudo(name="char_lanudo", scale=1.0, king=False):
    w, h = (320, 330) if king else (240, 220)
    s, g = base(name, w, h)
    rng = random.Random(3)
    k = scale
    cx = w / 2 - 6
    if king:
        d = f"M{cx - 60} {g - 150} Q{cx - 110} {g - 60} {cx - 96} {g - 4} Q{cx} {g + 6} {cx + 70} {g - 10} Q{cx + 40} {g - 100} {cx + 20} {g - 150} Z"
        cel(s, d, "#b8232f", (cx - 110, g - 150, cx + 70, g + 6))
        s.path(f"M{cx - 96} {g - 8} Q{cx - 10} {g + 4} {cx + 68} {g - 12}", "none", stroke="#f2f2f2", sw=10)
        s.path(f"M{cx - 96} {g - 8} Q{cx - 10} {g + 4} {cx + 68} {g - 12}", "none", stroke=INK, sw=1.5, extra='stroke-dasharray="3 9"')
    for lx in (-44, -20, 18, 40):
        s.limb([(cx + lx * k, g - 40 * k), (cx + lx * k, g - 6)], 13 * k, "#3a2e2a")
        s.ellipse(cx + lx * k + 2, g - 4, 9 * k, 5 * k, "#1f1715", stroke=INK, sw=2)
    body = cloud(cx - 8 * k, g - 78 * k, 70 * k, 14, rng, squash=0.72, jitter=0.06)
    cel(s, body, "#f6f1e6", (cx - 78 * k, g - 130 * k, cx + 62 * k, g - 26 * k), shadow=0.12, highlight=0.1)
    for _ in range(9):
        x, y = cx + rng.uniform(-60, 40) * k, g + rng.uniform(-115, -45) * k
        s.path(f"M{f(x)} {f(y)} q6 -8 12 0", "none", stroke="#d8cfbf", sw=2.5)
    # cabeça
    fx, fy = cx + 58 * k, g - 72 * k
    for sx in (-1, 1):
        hx0 = fx - 6 * k + sx * 18 * k
        s.path(f"M{f(hx0)} {f(fy - 26 * k)} Q{f(hx0 + sx * 34 * k)} {f(fy - 44 * k)} {f(hx0 + sx * 26 * k)} {f(fy - 6 * k)} Q{f(hx0 + sx * 16 * k)} {f(fy - 20 * k)} {f(hx0 + sx * 12 * k)} {f(fy - 14 * k)}",
               "none", stroke=INK, sw=15 * k)
        s.path(f"M{f(hx0)} {f(fy - 26 * k)} Q{f(hx0 + sx * 34 * k)} {f(fy - 44 * k)} {f(hx0 + sx * 26 * k)} {f(fy - 6 * k)} Q{f(hx0 + sx * 16 * k)} {f(fy - 20 * k)} {f(hx0 + sx * 12 * k)} {f(fy - 14 * k)}",
               "none", stroke="#c9985a", sw=9 * k)
    d, bb = ell(fx, fy, 30 * k, 34 * k)
    cel(s, d, "#4d3a33", bb, shadow=0.12)
    s.add(f'<g transform="translate({f(fx)},{f(fy)}) scale({k}) translate({f(-fx)},{f(-fy)})">')
    eyes(s, fx - 10, fx + 10, fy - 4, rx=7, ry=9, angry=king)
    s.ellipse(fx + 2, fy + 18, 8, 5, "#6b5048", stroke=INK, sw=1.8)
    s.add("</g>")
    d = cloud(fx - 8 * k, fy - 34 * k, 20 * k, 7, rng, squash=0.7)
    s.path(d, "#fbf8f1", stroke=INK, sw=2.5)
    if king:
        cx2, cy2 = fx - 6, fy - 70
        d = f"M{cx2 - 30} {cy2 + 20} L{cx2 - 34} {cy2 - 14} L{cx2 - 16} {cy2 + 2} L{cx2} {cy2 - 22} L{cx2 + 16} {cy2 + 2} L{cx2 + 34} {cy2 - 14} L{cx2 + 30} {cy2 + 20} Z"
        cel(s, d, "#f2c230", (cx2 - 34, cy2 - 22, cx2 + 34, cy2 + 20))
        for gx, col in ((cx2 - 16, "#e8453c"), (cx2, "#3b82f6"), (cx2 + 16, "#e8453c")):
            s.circle(gx, cy2 + 10, 4.5, col, stroke=INK, sw=1.5)
    s.end()
    return s


def pipio():
    s, g = base("char_pipio", 200, 200)
    cx = 96
    for fx in (-14, 12):
        s.path(f"M{cx + fx} {g - 18} L{cx + fx} {g - 4} M{cx + fx - 8} {g - 2} L{cx + fx + 8} {g - 2}", "none", stroke=INK, sw=7)
        s.path(f"M{cx + fx} {g - 18} L{cx + fx} {g - 4} M{cx + fx - 8} {g - 2} L{cx + fx + 8} {g - 2}", "none", stroke="#ff8a3d", sw=3.5)
    for k, (tx, rot) in enumerate(((cx - 12, -25), (cx, 0), (cx + 12, 25))):
        s.ellipse(tx, g - 124, 7, 17, "#f7b500", stroke=INK, sw=2.5, extra=f'transform="rotate({rot} {tx} {g - 110})"')
    d, bb = ell(cx, g - 62, 54, 52)
    cel(s, d, "#ffd43b", bb, shadow=0.14, highlight=0.12)
    d, bb = ell(cx + 10, g - 42, 30, 22)
    s.path(d, "#fff4b8", stroke=None, extra='opacity="0.9"')
    d = f"M{cx - 50} {g - 70} Q{cx - 76} {g - 58} {cx - 62} {g - 36} Q{cx - 46} {g - 44} {cx - 40} {g - 56} Z"
    cel(s, d, "#f2b705", (cx - 76, g - 70, cx - 40, g - 36))
    s.path(f"M{cx + 40} {g - 70} L{cx + 72} {g - 62} L{cx + 40} {g - 50} Z", "#ff8a3d", stroke=INK, sw=2.5)
    s.path(f"M{cx + 42} {g - 60} L{cx + 66} {g - 62}", "none", stroke=INK, sw=1.5)
    eyes(s, cx + 6, cx + 30, g - 80, rx=9, ry=12)
    s.ellipse(cx + 2, g - 58, 7, 4, "#ff9a7a", stroke=None, extra='opacity="0.6"')
    s.end()
    return s


def cogumelo():
    s, g = base("char_cogumelo", 220, 230)
    cx = 110
    for fx in (-20, 20):
        s.ellipse(cx + fx, g - 6, 16, 9, "#8a6a4a", stroke=INK, sw=2.5)
    d = f"M{cx - 32} {g - 8} Q{cx - 40} {g - 60} {cx - 26} {g - 90} L{cx + 26} {g - 90} Q{cx + 40} {g - 60} {cx + 32} {g - 8} Q{cx} {g} {cx - 32} {g - 8} Z"
    cel(s, d, "#f2e3c4", (cx - 40, g - 90, cx + 40, g))
    for sx in (-1, 1):
        s.limb([(cx + sx * 30, g - 52), (cx + sx * 50, g - 36)], 10, "#e6d2ad")
    eyes(s, cx - 6, cx + 20, g - 56, rx=7.5, ry=9.5, angry=True)
    s.path(f"M{cx - 2} {g - 30} Q{cx + 8} {g - 38} {cx + 18} {g - 30}", "none", stroke=INK, sw=3)
    d = f"M{cx - 88} {g - 88} Q{cx - 92} {g - 180} {cx} {g - 186} Q{cx + 92} {g - 180} {cx + 88} {g - 88} Q{cx} {g - 70} {cx - 88} {g - 88} Z"
    cel(s, d, "#d93a2f", (cx - 92, g - 186, cx + 92, g - 70), shadow=0.14, highlight=0.12)
    s.path(f"M{cx - 84} {g - 90} Q{cx} {g - 72} {cx + 84} {g - 90} Q{cx} {g - 84} {cx - 84} {g - 90} Z", "#caa98a", stroke=INK, sw=2)
    cid = s.clip(f'<path d="{d}"/>')
    s.add(f'<g clip-path="url(#{cid})">' + "".join(
        f'<ellipse cx="{f(x)}" cy="{f(y)}" rx="{f(r)}" ry="{f(r * 0.8)}" fill="#fff8ec" stroke="{INK}" stroke-width="2"/>'
        for x, y, r in ((cx - 50, g - 130, 14), (cx + 2, g - 160, 16), (cx + 52, g - 124, 13), (cx - 14, g - 110, 9), (cx + 34, g - 100, 7), (cx - 70, g - 100, 7))) + "</g>")
    s.end()
    return s


def javali():
    s, g = base("char_javali", 260, 220)
    cx = 124
    for lx in (-50, -26, 26, 50):
        s.limb([(cx + lx, g - 40), (cx + lx, g - 8)], 14, "#5a3a26")
        s.path(f"M{cx + lx - 8} {g - 8} L{cx + lx + 8} {g - 8} L{cx + lx + 6} {g} L{cx + lx - 6} {g} Z", "#2b1a10", stroke=INK, sw=2)
    body = f"M{cx - 80} {g - 50} Q{cx - 92} {g - 110} {cx - 30} {g - 122} Q{cx + 40} {g - 130} {cx + 70} {g - 90} Q{cx + 80} {g - 40} {cx + 30} {g - 34} Q{cx - 40} {g - 28} {cx - 80} {g - 50} Z"
    cel(s, body, "#8a5a36", (cx - 92, g - 130, cx + 80, g - 28))
    # crina espetada
    spikes = " ".join(f"L{f(cx - 60 + i * 12)} {f(g - 126 - (10 if i % 2 == 0 else 0) + abs(i - 6) * 1.5)}" for i in range(13))
    s.path(f"M{cx - 66} {g - 112} {spikes} L{cx + 90} {g - 112} Q{cx} {g - 116} {cx - 66} {g - 112} Z", "#4a2e1c", stroke=INK, sw=2.5)
    s.path(f"M{cx - 84} {g - 72} q-16 -6 -12 -20", "none", stroke=INK, sw=4)
    d, bb = ell(cx + 76, g - 80, 36, 32)
    cel(s, d, "#94603a", bb)
    s.path(f"M{cx + 58} {g - 110} L{cx + 50} {g - 136} L{cx + 74} {g - 112} Z", "#6b4226", stroke=INK, sw=2.5)
    d, bb = ell(cx + 108, g - 70, 16, 13)
    cel(s, d, "#e8a0a0", bb, shadow=0.1)
    s.circle(cx + 104, g - 70, 3, INK, stroke=None)
    s.circle(cx + 114, g - 70, 3, INK, stroke=None)
    for tx in (cx + 92, cx + 116):
        s.path(f"M{tx} {g - 58} Q{tx + 4} {g - 76} {tx + 14} {g - 84} Q{tx + 6} {g - 64} {tx + 6} {g - 56} Z", "#fffaf0", stroke=INK, sw=2.2)
    eyes(s, cx + 70, cx + 90, g - 92, rx=6, ry=7.5, angry=True)
    s.end()
    return s


def lobo():
    s, g = base("char_lobo", 250, 240)
    cx = 118
    tail = f"M{cx - 64} {g - 70} Q{cx - 120} {g - 90} {cx - 104} {g - 140} Q{cx - 80} {g - 104} {cx - 50} {g - 90} Z"
    cel(s, tail, "#8a8f98", (cx - 120, g - 140, cx - 50, g - 70))
    for lx in (-44, -22, 24, 44):
        s.limb([(cx + lx, g - 44), (cx + lx, g - 8)], 13, "#6b7078")
        s.ellipse(cx + lx + 3, g - 5, 9, 5, "#4a4e56", stroke=INK, sw=2)
    body = f"M{cx - 70} {g - 60} Q{cx - 74} {g - 110} {cx - 20} {g - 112} Q{cx + 40} {g - 116} {cx + 56} {g - 80} Q{cx + 60} {g - 40} {cx + 20} {g - 36} Q{cx - 40} {g - 32} {cx - 70} {g - 60} Z"
    cel(s, body, "#9aa0a8", (cx - 74, g - 116, cx + 60, g - 32))
    s.path(f"M{cx - 10} {g - 40} Q{cx + 20} {g - 60} {cx + 40} {g - 44}", "none", stroke="#e8eaee", sw=8)
    hx, hy = cx + 62, g - 120
    for ex in (-18, 14):
        s.path(f"M{hx + ex - 12} {hy - 22} L{hx + ex - 4} {hy - 60} L{hx + ex + 12} {hy - 22} Z", "#7a8088", stroke=INK, sw=2.5)
        s.path(f"M{hx + ex - 5} {hy - 26} L{hx + ex - 2} {hy - 48} L{hx + ex + 5} {hy - 26} Z", "#e8a0a0", stroke=None)
    d, bb = ell(hx, hy, 42, 38)
    cel(s, d, "#a8aeb6", bb)
    d = f"M{hx + 10} {hy - 2} Q{hx + 60} {hy - 4} {hx + 66} {hy + 14} Q{hx + 50} {hy + 30} {hx + 10} {hy + 26} Z"
    cel(s, d, "#c8ccd2", (hx + 10, hy - 4, hx + 66, hy + 30), shadow=0.1)
    s.ellipse(hx + 64, hy + 8, 7, 6, INK, stroke=None)
    s.path(f"M{hx + 30} {hy + 24} L{hx + 34} {hy + 34} L{hx + 40} {hy + 24}", "#ffffff", stroke=INK, sw=1.5)
    eyes(s, hx - 6, hx + 18, hy - 8, rx=7, ry=8.5, iris="#e8b82a", angry=True)
    s.end()
    return s


def sapo():
    s, g = base("char_sapo", 230, 200)
    cx = 114
    for sx in (-1, 1):
        s.path(f"M{cx + sx * 40} {g - 40} Q{cx + sx * 80} {g - 50} {cx + sx * 76} {g - 10} L{cx + sx * 50} {g - 6}", "none", stroke=INK, sw=20)
        s.path(f"M{cx + sx * 40} {g - 40} Q{cx + sx * 80} {g - 50} {cx + sx * 76} {g - 10} L{cx + sx * 50} {g - 6}", "none", stroke="#5aa03a", sw=14)
    d, bb = ell(cx, g - 50, 66, 46)
    cel(s, d, "#6ab845", bb)
    d, bb = ell(cx + 6, g - 32, 42, 22)
    s.path(d, "#d8ec9a", stroke=None, extra='opacity="0.95"')
    for x, y, r in ((cx - 40, g - 70, 7), (cx - 20, g - 84, 5), (cx + 36, g - 76, 6), (cx - 50, g - 46, 4)):
        s.circle(x, y, r, "#9a4ad0", stroke=INK, sw=1.5)
    for ex in (cx - 26, cx + 26):
        d, bb = ell(ex, g - 92, 22, 20)
        cel(s, d, "#6ab845", bb, shadow=0.1)
    eyes(s, cx - 26, cx + 26, g - 94, rx=12, ry=13, iris="#e8b82a")
    s.path(f"M{cx - 40} {g - 58} Q{cx} {g - 36} {cx + 44} {g - 60}", "none", stroke=INK, sw=3.5)
    s.end()
    return s


def morcego():
    s, g = base("char_morcego", 280, 240, pivot_y=18)
    cx, cy = 140, g - 110
    bl = s.soft_filter(5)
    s.ellipse(cx, g - 4, 40, 10, "#000000", stroke=None, extra=f'opacity="0.25" filter="url(#{bl})"')
    for sx in (-1, 1):
        d = (f"M{cx + sx * 20} {cy - 6} Q{cx + sx * 70} {cy - 60} {cx + sx * 124} {cy - 30} "
             f"Q{cx + sx * 108} {cy - 8} {cx + sx * 110} {cy + 20} Q{cx + sx * 92} {cy + 4} {cx + sx * 80} {cy + 26} "
             f"Q{cx + sx * 66} {cy + 8} {cx + sx * 52} {cy + 28} Q{cx + sx * 40} {cy + 10} {cx + sx * 20} {cy + 16} Z")
        cel(s, d, "#5a3a7a", (cx - 124, cy - 60, cx + 124, cy + 28) if sx < 0 else (cx, cy - 60, cx + 124, cy + 28), shadow=0.12)
        s.path(f"M{cx + sx * 24} {cy} L{cx + sx * 100} {cy - 24} M{cx + sx * 40} {cy + 4} L{cx + sx * 80} {cy + 20}", "none", stroke=shade("#5a3a7a", -0.25), sw=2)
    for ex in (-18, 18):
        s.path(f"M{cx + ex - 10} {cy - 30} L{cx + ex} {cy - 60} L{cx + ex + 10} {cy - 30} Z", "#6b4a8c", stroke=INK, sw=2.5)
    d, bb = ell(cx, cy, 36, 36)
    cel(s, d, "#6b4a8c", bb)
    eyes(s, cx - 10, cx + 12, cy - 6, rx=7.5, ry=9, iris="#e8453c")
    s.path(f"M{cx - 8} {cy + 16} L{cx - 4} {cy + 25} L{cx} {cy + 16} M{cx + 4} {cy + 16} L{cx + 8} {cy + 25} L{cx + 12} {cy + 16}", "#ffffff", stroke=INK, sw=1.5)
    s.end()
    return s


def esqueleto():
    s, g = base("char_esqueleto", 220, 300)
    cx = 110
    bone = "#efe8d6"
    s.path("M52 110 Q10 190 58 272", "none", stroke=INK, sw=11)
    s.path("M52 110 Q10 190 58 272", "none", stroke="#5a4a3a", sw=6)
    s.path("M52 110 L58 272", "none", stroke="#d8d0c0", sw=1.5)
    s.limb([(90, 168), (72, 200)], 7, bone, highlight=False)
    s.circle(70, 204, 8, bone, stroke=INK, sw=2.5)
    for lx in (96, 124):
        s.limb([(lx, 214), (lx, g - 10)], 8, bone, highlight=False)
        s.ellipse(lx + 5, g - 6, 12, 6, bone, stroke=INK, sw=2.5)
    d = "M82 214 Q110 226 138 214 L132 234 Q110 244 88 234 Z"
    cel(s, d, "#6b4a3a", (82, 214, 138, 244), shadow=0.1)
    s.path("M110 150 L110 214", "none", stroke=INK, sw=9)
    s.path("M110 150 L110 214", "none", stroke=bone, sw=5)
    for k in range(4):
        y = 160 + k * 12
        s.path(f"M{90 + k * 2} {y} Q110 {y + 8} {130 - k * 2} {y}", "none", stroke=INK, sw=8)
        s.path(f"M{90 + k * 2} {y} Q110 {y + 8} {130 - k * 2} {y}", "none", stroke=bone, sw=4)
    d, bb = ell(108, 106, 50, 46)
    cel(s, d, bone, bb, shadow=0.12)
    d = "M92 138 Q108 158 128 140 L126 150 Q108 164 94 150 Z"
    s.path(d, bone, stroke=INK, sw=2.5)
    for x in (100, 108, 116, 124):
        s.path(f"M{x} 142 L{x} 152", "none", stroke=INK, sw=1.5)
    for ex, rx in ((96, 12), (126, 10)):
        s.ellipse(ex, 104, rx, 13, "#2b1a10", stroke=INK, sw=2)
        s.circle(ex + 2, 104, 3.5, "#6ae8ff", stroke=None)
    s.path("M110 118 L106 128 L114 128 Z", "#2b1a10", stroke=None)
    s.path("M62 90 Q108 44 156 88 Q140 70 110 68 Q80 70 62 90 Z", "#5a4a6a", stroke=INK, sw=2.5)
    s.limb([(130, 168), (146, 190), (150, 200)], 7, bone, highlight=False)
    s.circle(150, 202, 8, bone, stroke=INK, sw=2.5)
    s.end()
    return s


def golem():
    s, g = base("char_golem", 280, 300)
    cx = 140
    stone = "#8f8a82"
    for lx in (-34, 30):
        d, bb = rrect(cx + lx - 22, g - 70, 44, 66, 12)
        cel(s, d, shade(stone, -0.08), bb)
    d = f"M{cx - 80} {g - 80} L{cx - 90} {g - 180} L{cx - 40} {g - 220} L{cx + 50} {g - 216} L{cx + 92} {g - 170} L{cx + 80} {g - 76} Q{cx} {g - 56} {cx - 80} {g - 80} Z"
    cel(s, d, stone, (cx - 90, g - 220, cx + 92, g - 56))
    s.path(f"M{cx - 40} {g - 180} L{cx - 20} {g - 150} L{cx - 36} {g - 120} M{cx + 30} {g - 110} L{cx + 50} {g - 90}", "none", stroke=shade(stone, -0.35), sw=3)
    s.path(f"M{cx - 10} {g - 140} L{cx + 10} {g - 120} L{cx - 4} {g - 100}", "none", stroke="#6ae8ff", sw=5)
    s.path(f"M{cx - 10} {g - 140} L{cx + 10} {g - 120} L{cx - 4} {g - 100}", "none", stroke="#dffaff", sw=2)
    d, bb = rrect(cx - 44, g - 270, 88, 64, 18)
    cel(s, d, shade(stone, 0.04), bb)
    for ex in (cx - 16, cx + 18):
        s.path(f"M{ex - 10} {g - 244} L{ex + 10} {g - 240} L{ex + 8} {g - 232} L{ex - 8} {g - 234} Z", "#6ae8ff", stroke=INK, sw=2)
    s.circle(cx, g - 240, 30, "#6ae8ff", stroke=None, extra='opacity="0.12"')
    for sx in (-1, 1):
        d, bb = rrect(cx + sx * 100 - 26, g - 190, 52, 110, 18)
        cel(s, d, shade(stone, -0.04), bb)
        d, bb = ell(cx + sx * 100, g - 74, 34, 26)
        cel(s, d, shade(stone, -0.1), bb)
    for x, y in ((cx - 70, g - 200), (cx + 60, g - 206), (cx - 20, g - 268)):
        s.path(cloud(x, y, 14, 6, random.Random(int(x)), squash=0.6), "#7fae45", stroke=INK, sw=2)
    s.end()
    return s


def build():
    return [lanudo(), pipio(), cogumelo(), javali(), lobo(), sapo(), morcego(), esqueleto(), golem(),
            lanudo("char_rei_lanudo", scale=1.45, king=True)]
