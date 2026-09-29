"""Mais cenário para o mapa não ficar repetitivo: espécies de árvore, objetos de vila, detalhes de chão e casas variadas."""
import math
import random
import zlib
from .lib import Svg, INK, shade, mix, f
from .props import cloud, foliage, trunk, ground_shadow, iso_pt, poly, branches
from .tiles import BIOMES


def _rng(name):
    return random.Random(zlib.crc32(name.encode()))


# ---------------------------------------------------------------------- árvores

def tree_birch(name):
    rng = _rng(name)
    W, H = 220, 370
    s = Svg(name, W, H, pivot=(110, 24), kind="prop")
    base = H - 24
    ground_shadow(s, 110, base, 55, 16)
    bark = "#ece6da"
    for dx, top, lean in ((-12, base - 200, -14), (14, base - 230, 10)):
        d = f"M{110 + dx - 8} {base} L{110 + dx + lean - 5} {top} L{110 + dx + lean + 5} {top} L{110 + dx + 8} {base} Z"
        s.path(d, s.lin(bark, shade(bark, -0.2), 0, 0, 1, 0), stroke=INK, sw=2.6)
        for k in range(6):
            t = rng.uniform(0.1, 0.9)
            x = 110 + dx + lean * t
            y = base - (base - top) * t
            s.path(f"M{f(x - 6)} {f(y)} l{f(rng.uniform(5, 9))} {f(rng.uniform(-2, 2))}", "none", stroke="#3a3530", sw=2.4)
    pal = ("#5c7a2c", "#8fae3e", "#cfe07a")
    cy = base - 230
    clumps = [(96, cy + 30, 40), (134, cy + 16, 44), (110, cy - 26, 42), (78, cy - 6, 30), (146, cy - 36, 30), (118, cy + 52, 30)]
    foliage(s, clumps, pal, rng)
    return s


def tree_cypress(name, pal):
    rng = _rng(name)
    W, H = 170, 420
    s = Svg(name, W, H, pivot=(85, 24), kind="prop")
    base = H - 24
    ground_shadow(s, 85, base, 42, 13)
    trunk(s, 85, base, base - 60, 9, "#5e3d24", rng)
    clumps = []
    y = base - 70
    r = 44
    while r > 12:
        clumps.append((85 + rng.uniform(-6, 6), y, r))
        y -= r * 1.05
        r *= 0.86
    foliage(s, clumps[::-1], pal, rng)
    return s


def tree_oak_big(name, pal, dots=None):
    rng = _rng(name)
    W, H = 340, 420
    s = Svg(name, W, H, pivot=(170, 24), kind="prop")
    base = H - 24
    ground_shadow(s, 170, base, 105, 26)
    trunk(s, 170, base, base - 170, 22, "#6b4226", rng)
    for sx in (-1, 1):
        s.limb([(170 + sx * 6, base - 140), (170 + sx * 60, base - 200)], 12, "#6b4226", ow=2.5, highlight=False)
    cy = base - 240
    clumps = [(170, cy, 92), (86, cy + 36, 58), (256, cy + 30, 60), (110, cy - 52, 62), (228, cy - 54, 60),
              (170, cy - 100, 54), (130, cy + 62, 46), (214, cy + 64, 44), (60, cy - 10, 40), (284, cy - 12, 40)]
    foliage(s, clumps, pal, rng, dots=dots)
    return s


def tree_young(name, pal, dots=None):
    rng = _rng(name)
    W, H = 180, 260
    s = Svg(name, W, H, pivot=(90, 20), kind="prop")
    base = H - 20
    ground_shadow(s, 90, base, 40, 12)
    trunk(s, 90, base, base - 110, 7, "#7a4a28", rng)
    cy = base - 140
    foliage(s, [(90, cy, 46), (60, cy + 18, 28), (120, cy + 16, 30), (80, cy - 34, 30), (104, cy - 38, 28)], pal, rng, dots=dots)
    return s


def tree_spruce(name, pal):
    """Pinheiro alto e fino (mata)."""
    rng = _rng(name)
    W, H = 200, 460
    s = Svg(name, W, H, pivot=(100, 24), kind="prop")
    base = H - 24
    ground_shadow(s, 100, base, 50, 15)
    trunk(s, 100, base, base - 60, 9, "#5a3a22", rng)
    dark, mid, light = pal
    y, hw = base - 50, 70
    while hw > 14:
        top = y - hw * 1.1
        d = f"M100 {f(top)} L{f(100 + hw)} {f(y)} Q{f(100 + hw * 0.4)} {f(y - 10)} 100 {f(y + 8)} Q{f(100 - hw * 0.4)} {f(y - 10)} {f(100 - hw)} {f(y)} Z"
        s.path(d, s.lin(mid, dark, 0, 0, 1, 1), stroke=INK, sw=3)
        cid = s.clip(f'<path d="{d}"/>')
        s.add(f'<g clip-path="url(#{cid})"><path d="M{f(100 - hw)} {f(y + 10)} L100 {f(top - 4)} L{f(100 - hw * 0.15)} {f(y + 10)} Z" fill="{light}" opacity="0.5"/></g>')
        y -= hw * 0.78
        hw *= 0.8
    return s


def tree_palm_dry(name):
    """Árvore retorcida de ruínas, com poucas folhas secas."""
    rng = _rng(name)
    W, H = 260, 320
    s = Svg(name, W, H, pivot=(130, 24), kind="prop")
    base = H - 24
    ground_shadow(s, 130, base, 60, 16)
    col = "#8a6a48"
    trunk(s, 130, base, base - 90, 14, col, rng, lean=10)
    branches(s, 140, base - 88, -math.pi / 2 - 0.3, 60, 14, 3, rng, col)
    branches(s, 140, base - 88, -math.pi / 2 + 0.5, 50, 12, 3, rng, col)
    pal = ("#6f6a2a", "#a39a45", "#d4c77a")
    foliage(s, [(96, base - 200, 34), (170, base - 190, 38), (132, base - 232, 32)], pal, rng)
    return s


# ---------------------------------------------------------------------- objetos

def stump(name, moss=False):
    rng = _rng(name)
    W, H = 150, 120
    s = Svg(name, W, H, pivot=(75, 22), kind="prop")
    base = H - 22
    ground_shadow(s, 75, base, 50, 13)
    wood = "#7a5234"
    d = f"M36 {base - 44} L36 {base - 4} Q24 {base + 2} 22 {base + 4} Q75 {base + 16} 128 {base + 4} Q118 {base} 114 {base - 4} L114 {base - 44} Z"
    s.path(d, s.lin(shade(wood, 0.08), shade(wood, -0.25), 0, 0, 1, 0), stroke=INK, sw=3)
    for x in (52, 72, 96):
        s.path(f"M{x} {base - 40} q{rng.uniform(-3, 3)} 18 {rng.uniform(-2, 2)} 36", "none", stroke=shade(wood, -0.35), sw=2)
    s.ellipse(75, base - 44, 39, 16, "#d9b27a", stroke=INK, sw=3)
    for r in (28, 18, 8):
        s.ellipse(75, base - 44, r, r * 0.4, "none", stroke="#a47a48", sw=1.8)
    if moss:
        s.path(cloud(50, base - 12, 14, 6, rng, squash=0.6), s.lin("#8fbf4a", "#5e8f30"), stroke=INK, sw=2)
    else:
        s.path(f"M110 {base - 30} q12 -4 14 -18", "none", stroke=INK, sw=5)
        s.path(f"M110 {base - 30} q12 -4 14 -18", "none", stroke="#6aa83c", sw=2.5)
    return s


def log(name):
    rng = _rng(name)
    W, H = 220, 130
    s = Svg(name, W, H, pivot=(110, 26), kind="prop")
    base = H - 26
    ground_shadow(s, 110, base - 4, 84, 16)
    wood = "#7a5234"
    # tronco deitado na diagonal da grade
    x0, y0, x1, y1, r = 40, base - 44, 170, base - 10, 22
    ang = math.atan2(y1 - y0, x1 - x0)
    nx, ny = -math.sin(ang) * r, math.cos(ang) * r
    d = f"M{f(x0 - nx)} {f(y0 - ny)} L{f(x1 - nx)} {f(y1 - ny)} L{f(x1 + nx)} {f(y1 + ny)} L{f(x0 + nx)} {f(y0 + ny)} Z"
    s.path(d, s.lin(shade(wood, 0.1), shade(wood, -0.25)), stroke=INK, sw=3)
    for t in (0.25, 0.5, 0.75):
        x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
        s.path(f"M{f(x - nx * 0.6)} {f(y - ny * 0.6)} l{f(rng.uniform(10, 20))} {f(rng.uniform(3, 6))}", "none", stroke=shade(wood, -0.35), sw=2)
    s.ellipse(x1, y1, r * 0.72, r, "#d9b27a", stroke=INK, sw=3, extra=f'transform="rotate({f(math.degrees(ang))} {f(x1)} {f(y1)})"')
    s.ellipse(x1, y1, r * 0.4, r * 0.55, "none", stroke="#a47a48", sw=1.8, extra=f'transform="rotate({f(math.degrees(ang))} {f(x1)} {f(y1)})"')
    for x in (70, 120):
        s.path(cloud(x, y0 + (x - x0) * (y1 - y0) / (x1 - x0) - r + 4, 10, 6, rng, squash=0.6), s.lin("#8fbf4a", "#5e8f30"), stroke=INK, sw=2)
    return s


def mushrooms(name, cap="#c8423a"):
    rng = _rng(name)
    W, H = 160, 150
    s = Svg(name, W, H, pivot=(80, 22), kind="prop")
    base = H - 22
    ground_shadow(s, 80, base, 52, 13)
    for x, h, r in ((54, 46, 26), (100, 70, 34), (78, 26, 18)):
        s.path(f"M{x - 8} {base} L{x - 6} {base - h} L{x + 6} {base - h} L{x + 8} {base} Q{x} {base + 4} {x - 8} {base} Z", "#efe3c8", stroke=INK, sw=2.4)
        d = f"M{x - r} {base - h + 4} Q{x - r} {base - h - r * 1.05} {x} {base - h - r * 1.05} Q{x + r} {base - h - r * 1.05} {x + r} {base - h + 4} Q{x} {base - h - 6} {x - r} {base - h + 4} Z"
        s.path(d, s.lin(shade(cap, 0.12), shade(cap, -0.2)), stroke=INK, sw=2.6)
        for _ in range(3):
            s.circle(x + rng.uniform(-r * 0.55, r * 0.55), base - h - rng.uniform(r * 0.3, r * 0.75), rng.uniform(2.5, 4.5), "#fff6e8", stroke=None)
    return s


def _box(s, P, u0, v0, u1, v1, z0, z1, color, lines=0, top=None):
    """Caixa isométrica (u, v em células, z em px)."""
    left = poly([P(u0, v1, z0), P(u1, v1, z0), P(u1, v1, z1), P(u0, v1, z1)])
    right = poly([P(u1, v1, z0), P(u1, v0, z0), P(u1, v0, z1), P(u1, v1, z1)])
    lid = poly([P(u0, v0, z1), P(u1, v0, z1), P(u1, v1, z1), P(u0, v1, z1)])
    s.path(left, shade(color, -0.05), stroke=INK, sw=2.5)
    s.path(right, shade(color, -0.22), stroke=INK, sw=2.5)
    s.path(lid, top or shade(color, 0.12), stroke=INK, sw=2.5)
    for k in range(1, lines):
        z = z0 + (z1 - z0) * k / lines
        for a, b in ((P(u0, v1, z), P(u1, v1, z)), (P(u1, v1, z), P(u1, v0, z))):
            s.path(f"M{f(a[0])} {f(a[1])} L{f(b[0])} {f(b[1])}", "none", stroke=shade(color, -0.35), sw=1.8)


def crates(name):
    W, H = 200, 180
    s = Svg(name, W, H, pivot=(100, 50), kind="prop")
    P = lambda u, v, z=0: iso_pt(100, H - 100, u, v, z)
    ground_shadow(s, 100, H - 50, 70, 20)
    wood = "#b07a44"
    _box(s, P, 0.18, 0.5, 0.58, 0.9, 0, 44, wood, lines=3)
    _box(s, P, 0.52, 0.2, 0.9, 0.58, 0, 40, shade(wood, -0.08), lines=3)
    _box(s, P, 0.3, 0.55, 0.62, 0.87, 44, 80, shade(wood, 0.05), lines=2)
    return s


def barrels(name):
    rng = _rng(name)
    W, H = 180, 170
    s = Svg(name, W, H, pivot=(90, 24), kind="prop")
    base = H - 24
    ground_shadow(s, 90, base, 64, 16)
    wood = "#8a5a32"
    for x, y, r, h in ((62, base - 10, 26, 64), (116, base - 2, 28, 70)):
        d = f"M{x - r} {y - h} Q{x - r - 7} {y - h / 2} {x - r} {y} A{r} {r * 0.4} 0 0 0 {x + r} {y} Q{x + r + 7} {y - h / 2} {x + r} {y - h} Z"
        s.path(d, s.lin(shade(wood, 0.1), shade(wood, -0.25), 0, 0, 1, 0), stroke=INK, sw=2.8)
        for t in (0.18, 0.82):
            yy = y - h * t
            s.path(f"M{x - r - 5} {f(yy)} A{r + 5} {(r + 5) * 0.4} 0 0 0 {x + r + 5} {f(yy)}", "none", stroke="#55524e", sw=4)
        s.ellipse(x, y - h, r, r * 0.4, shade(wood, 0.18), stroke=INK, sw=2.5)
        s.path(f"M{x - r * 0.6} {y - h} L{x + r * 0.6} {y - h}", "none", stroke=shade(wood, -0.2), sw=1.5)
    return s


def haystack(name):
    rng = _rng(name)
    W, H = 190, 170
    s = Svg(name, W, H, pivot=(95, 24), kind="prop")
    base = H - 24
    ground_shadow(s, 95, base, 72, 18)
    hay = "#e2bf5a"
    d = f"M22 {base} Q18 {base - 70} 60 {base - 108} Q95 {base - 134} 130 {base - 108} Q172 {base - 70} 168 {base} Q95 {base + 14} 22 {base} Z"
    s.path(d, s.lin(shade(hay, 0.1), shade(hay, -0.22), 0.2, 0, 0.8, 1), stroke=INK, sw=3)
    for _ in range(26):
        x = rng.uniform(34, 156)
        y = rng.uniform(base - 110, base - 8)
        if abs(x - 95) > 70 - (base - y) * 0.2:
            continue
        s.path(f"M{f(x)} {f(y)} l{f(rng.uniform(-6, 6))} {f(rng.uniform(8, 16))}", "none", stroke=shade(hay, -0.3), sw=1.8)
    s.path(f"M36 {base - 40} Q95 {base - 22} 154 {base - 40}", "none", stroke="#8a5a32", sw=4)
    return s


def lamp(name):
    W, H = 110, 290
    s = Svg(name, W, H, pivot=(55, 20), kind="prop")
    base = H - 20
    ground_shadow(s, 55, base, 26, 8)
    s.circle(55, base - 216, 44, "#ffd66b", stroke=None, extra='opacity="0.22"')
    s.path(f"M40 {base} L44 {base - 16} L66 {base - 16} L70 {base} Z", "#4a4c55", stroke=INK, sw=2.5)
    s.limb([(55, base - 16), (55, base - 190)], 5, "#3d3f48", ow=2.2, highlight=False)
    s.path(f"M40 {base - 190} L70 {base - 190} L74 {base - 196} L36 {base - 196} Z", "#3d3f48", stroke=INK, sw=2)
    s.path(f"M40 {base - 196} L38 {base - 236} L72 {base - 236} L70 {base - 196} Z", s.lin("#fff2b0", "#ffc24a"), stroke=INK, sw=2.5)
    s.path(f"M55 {base - 196} L55 {base - 236}", "none", stroke="#3d3f48", sw=2)
    s.path(f"M32 {base - 236} L55 {base - 254} L78 {base - 236} Z", "#3d3f48", stroke=INK, sw=2.5)
    return s


def signpost(name):
    W, H = 170, 230
    s = Svg(name, W, H, pivot=(85, 20), kind="prop")
    base = H - 20
    ground_shadow(s, 85, base, 30, 9)
    wood = "#9a6a3c"
    s.limb([(85, base), (85, base - 180)], 6, shade(wood, -0.1), ow=2.4, highlight=False)
    for y, dx, flip in ((base - 170, 1, 1), (base - 128, -1, -1)):
        x0 = 85 - 10 * dx
        x1 = 85 + 62 * dx
        d = f"M{x0} {y - 13} L{x1} {y - 13} L{x1 + 14 * dx} {y} L{x1} {y + 13} L{x0} {y + 13} Z"
        s.path(d, s.lin(shade(wood, 0.12), shade(wood, -0.12)), stroke=INK, sw=2.5)
        s.path(f"M{x0 + 10 * dx} {y} L{x1 - 6 * dx} {y}", "none", stroke=shade(wood, -0.35), sw=2)
    return s


def bench(name):
    W, H = 200, 150
    s = Svg(name, W, H, pivot=(100, 50), kind="prop")
    P = lambda u, v, z=0: iso_pt(100, H - 100, u, v, z)
    ground_shadow(s, 100, H - 50, 66, 16)
    wood = "#a06a3a"
    for u in (0.2, 0.75):
        _box(s, P, u, 0.46, u + 0.07, 0.54, 0, 30, "#55524e")
    _box(s, P, 0.12, 0.4, 0.88, 0.6, 30, 38, wood)
    _box(s, P, 0.12, 0.36, 0.88, 0.42, 38, 72, shade(wood, -0.05), lines=2)
    return s


def stall(name, cloth=("#d9493c", "#f4ead2")):
    W, H = 260, 290
    s = Svg(name, W, H, pivot=(130, 60), kind="prop")
    P = lambda u, v, z=0: iso_pt(130, H - 110, u, v, z)
    ground_shadow(s, 130, H - 60, 100, 28)
    wood = "#9a6a3c"
    for u, v in ((0.05, 0.3), (0.95, 0.3), (0.05, 0.95), (0.95, 0.95)):
        s.limb([P(u, v, 0), P(u, v, 150)], 4, wood, ow=2, highlight=False)
    _box(s, P, 0.05, 0.62, 0.95, 0.95, 0, 58, wood, lines=2, top="#c89a62")
    for k, (x, c) in enumerate(((0.2, "#e8453c"), (0.35, "#74b83f"), (0.5, "#ffb640"), (0.66, "#e8453c"), (0.8, "#ffe066"))):
        px, py = P(x, 0.78, 62)
        for j in range(3):
            s.circle(px + (j - 1) * 7, py - (4 if j == 1 else 0), 5, c, stroke=INK, sw=1.2)
    # toldo listrado inclinado
    n = 6
    for k in range(n):
        u0, u1 = k / n, (k + 1) / n
        pts = [P(u0 * 1.1 - 0.05, 0.2, 172), P(u1 * 1.1 - 0.05, 0.2, 172), P(u1 * 1.1 - 0.05, 1.05, 130), P(u0 * 1.1 - 0.05, 1.05, 130)]
        s.path(poly(pts), cloth[k % 2], stroke=INK, sw=2.2)
    for k in range(n):
        u0, u1 = k / n * 1.1 - 0.05, (k + 1) / n * 1.1 - 0.05
        a, b = P(u0, 1.05, 130), P(u1, 1.05, 130)
        mx, my = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2 + 14
        s.path(f"M{f(a[0])} {f(a[1])} Q{f(mx)} {f(my)} {f(b[0])} {f(b[1])} Z", cloth[k % 2], stroke=INK, sw=2)
    return s


def rubble(name):
    rng = _rng(name)
    W, H = 190, 140
    s = Svg(name, W, H, pivot=(95, 50), kind="prop")
    P = lambda u, v, z=0: iso_pt(95, H - 100, u, v, z)
    ground_shadow(s, 95, H - 50, 70, 18)
    stone = "#cfc7b2"
    _box(s, P, 0.15, 0.45, 0.55, 0.8, 0, 30, stone)
    _box(s, P, 0.5, 0.2, 0.85, 0.5, 0, 22, shade(stone, -0.06))
    _box(s, P, 0.25, 0.5, 0.5, 0.72, 30, 50, shade(stone, 0.04))
    for _ in range(4):
        x, y = P(rng.uniform(0.1, 0.9), rng.uniform(0.7, 1.0))
        s.ellipse(x, y, rng.uniform(5, 9), rng.uniform(3, 5), shade(stone, -0.1), stroke=INK, sw=1.6)
    s.path(cloud(*P(0.62, 0.62, 6), 11, 6, rng, squash=0.6), s.lin("#a9b85a", "#7a8a3a"), stroke=INK, sw=1.8)
    return s


# ---------------------------------------------------------------------- detalhes de chão (não bloqueiam, ficam sob os personagens)

def decor(biome, kind, v):
    name = f"decor_{biome}_{kind}_{v}"
    rng = _rng(name)
    b = BIOMES[biome]
    W, H = 200, 100
    s = Svg(name, W, H, pivot=(100, 50), kind="tile")
    tuft = b["tuft"]
    spots = [(100 + rng.uniform(-50, 50), 50 + rng.uniform(-18, 18)) for _ in range(3 if kind != "tufts" else 4)]
    if kind == "tufts":
        for x, y in spots:
            for k in range(5):
                a = -math.pi / 2 + (k - 2) * 0.35
                L = rng.uniform(10, 17)
                d = f"M{f(x)} {f(y)} q{f(math.cos(a) * L * 0.3)} {f(-L * 0.6)} {f(math.cos(a) * L)} {f(math.sin(a) * L)}"
                s.add(f'<path d="{d}" fill="none" stroke="{shade(tuft, -0.25)}" stroke-width="4" stroke-linecap="round"/>')
                s.add(f'<path d="{d}" fill="none" stroke="{shade(tuft, 0.12 if k % 2 else 0)}" stroke-width="2" stroke-linecap="round"/>')
    elif kind == "flowers":
        for x, y in spots:
            for _ in range(4):
                fx, fy = x + rng.uniform(-10, 10), y + rng.uniform(-5, 5)
                s.path(f"M{f(fx)} {f(fy + 6)} L{f(fx)} {f(fy)}", "none", stroke=tuft, sw=2)
                c = rng.choice(b["flowers"])
                for a in range(5):
                    ang = a / 5 * math.tau
                    s.circle(fx + math.cos(ang) * 2.6, fy + math.sin(ang) * 2, 2.1, c, stroke=None)
                s.circle(fx, fy, 1.4, "#e8a33a", stroke=None)
    elif kind == "pebbles":
        for x, y in spots:
            for _ in range(3):
                g = rng.choice(["#b9b2a3", "#a39c8e", "#cfc8b8"])
                px, py = x + rng.uniform(-12, 12), y + rng.uniform(-5, 5)
                s.ellipse(px, py, rng.uniform(4, 7), rng.uniform(2.5, 4), g, stroke=shade(g, -0.4), sw=1.4)
                s.ellipse(px - 1.5, py - 1, 2, 1, "#ffffff", stroke=None, extra='opacity="0.4"')
    elif kind == "mush":
        for x, y in spots[:2]:
            for _ in range(2):
                mx, my = x + rng.uniform(-8, 8), y + rng.uniform(-4, 4)
                s.path(f"M{f(mx - 2)} {f(my)} L{f(mx - 2)} {f(my - 7)} L{f(mx + 2)} {f(my - 7)} L{f(mx + 2)} {f(my)} Z", "#efe3c8", stroke=INK, sw=1.2)
                s.path(f"M{f(mx - 7)} {f(my - 6)} Q{f(mx)} {f(my - 16)} {f(mx + 7)} {f(my - 6)} Z", "#c8423a" if v == 0 else "#b8864a", stroke=INK, sw=1.4)
    elif kind == "leaves":
        for x, y in spots:
            for _ in range(4):
                lx, ly = x + rng.uniform(-14, 14), y + rng.uniform(-6, 6)
                c = rng.choice(["#c98a3a", "#b8552a", "#d9b14a", "#8a6a2a"])
                s.ellipse(lx, ly, 4.5, 2.2, c, stroke=shade(c, -0.4), sw=1, extra=f'transform="rotate({rng.randint(0, 180)} {f(lx)} {f(ly)})"')
    elif kind == "clover":
        for x, y in spots:
            for _ in range(4):
                cx, cy = x + rng.uniform(-10, 10), y + rng.uniform(-5, 5)
                for a in range(3):
                    ang = a / 3 * math.tau - math.pi / 2
                    s.circle(cx + math.cos(ang) * 2.8, cy + math.sin(ang) * 2.2, 2.6, shade(tuft, 0.15), stroke=shade(tuft, -0.3), sw=1)
    elif kind == "bones":
        for x, y in spots[:2]:
            s.limb([(x - 9, y + 2), (x + 9, y - 2)], 2.5, "#efe6d0", ow=1.2, highlight=False)
            for ex, ey in ((x - 9, y + 2), (x + 9, y - 2)):
                s.circle(ex, ey - 1.5, 2.6, "#efe6d0", stroke=INK, sw=1)
                s.circle(ex, ey + 1.5, 2.6, "#efe6d0", stroke=INK, sw=1)
    return s


DECOR_KINDS = {
    "village": ["tufts", "flowers", "pebbles", "clover"],
    "meadow": ["tufts", "flowers", "pebbles", "clover"],
    "forest": ["tufts", "mush", "leaves", "pebbles"],
    "swamp": ["tufts", "mush", "clover", "pebbles"],
    "ruins": ["tufts", "pebbles", "bones", "leaves"],
}


# ---------------------------------------------------------------------- casas variadas

def house2(name, roof, wall, style="timber", roof_style="tiles", chimney=True, extras=(), seed=0):
    """Casa 2x2: parede de enxaimel, tábuas ou pedra; telhado de telhas, palha ou ardósia."""
    rng = random.Random(seed or zlib.crc32(name.encode()))
    W, H = 440, 560
    ox, oy = 220, H - 230
    s = Svg(name, W, H, pivot=(220, 130), kind="prop")
    P = lambda u, v, z=0: iso_pt(ox, oy, u, v, z)
    a, b = 0.28, 1.72
    wall_h = 150 if style != "stone" else 140
    ridge = wall_h + (110 if roof_style != "thatch" else 120)
    ground_shadow(s, *P(1.05, 1.05), 200, 64, opacity=0.25)
    left = poly([P(a, b), P(b, b), P(b, b, wall_h), P(a, b, wall_h)])
    right = poly([P(b, b), P(b, a), P(b, a, wall_h), P(b, b, wall_h)])
    s.path(left, s.lin(shade(wall, 0.04), shade(wall, -0.08), 0, 0, 1, 0), stroke=INK, sw=3)
    s.path(right, s.lin(shade(wall, -0.14), shade(wall, -0.22), 0, 0, 1, 0), stroke=INK, sw=3)
    beam = "#6b4226"
    if style == "stone":
        # fiadas de pedra com juntas desencontradas
        rows = 7
        for r in range(1, rows):
            z = wall_h * r / rows
            for p0, p1 in ((P(a, b, z), P(b, b, z)), (P(b, b, z), P(b, a, z))):
                s.path(f"M{f(p0[0])} {f(p0[1])} L{f(p1[0])} {f(p1[1])}", "none", stroke=shade(wall, -0.35), sw=1.6)
            for k in range(6):
                t = (k + (0.5 if r % 2 else 0)) / 6
                if t >= 1:
                    continue
                u = a + (b - a) * t
                p0, p1 = P(u, b, z - wall_h / rows), P(u, b, z)
                s.path(f"M{f(p0[0])} {f(p0[1])} L{f(p1[0])} {f(p1[1])}", "none", stroke=shade(wall, -0.35), sw=1.4)
                v = b - (b - a) * t
                p0, p1 = P(b, v, z - wall_h / rows), P(b, v, z)
                s.path(f"M{f(p0[0])} {f(p0[1])} L{f(p1[0])} {f(p1[1])}", "none", stroke=shade(wall, -0.45), sw=1.4)
    elif style == "plank":
        for k in range(1, 12):
            u = a + (b - a) * k / 12
            p0, p1 = P(u, b, 0), P(u, b, wall_h)
            s.path(f"M{f(p0[0])} {f(p0[1])} L{f(p1[0])} {f(p1[1])}", "none", stroke=shade(wall, -0.3), sw=1.6)
            v = a + (b - a) * k / 12
            p0, p1 = P(b, v, 0), P(b, v, wall_h)
            s.path(f"M{f(p0[0])} {f(p0[1])} L{f(p1[0])} {f(p1[1])}", "none", stroke=shade(wall, -0.4), sw=1.6)
    s.path(poly([P(a, b), P(b, b), P(b, b, 24), P(a, b, 24)]), "#a39a8a", stroke=INK, sw=2.5)
    s.path(poly([P(b, b), P(b, a), P(b, a, 24), P(b, b, 24)]), "#8a8274", stroke=INK, sw=2.5)
    if style == "timber":
        for u in (a, 0.95, 1.4, b):
            s.limb([P(u, b, 24), P(u, b, wall_h)], 6, beam, ow=1.5, highlight=False)
        for v in (a, 1.0, b):
            s.limb([P(b, v, 24), P(b, v, wall_h)], 6, beam, ow=1.5, highlight=False)
        s.limb([P(a, b, wall_h * 0.62), P(b, b, wall_h * 0.62)], 5, beam, ow=1.5, highlight=False)
        s.limb([P(a, b, 60), P(0.95, b, wall_h * 0.62)], 4, beam, ow=1.2, highlight=False)
    else:
        for u, v in ((a, b), (b, b), (b, a)):
            s.limb([P(u, v, 24), P(u, v, wall_h)], 7, beam if style == "plank" else shade(wall, -0.2), ow=1.5, highlight=False)
    # porta
    du0, du1 = 1.0, 1.32
    door_h = 96
    s.path(poly([P(du0 - 0.04, b, 22), P(du1 + 0.04, b, 22), P(du1 + 0.04, b, door_h + 6), P(du0 - 0.04, b, door_h + 6)]), "#5e3a20", stroke=INK, sw=2.5)
    door = poly([P(du0, b, 22), P(du1, b, 22), P(du1, b, door_h), P(du0, b, door_h)])
    s.path(door, s.lin("#9a6236", "#6a4224", 0, 0, 1, 0), stroke=INK, sw=2.5)
    for t in (0.33, 0.66):
        p0, p1 = P(du0 + (du1 - du0) * t, b, 24), P(du0 + (du1 - du0) * t, b, door_h - 2)
        s.path(f"M{f(p0[0])} {f(p0[1])} L{f(p1[0])} {f(p1[1])}", "none", stroke="#4a2e1a", sw=1.6)
    s.circle(*P(du1 - 0.06, b, 58), 3, "#e8c14a", stroke=INK, sw=1.2)
    # degrau
    s.path(poly([P(du0 - 0.06, b + 0.02), P(du1 + 0.06, b + 0.02), P(du1 + 0.06, b + 0.14), P(du0 - 0.06, b + 0.14)]), "#b8b0a0", stroke=INK, sw=2)

    def window(p0, p1, z0, z1, box=False):
        w = poly([p0(z0), p1(z0), p1(z1), p0(z1)])
        s.path(poly([p0(z0 - 5), p1(z0 - 5), p1(z1 + 5), p0(z1 + 5)]), beam, stroke=INK, sw=2)
        s.path(w, s.lin("#fff0b8", "#f2b64a"), stroke=INK, sw=2.2)
        c0, c1 = p0((z0 + z1) / 2), p1((z0 + z1) / 2)
        s.path(f"M{f(c0[0])} {f(c0[1])} L{f(c1[0])} {f(c1[1])}", "none", stroke=beam, sw=2.5)
        m0, m1 = p0(z0), p1(z0)
        mx, my = (m0[0] + m1[0]) / 2, (m0[1] + m1[1]) / 2
        n0, n1 = p0(z1), p1(z1)
        s.path(f"M{f(mx)} {f(my)} L{f((n0[0] + n1[0]) / 2)} {f((n0[1] + n1[1]) / 2)}", "none", stroke=beam, sw=2)
        if box:
            q0, q1 = p0(z0 - 6), p1(z0 - 6)
            q2, q3 = p1(z0 - 20), p0(z0 - 20)
            s.path(poly([q0, q1, q2, q3]), "#8a5a32", stroke=INK, sw=2)
            for t in (0.15, 0.4, 0.62, 0.85):
                x = q0[0] + (q1[0] - q0[0]) * t
                y = q0[1] + (q1[1] - q0[1]) * t - 6
                s.path(cloud(x, y, 6, 5, rng, squash=0.8), "#5e8f30", stroke=INK, sw=1.4)
                s.circle(x + rng.uniform(-2, 2), y - 2, 2.4, rng.choice(["#f5a3b8", "#ffe07a", "#e8453c", "#ffffff"]), stroke=None)
    flowers = "flowerbox" in extras
    window(lambda z: P(0.48, b, z), lambda z: P(0.8, b, z), 58, 100, flowers)
    window(lambda z: P(b, 0.5, z), lambda z: P(b, 0.86, z), 58, 100, flowers)
    window(lambda z: P(b, 1.14, z), lambda z: P(b, 1.48, z), 58, 100, flowers)
    if "lantern" in extras:
        lx, ly = P(du1 + 0.12, b, 118)
        s.circle(lx, ly + 10, 14, "#ffd66b", stroke=None, extra='opacity="0.3"')
        s.path(f"M{f(lx - 6)} {f(ly)} L{f(lx + 6)} {f(ly)} L{f(lx + 5)} {f(ly + 18)} L{f(lx - 5)} {f(ly + 18)} Z", "#ffcf5a", stroke=INK, sw=1.8)
    if "sign" in extras:
        sx, sy = P(0.64, b, 128)
        s.limb([(sx, sy - 18), (sx - 24, sy - 18)], 2.5, "#3d3f48", ow=1, highlight=False)
        s.path(f"M{f(sx - 40)} {f(sy - 14)} L{f(sx - 8)} {f(sy - 14)} L{f(sx - 8)} {f(sy + 12)} L{f(sx - 40)} {f(sy + 12)} Z", "#c89a62", stroke=INK, sw=2)
        s.circle(sx - 24, sy - 1, 6, "#d9493c", stroke=INK, sw=1.4)
    # telhado (cumeeira ao longo de u, em v = 1)
    oh = 0.2
    eave = wall_h - 8
    back = poly([P(a - oh, 1.0, ridge), P(b + oh, 1.0, ridge), P(b + oh, a - oh, eave), P(a - oh, a - oh, eave)])
    s.path(back, shade(roof, -0.25), stroke=INK, sw=3)
    gable = poly([P(b, b, wall_h), P(b, a, wall_h), P(b, 1.0, ridge - 6)])
    gwall = wall if style != "stone" else shade(wall, 0.02)
    s.path(gable, s.lin(shade(gwall, -0.12), shade(gwall, -0.24)), stroke=INK, sw=3)
    if style == "timber":
        s.limb([P(b, a + 0.2, wall_h + 4), P(b, 1.0, ridge - 14)], 4, beam, ow=1.2, highlight=False)
        s.limb([P(b, b - 0.2, wall_h + 4), P(b, 1.0, ridge - 14)], 4, beam, ow=1.2, highlight=False)
    gx, gy = P(b, 1.0, wall_h + 42)
    s.path(f"M{f(gx - 8)} {f(gy - 12)} L{f(gx + 8)} {f(gy - 20)} L{f(gx + 8)} {f(gy + 4)} L{f(gx - 8)} {f(gy + 12)} Z", s.lin("#fff0b8", "#f2b64a"), stroke=INK, sw=2)
    front = poly([P(a - oh, 1.0, ridge), P(b + oh, 1.0, ridge), P(b + oh, b + oh, eave), P(a - oh, b + oh, eave)])
    if roof_style == "thatch":
        s.path(front, s.lin(shade(roof, 0.12), shade(roof, -0.1), 0, 0, 0.3, 1), stroke=INK, sw=3.2)
        for k in range(40):
            u = rng.uniform(a - oh + 0.05, b + oh - 0.05)
            t = rng.uniform(0.05, 0.95)
            v = 1.0 + (b + oh - 1.0) * t
            z = ridge + (eave - ridge) * t
            p0, p1 = P(u, v, z), P(u, v + 0.08, z - 12)
            s.path(f"M{f(p0[0])} {f(p0[1])} L{f(p1[0])} {f(p1[1])}", "none", stroke=shade(roof, -0.3), sw=1.8)
        # beiral grosso e irregular
        pts = []
        for k in range(13):
            u = (a - oh) + (b - a + 2 * oh) * k / 12
            pts.append(P(u, b + oh, eave - (6 if k % 2 else 14)))
        edge = "M" + " L".join(f"{f(x)} {f(y)}" for x, y in [P(a - oh, b + oh, eave)] + pts + [P(b + oh, b + oh, eave)])
        s.path(edge + " Z", shade(roof, -0.28), stroke=INK, sw=2.2)
        cap = poly([P(a - oh, 0.9, ridge + 4), P(b + oh, 0.9, ridge + 4), P(b + oh, 1.1, ridge - 8), P(a - oh, 1.1, ridge - 8)])
        s.path(cap, shade(roof, -0.15), stroke=INK, sw=2.2)
    else:
        s.path(front, s.lin(shade(roof, 0.1), shade(roof, -0.08), 0, 0, 0.3, 1), stroke=INK, sw=3.2)
        rows = 6 if roof_style == "tiles" else 7
        for k in range(1, rows):
            t = k / rows
            v = 1.0 + (b + oh - 1.0) * t
            z = ridge + (eave - ridge) * t
            p0, p1 = P(a - oh, v, z), P(b + oh, v, z)
            s.path(f"M{f(p0[0])} {f(p0[1])} L{f(p1[0])} {f(p1[1])}", "none", stroke=shade(roof, -0.3), sw=2)
            if roof_style == "tiles":
                for j in range(1, 10):
                    u = (a - oh) + (b - a + 2 * oh) * (j + 0.5 * (k % 2)) / 10
                    q0 = P(u, v, z)
                    q1 = P(u, v - (b + oh - 1.0) / rows, z - (eave - ridge) / rows)
                    s.path(f"M{f(q0[0])} {f(q0[1])} L{f(q1[0])} {f(q1[1])}", "none", stroke=shade(roof, -0.22), sw=1.4)
            else:
                for j in range(1, 14):
                    u = (a - oh) + (b - a + 2 * oh) * (j + 0.5 * (k % 2)) / 14
                    q0 = P(u, v, z)
                    q1 = P(u, v - (b + oh - 1.0) / rows, z - (eave - ridge) / rows)
                    s.path(f"M{f(q0[0])} {f(q0[1])} L{f(q1[0])} {f(q1[1])}", "none", stroke=shade(roof, -0.3), sw=1.2)
        s.path(poly([P(a - oh, b + oh, eave), P(b + oh, b + oh, eave), P(b + oh, b + oh, eave - 10), P(a - oh, b + oh, eave - 10)]), shade(roof, -0.3), stroke=INK, sw=2.5)
        s.path(poly([P(a - oh, 0.94, ridge + 6), P(b + oh, 0.94, ridge + 6), P(b + oh, 1.06, ridge - 2), P(a - oh, 1.06, ridge - 2)]), shade(roof, -0.18), stroke=INK, sw=2)
    s.path(poly([P(b + oh, b + oh, eave), P(b + oh, 1.0, ridge), P(b + oh, 1.0, ridge - 10), P(b + oh, b + oh, eave - 10)]), shade(roof, -0.38), stroke=INK, sw=2.5)
    if chimney:
        cu, cv = 0.62, 0.7
        chim_col = "#a8a196" if style == "stone" else "#b0664a"
        pts = [P(cu, cv, ridge - 20), P(cu + 0.2, cv, ridge - 30), P(cu + 0.2, cv, ridge + 48), P(cu, cv, ridge + 58)]
        s.path(poly(pts), s.lin(chim_col, shade(chim_col, -0.25), 0, 0, 1, 0), stroke=INK, sw=2.5)
        s.path(poly([P(cu, cv, ridge + 58), P(cu + 0.2, cv, ridge + 48), P(cu + 0.2, cv - 0.2, ridge + 48), P(cu, cv - 0.2, ridge + 58)]), "#4a3226", stroke=INK, sw=2)
        tx, ty = P(cu + 0.1, cv - 0.1, ridge + 60)
        for k in range(3):
            s.circle(tx + k * 9, ty - 16 - k * 16, 7 + k * 2.5, "#e8eaf0", stroke=None, extra=f'opacity="{0.55 - k * 0.15}"')
    if "barrel" in extras:
        bx, by = P(b + 0.14, b - 0.1)
        s.path(f"M{f(bx - 14)} {f(by - 36)} Q{f(bx - 18)} {f(by - 18)} {f(bx - 14)} {f(by)} A14 6 0 0 0 {f(bx + 14)} {f(by)} Q{f(bx + 18)} {f(by - 18)} {f(bx + 14)} {f(by - 36)} Z", "#8a5a32", stroke=INK, sw=2.2)
        s.ellipse(bx, by - 36, 14, 6, "#a8764a", stroke=INK, sw=2)
        s.path(f"M{f(bx - 16)} {f(by - 10)} A16 6 0 0 0 {f(bx + 16)} {f(by - 10)}", "none", stroke="#55524e", sw=3)
    if "woodpile" in extras:
        wx, wy = P(a + 0.1, b + 0.16)
        for r in range(2):
            for k in range(3 - r):
                cx, cy = wx - 16 + k * 14 + r * 7, wy - 8 - r * 12
                s.circle(cx, cy, 7, "#c89a62", stroke=INK, sw=1.6)
                s.circle(cx, cy, 3, "#a47a48", stroke=None)
    return s


HOUSES = [
    ("house_0", "#c4553a", "#f1e3c4", "timber", "tiles", ("flowerbox",)),
    ("house_1", "#4a78b8", "#efe2c8", "timber", "tiles", ("lantern", "barrel")),
    ("house_2", "#5b6173", "#b9b3a6", "stone", "slate", ("woodpile",)),
    ("house_3", "#d9b25a", "#c9a172", "plank", "thatch", ("barrel", "woodpile")),
    ("house_4", "#3f7a55", "#f4ecdc", "timber", "tiles", ("flowerbox", "sign", "lantern")),
    ("house_5", "#8a4a6a", "#e7d6bc", "stone", "tiles", ("flowerbox",)),
]


def build():
    out = []
    out.append(tree_birch("tree_birch"))
    out.append(tree_cypress("tree_cypress", ("#233d24", "#3f6b3a", "#77a25a")))
    out.append(tree_cypress("tree_cypress_swamp", ("#2e3a24", "#4e5e34", "#8a9a52")))
    out.append(tree_oak_big("tree_oak_big", ("#2c4620", "#4a7030", "#8aab52")))
    out.append(tree_oak_big("tree_oak_forest", ("#1f3319", "#34522a", "#62854a"), dots=("#cfe3a0", 0)))
    out.append(tree_young("tree_young", ("#3a5a28", "#62903e", "#a8c86a")))
    out.append(tree_young("tree_apple", ("#2e4a22", "#4f7a36", "#94b45a"), dots=("#d9383c", 8)))
    out.append(tree_spruce("tree_spruce", ("#1a352a", "#2c5c44", "#5a9474")))
    out.append(tree_palm_dry("tree_ruins_2"))
    out.append(stump("stump"))
    out.append(stump("stump_moss", moss=True))
    out.append(log("log"))
    out.append(mushrooms("mushrooms"))
    out.append(mushrooms("mushrooms_swamp", cap="#8a6aa8"))
    out.append(crates("crates"))
    out.append(barrels("barrels"))
    out.append(haystack("haystack"))
    out.append(lamp("lamp"))
    out.append(signpost("signpost"))
    out.append(bench("bench"))
    out.append(stall("stall_0"))
    out.append(stall("stall_1", cloth=("#3f6fb0", "#f4ead2")))
    out.append(rubble("rubble"))
    for biome, kinds in DECOR_KINDS.items():
        for kind in kinds:
            for v in range(2):
                out.append(decor(biome, kind, v))
    for name, roof, wall, style, roof_style, extras in HOUSES:
        out.append(house2(name, roof, wall, style, roof_style, extras=extras))
    return out
