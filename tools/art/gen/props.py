"""Cenário: árvores, pedras, arbustos, casas, cercas, poço e colunas."""
import math
import random
import zlib
from .lib import Svg, INK, shade, mix, f, diamond

PPU = 200


def cloud(cx, cy, r, bumps, rng, squash=0.88, jitter=0.12):
    """Contorno de "nuvem" (copa de árvore, arbusto): círculo com gomos."""
    pts = []
    off = rng.uniform(0, math.tau)
    for i in range(bumps):
        a = off + i / bumps * math.tau
        rr = r * (1 + rng.uniform(-jitter, jitter))
        pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr * squash))
    d = f"M{f(pts[0][0])} {f(pts[0][1])}"
    for i in range(bumps):
        x1, y1 = pts[i]
        x2, y2 = pts[(i + 1) % bumps]
        chord = math.hypot(x2 - x1, y2 - y1)
        d += f" A{f(chord * 0.58)} {f(chord * 0.58)} 0 0 1 {f(x2)} {f(y2)}"
    return d + " Z"


def foliage(s, clumps, pal, rng, dots=None):
    """Copa em camadas: contorno único, preenchimento com gradiente, brilho no topo-esquerda."""
    dark, mid, light = pal
    paths = [cloud(x, y, r, max(9, int(r / 4)), rng, jitter=0.2) for x, y, r in clumps]
    for d in paths:
        s.path(d, "#1f2a14", stroke="#1f2a14", sw=4)
    grad = s.lin(mid, dark, 0.2, 0.1, 0.7, 1)
    for d in paths:
        s.path(d, grad, stroke=None)
    # brilho de cada gomo
    for (x, y, r), d in zip(clumps, paths):
        cid = s.clip(f'<path d="{d}"/>')
        s.add(f'<g clip-path="url(#{cid})">'
              f'<path d="{cloud(x - r * 0.22, y - r * 0.28, r * 0.72, max(8, int(r / 5)), rng, jitter=0.2)}" fill="{light}" opacity="0.55"/>'
              f'<path d="{cloud(x - r * 0.3, y - r * 0.42, r * 0.36, 7, rng, jitter=0.2)}" fill="{shade(light, 0.08)}" opacity="0.45"/>'
              f'</g>')
    # sombra interna embaixo, linhas de gomo
    for (x, y, r), d in zip(clumps, paths):
        if r > 40:
            s.path(f"M{f(x - r * 0.35)} {f(y + r * 0.5)} Q{f(x)} {f(y + r * 0.66)} {f(x + r * 0.4)} {f(y + r * 0.45)}", "none", stroke=shade(dark, -0.15), sw=2, extra='opacity="0.45"')
    if dots:
        for _ in range(dots[1]):
            x, y, r = rng.choice(clumps)
            a, rr = rng.uniform(0, math.tau), rng.uniform(0, r * 0.75)
            s.circle(x + math.cos(a) * rr, y + math.sin(a) * rr * 0.85, rng.uniform(3, 4.5), dots[0], stroke=INK, sw=1.4)


def trunk(s, x, base, top, w, color, rng, lean=0.0):
    """Tronco com raízes e riscos de casca."""
    tx = x + lean
    d = (f"M{f(x - w * 1.5)} {f(base)} Q{f(x - w * 0.7)} {f(base - 10)} {f(x - w * 0.7)} {f(base - 26)} "
         f"L{f(tx - w * 0.5)} {f(top)} L{f(tx + w * 0.5)} {f(top)} L{f(x + w * 0.7)} {f(base - 26)} "
         f"Q{f(x + w * 0.7)} {f(base - 10)} {f(x + w * 1.6)} {f(base)} Q{f(x)} {f(base + 6)} {f(x - w * 1.5)} {f(base)} Z")
    s.path(d, s.lin(shade(color, 0.08), shade(color, -0.18), 0, 0, 1, 0), stroke=INK, sw=3)
    for k in range(3):
        yy = base - 30 - k * (base - top - 40) / 3
        s.path(f"M{f(x - w * 0.3 + rng.uniform(-2, 2))} {f(yy)} q{f(rng.uniform(-3, 3))} -12 {f(rng.uniform(-2, 2))} -24", "none", stroke=shade(color, -0.3), sw=2)


def ground_shadow(s, cx, cy, rx, ry, opacity=0.3):
    bl = s.soft_filter(5)
    s.ellipse(cx, cy, rx, ry, "#000000", stroke=None, extra=f'opacity="{opacity}" filter="url(#{bl})"')


def tree_round(name, pal, seed, blossom=None, size=1.0):
    rng = random.Random(seed)
    W, H = 260, 360
    s = Svg(name, W, H, pivot=(130, 24), kind="prop")
    base = H - 24
    ground_shadow(s, 130, base, 70 * size, 20)
    trunk(s, 130, base, base - 150 * size, 13 * size, "#7a4a28", rng, lean=rng.uniform(-6, 6))
    cy = base - 190 * size
    clumps = [
        (130 + rng.uniform(-6, 6), cy + 10, 74 * size),
        (74, cy + 34, 44 * size), (188, cy + 30, 44 * size),
        (96, cy - 36, 50 * size), (166, cy - 38, 48 * size),
        (130, cy - 76 * size, 42 * size), (112, cy + 44, 38 * size), (152, cy + 46, 36 * size),
    ]
    foliage(s, clumps, pal, rng, dots=blossom)
    return s


def tree_pine(name, pal, seed):
    rng = random.Random(seed)
    W, H = 220, 400
    s = Svg(name, W, H, pivot=(110, 24), kind="prop")
    base = H - 24
    ground_shadow(s, 110, base, 60, 18)
    trunk(s, 110, base, base - 70, 11, "#6b4226", rng)
    dark, mid, light = pal
    layers = [(base - 60, 92), (base - 130, 78), (base - 196, 62), (base - 256, 46), (base - 306, 30)]
    for i, (y, hw) in enumerate(layers):
        top = y - hw * 1.25
        teeth = 6
        d = f"M110 {f(top)} "
        for k in range(teeth + 1):
            t = k / teeth
            x = 110 + hw * t
            yy = top + (y - top) * t + (8 if k % 2 else 0)
            d += f"L{f(x)} {f(yy)} "
        d += f"Q110 {f(y + 14)} {f(110 - hw)} {f(y)} "
        for k in range(teeth, -1, -1):
            t = k / teeth
            d += f"L{f(110 - hw * t)} {f(top + (y - top) * t + (8 if k % 2 else 0))} "
        d += "Z"
        s.path(d, s.lin(mid, dark, 0, 0, 1, 1), stroke=INK, sw=3.2)
        cid = s.clip(f'<path d="{d}"/>')
        s.add(f'<g clip-path="url(#{cid})"><path d="M{f(110 - hw)} {f(y + 20)} L110 {f(top - 5)} L{f(110 - hw * 0.1)} {f(y + 20)} Z" fill="{light}" opacity="0.55"/></g>')
    if rng.random() < 0.5:
        for _ in range(4):
            x, y = rng.uniform(80, 140), rng.uniform(base - 280, base - 80)
            s.circle(x, y, 2.5, "#e8f4ff", stroke=None, extra='opacity="0.8"')
    return s


def tree_willow(name, seed):
    rng = random.Random(seed)
    W, H = 280, 360
    s = Svg(name, W, H, pivot=(140, 24), kind="prop")
    base = H - 24
    ground_shadow(s, 140, base, 80, 20)
    trunk(s, 140, base, base - 140, 15, "#5d4630", rng, lean=-8)
    pal = ("#3f5e2a", "#6b8a3c", "#9fbf5a")
    cy = base - 190
    foliage(s, [(140, cy, 80), (86, cy + 20, 50), (196, cy + 18, 52), (140, cy - 55, 50)], pal, rng)
    # galhos caídos
    for i in range(16):
        x = rng.uniform(62, 218)
        y0 = cy + rng.uniform(0, 40)
        L = rng.uniform(60, 120)
        d = f"M{f(x)} {f(y0)} Q{f(x + rng.uniform(-10, 10))} {f(y0 + L * 0.6)} {f(x + rng.uniform(-6, 6))} {f(y0 + L)}"
        s.add(f'<path d="{d}" fill="none" stroke="{INK}" stroke-width="7" stroke-linecap="round"/>')
        s.add(f'<path d="{d}" fill="none" stroke="{rng.choice(pal[1:])}" stroke-width="4" stroke-linecap="round"/>')
    return s


def branches(s, x, y, angle, length, width, depth, rng, color):
    if depth == 0 or length < 10:
        return
    x2 = x + math.cos(angle) * length
    y2 = y + math.sin(angle) * length
    s.limb([(x, y), (x2, y2)], width, color, ow=2.5, highlight=False)
    for da in (-0.5, 0.45):
        branches(s, x2, y2, angle + da + rng.uniform(-0.2, 0.2), length * rng.uniform(0.6, 0.75), width * 0.68, depth - 1, rng, color)


def tree_dead(name, seed, leaves=None):
    rng = random.Random(seed)
    W, H = 240, 340
    s = Svg(name, W, H, pivot=(120, 24), kind="prop")
    base = H - 24
    ground_shadow(s, 120, base, 55, 16)
    color = "#7d6a55" if not leaves else "#6d4c2e"
    trunk(s, 120, base, base - 110, 13, color, rng)
    branches(s, 120, base - 108, -math.pi / 2 + rng.uniform(-0.2, 0.2), 70, 16, 4, rng, color)
    if leaves:
        pal, n = leaves
        for _ in range(n):
            x, y = rng.uniform(40, 200), rng.uniform(40, 200)
            s.path(cloud(x, y, rng.uniform(16, 26), 7, rng), s.lin(pal[1], pal[0]), stroke=INK, sw=2.5)
    return s


def rock(name, seed, moss=False):
    rng = random.Random(seed)
    W, H = 160, 130
    s = Svg(name, W, H, pivot=(80, 22), kind="prop")
    base = H - 22
    ground_shadow(s, 80, base, 58, 14)
    pts = []
    n = 9
    for i in range(n):
        a = math.pi + i / (n - 1) * math.pi
        r = rng.uniform(42, 58)
        pts.append((80 + math.cos(a) * r * 1.15, base - 8 + math.sin(a) * r * 1.2))
    d = f"M{f(pts[0][0])} {f(base)} " + " ".join(f"L{f(x)} {f(y)}" for x, y in pts) + f" L{f(pts[-1][0])} {f(base)} Q80 {f(base + 10)} {f(pts[0][0])} {f(base)} Z"
    grey = "#9b968e"
    s.path(d, s.lin(shade(grey, 0.12), shade(grey, -0.2), 0.2, 0, 0.8, 1), stroke=INK, sw=3.2)
    cid = s.clip(f'<path d="{d}"/>')
    top_x, top_y = min(pts, key=lambda p: p[1])
    s.add(f'<g clip-path="url(#{cid})">'
          f'<path d="M{f(pts[0][0])} {f(base - 20)} L{f(top_x - 10)} {f(top_y + 20)} L{f(top_x + 20)} {f(top_y + 8)} L{f(pts[-1][0])} {f(base - 34)} L{f(pts[-1][0] + 10)} {f(base - 70)} L{f(pts[0][0] - 10)} {f(base - 90)} Z" fill="#ffffff" opacity="0.2"/>'
          f'<path d="M{f(top_x + 18)} {f(top_y + 10)} L{f(pts[-1][0] + 4)} {f(base - 30)} L{f(pts[-1][0] + 20)} {f(base + 10)} L{f(top_x + 60)} {f(base + 10)} Z" fill="#000000" opacity="0.18"/>'
          f'</g>')
    s.path(f"M{f(top_x + 4)} {f(top_y + 18)} l10 18 l-6 14", "none", stroke=shade(grey, -0.4), sw=2)
    if moss:
        for _ in range(3):
            x = rng.uniform(40, 120)
            s.path(cloud(x, top_y + rng.uniform(8, 26), rng.uniform(10, 16), 6, rng, squash=0.6), s.lin("#8fbf4a", "#5e8f30"), stroke=INK, sw=2)
    return s


def bush(name, kind, seed):
    rng = random.Random(seed)
    W, H = 170, 140
    s = Svg(name, W, H, pivot=(85, 20), kind="prop")
    base = H - 20
    ground_shadow(s, 85, base, 56, 14)
    if kind in ("berry", "flower"):
        pal = ("#34502a", "#557d3c", "#93b35c")
        foliage(s, [(60, base - 34, 30), (108, base - 36, 32), (84, base - 58, 34)], pal, rng,
                dots=(("#e8453c" if seed % 2 == 0 else "#6f5bd6") if kind == "berry" else ("#ff9ec8" if seed % 2 == 0 else "#fff4a3"), 9))
    elif kind == "fern":
        for i in range(9):
            a = math.pi + 0.3 + i / 8 * (math.pi - 0.6)
            L = rng.uniform(50, 70)
            x2, y2 = 85 + math.cos(a) * L, base - 8 + math.sin(a) * L * 0.9
            d = f"M85 {base - 8} Q{f((85 + x2) / 2)} {f(min(base - 8, y2) - 20)} {f(x2)} {f(y2)}"
            s.add(f'<path d="{d}" fill="none" stroke="{INK}" stroke-width="10" stroke-linecap="round"/>')
            s.add(f'<path d="{d}" fill="none" stroke="{"#4f8a2c" if i % 2 else "#6aa83c"}" stroke-width="6" stroke-linecap="round"/>')
            for k in range(1, 6):
                t = k / 6
                px = 85 + (x2 - 85) * t
                py = (base - 8) + (y2 - (base - 8)) * t - math.sin(t * math.pi) * 14
                s.ellipse(px, py - 3, 5, 2.5, "#7fbf45", stroke=INK, sw=1, extra=f'transform="rotate({f(math.degrees(a) + 90)} {f(px)} {f(py - 3)})"')
    elif kind == "reeds":
        for i in range(11):
            x = rng.uniform(45, 125)
            h = rng.uniform(55, 95)
            lean = rng.uniform(-10, 10)
            d = f"M{f(x)} {base - 4} Q{f(x + lean * 0.3)} {f(base - h * 0.6)} {f(x + lean)} {f(base - h)}"
            s.add(f'<path d="{d}" fill="none" stroke="{INK}" stroke-width="6" stroke-linecap="round"/>')
            s.add(f'<path d="{d}" fill="none" stroke="#7a9a3c" stroke-width="3" stroke-linecap="round"/>')
            if i % 3 == 0:
                s.ellipse(x + lean * 0.9, base - h + 6, 4.5, 11, "#7a4a2a", stroke=INK, sw=2)
    else:  # seco
        for i in range(8):
            a = math.pi + 0.4 + i / 7 * (math.pi - 0.8)
            L = rng.uniform(40, 62)
            s.limb([(85, base - 6), (85 + math.cos(a) * L, base - 6 + math.sin(a) * L)], 4, "#9a7b52", ow=2, highlight=False)
        for _ in range(6):
            x, y = rng.uniform(40, 130), rng.uniform(base - 60, base - 25)
            s.ellipse(x, y, 6, 4, rng.choice(["#b9a24f", "#c98a3a", "#a7b05a"]), stroke=INK, sw=1.5)
    return s


# ---------------------------------------------------------------------- construções isométricas

def iso_pt(ox, oy, u, v, z=0.0):
    """Ponto em coordenadas de célula (u, v) e altura z (px) para a tela."""
    return (ox + (u - v) * 100, oy + (u + v) * 50 - z)


def poly(pts):
    return "M" + " L".join(f"{f(x)} {f(y)}" for x, y in pts) + " Z"


def house(name, roof_color, wall="#f1e3c4", seed=0):
    rng = random.Random(seed)
    W, H = 420, 520
    ox, oy = 210, H - 220  # vértice de trás do terreno 2x2
    s = Svg(name, W, H, pivot=(210, 120), kind="prop")  # pivô = centro do terreno 2x2 (u=v=1)
    P = lambda u, v, z=0: iso_pt(ox, oy, u, v, z)
    a, b = 0.3, 1.7  # recuo das paredes
    wall_h, ridge = 150, 250
    ground_shadow(s, *P(1.05, 1.05), 190, 60, opacity=0.25)
    # paredes
    left = poly([P(a, b), P(b, b), P(b, b, wall_h), P(a, b, wall_h)])
    right = poly([P(b, b), P(b, a), P(b, a, wall_h), P(b, b, wall_h)])
    s.path(left, s.lin(shade(wall, 0.04), shade(wall, -0.08), 0, 0, 1, 0), stroke=INK, sw=3)
    s.path(right, s.lin(shade(wall, -0.12), shade(wall, -0.2), 0, 0, 1, 0), stroke=INK, sw=3)
    # base de pedra
    s.path(poly([P(a, b), P(b, b), P(b, b, 22), P(a, b, 22)]), "#a39a8a", stroke=INK, sw=2.5)
    s.path(poly([P(b, b), P(b, a), P(b, a, 22), P(b, b, 22)]), "#8a8274", stroke=INK, sw=2.5)
    # vigas de madeira (enxaimel)
    beam = "#6b4226"
    for u in (a, 0.95, 1.4, b):
        s.limb([P(u, b, 22), P(u, b, wall_h)], 6, beam, ow=1.5, highlight=False)
    for v in (a, 1.0, b):
        s.limb([P(b, v, 22), P(b, v, wall_h)], 6, beam, ow=1.5, highlight=False)
    s.limb([P(a, b, wall_h * 0.62), P(b, b, wall_h * 0.62)], 5, beam, ow=1.5, highlight=False)
    # porta (parede da esquerda)
    du0, du1 = 1.02, 1.32
    door = poly([P(du0, b, 22), P(du1, b, 22), P(du1, b, 92), P(du0, b, 92)])
    s.path(door, s.lin("#8a5530", "#5e3a20", 0, 0, 1, 0), stroke=INK, sw=3)
    mx, my = P((du0 + du1) / 2, b, 92)
    s.circle(*P(du1 - 0.05, b, 56), 3, "#e8c14a", stroke=INK, sw=1.2)
    # janelas com luz
    def window(p0, p1, z0, z1):
        w = poly([p0(z0), p1(z0), p1(z1), p0(z1)])
        s.path(w, s.lin("#ffe9a3", "#f2b64a"), stroke=INK, sw=3)
        c0 = p0((z0 + z1) / 2)
        c1 = p1((z0 + z1) / 2)
        s.path(f"M{f(c0[0])} {f(c0[1])} L{f(c1[0])} {f(c1[1])}", "none", stroke=beam, sw=2.5)
    window(lambda z: P(0.5, b, z), lambda z: P(0.8, b, z), 50, 88)
    window(lambda z: P(b, 0.55, z), lambda z: P(b, 0.9, z), 50, 88)
    window(lambda z: P(b, 1.15, z), lambda z: P(b, 1.45, z), 50, 88)
    # telhado: cumeeira ao longo de u, em v = 1
    oh = 0.18
    rz = wall_h + ridge - wall_h
    back = poly([P(a - oh, 1.0, ridge), P(b + oh, 1.0, ridge), P(b + oh, a - oh, wall_h - 8), P(a - oh, a - oh, wall_h - 8)])
    s.path(back, shade(roof_color, -0.22), stroke=INK, sw=3)
    gable = poly([P(b, b, wall_h), P(b, a, wall_h), P(b, 1.0, ridge - 6)])
    s.path(gable, s.lin(shade(wall, -0.1), shade(wall, -0.22)), stroke=INK, sw=3)
    s.circle(*P(b, 1.0, wall_h + 45), 9, "#5e3a20", stroke=INK, sw=2)
    front = poly([P(a - oh, 1.0, ridge), P(b + oh, 1.0, ridge), P(b + oh, b + oh, wall_h - 8), P(a - oh, b + oh, wall_h - 8)])
    s.path(front, s.lin(shade(roof_color, 0.1), shade(roof_color, -0.08), 0, 0, 0.3, 1), stroke=INK, sw=3.2)
    # fileiras de telhas
    for k in range(1, 5):
        t = k / 5
        v = 1.0 + (b + oh - 1.0) * t
        z = ridge + (wall_h - 8 - ridge) * t
        p0, p1 = P(a - oh, v, z), P(b + oh, v, z)
        s.path(f"M{f(p0[0])} {f(p0[1])} L{f(p1[0])} {f(p1[1])}", "none", stroke=shade(roof_color, -0.3), sw=2)
    # beiral da frente do telhado (espessura)
    s.path(poly([P(a - oh, b + oh, wall_h - 8), P(b + oh, b + oh, wall_h - 8), P(b + oh, b + oh, wall_h - 18), P(a - oh, b + oh, wall_h - 18)]), shade(roof_color, -0.3), stroke=INK, sw=2.5)
    s.path(poly([P(b + oh, b + oh, wall_h - 8), P(b + oh, 1.0, ridge), P(b + oh, 1.0, ridge - 10), P(b + oh, b + oh, wall_h - 18)]), shade(roof_color, -0.35), stroke=INK, sw=2.5)
    # chaminé
    cu, cv = 0.7, 0.72
    chim = [P(cu, cv, ridge - 20), P(cu + 0.18, cv, ridge - 30), P(cu + 0.18, cv, ridge + 40), P(cu, cv, ridge + 50)]
    s.path(poly(chim), s.lin("#b0664a", "#7e4432", 0, 0, 1, 0), stroke=INK, sw=2.5)
    s.path(poly([P(cu, cv, ridge + 50), P(cu + 0.18, cv, ridge + 40), P(cu + 0.18, cv - 0.18, ridge + 40), P(cu, cv - 0.18, ridge + 50)]), "#5a3226", stroke=INK, sw=2)
    return s


def fence(name, along_x):
    W, H = 220, 150
    cx, cy = 110, H - 40
    s = Svg(name, W, H, pivot=(110, 40), kind="prop")
    wood = "#9a6a3c"
    if along_x:
        p0, p1 = (cx - 50, cy - 25), (cx + 50, cy + 25)
    else:
        p0, p1 = (cx + 50, cy - 25), (cx - 50, cy + 25)
    ground_shadow(s, cx, cy + 4, 60, 14, 0.2)
    for z in (28, 55):
        s.limb([(p0[0], p0[1] - z), (p1[0], p1[1] - z)], 7, wood, ow=2.2)
    for p in (p0, ((p0[0] + p1[0]) / 2, (p0[1] + p1[1]) / 2), p1):
        s.path(f"M{f(p[0] - 6)} {f(p[1])} L{f(p[0] - 6)} {f(p[1] - 66)} L{f(p[0])} {f(p[1] - 74)} L{f(p[0] + 6)} {f(p[1] - 66)} L{f(p[0] + 6)} {f(p[1])} Z",
               s.lin(shade(wood, 0.1), shade(wood, -0.2), 0, 0, 1, 0), stroke=INK, sw=2.5)
    return s


def well(name):
    W, H = 220, 290
    cx, base = 110, H - 40
    s = Svg(name, W, H, pivot=(110, 40), kind="prop")
    ground_shadow(s, cx, base, 80, 22)
    stone = "#a8a196"
    # corpo cilíndrico
    s.path(f"M{cx - 66} {base - 60} L{cx - 66} {base} A66 30 0 0 0 {cx + 66} {base} L{cx + 66} {base - 60} Z", s.lin(shade(stone, 0.05), shade(stone, -0.22), 0, 0, 1, 0), stroke=INK, sw=3)
    for k in range(3):
        y = base - 15 - k * 20
        s.path(f"M{cx - 66} {y} A66 30 0 0 0 {cx + 66} {y}", "none", stroke=shade(stone, -0.35), sw=1.8)
    s.ellipse(cx, base - 60, 66, 30, shade(stone, 0.08), stroke=INK, sw=3)
    s.ellipse(cx, base - 60, 50, 21, "#1e3a4a", stroke=INK, sw=2.5)
    s.ellipse(cx - 8, base - 64, 20, 6, "#4a8ab0", stroke=None, extra='opacity="0.6"')
    # postes e telhadinho
    for dx in (-58, 58):
        s.limb([(cx + dx, base - 55), (cx + dx, base - 175)], 9, "#7a4a28", ow=2.5)
    s.limb([(cx - 58, base - 150), (cx + 58, base - 150)], 6, "#6b4226", ow=2)
    s.path(f"M{cx} {base - 150} L{cx} {base - 105}", "none", stroke="#d8c8a0", sw=2)
    s.path(f"M{cx - 12} {base - 105} L{cx + 12} {base - 105} L{cx + 9} {base - 85} L{cx - 9} {base - 85} Z", "#8a5a30", stroke=INK, sw=2)
    roof = f"M{cx - 88} {base - 165} L{cx} {base - 215} L{cx + 88} {base - 165} L{cx + 78} {base - 155} L{cx} {base - 200} L{cx - 78} {base - 155} Z"
    s.path(f"M{cx - 88} {base - 165} L{cx} {base - 222} L{cx + 88} {base - 165} L{cx} {base - 140} Z", s.lin("#c65a3a", "#8e3a24"), stroke=INK, sw=3)
    s.path(f"M{cx} {base - 222} L{cx} {base - 140}", "none", stroke=INK, sw=2)
    return s


def pillar(name, broken):
    W, H = 150, 300 if not broken else 200
    cx, base = 75, H - 30
    s = Svg(name, W, H, pivot=(75, 30), kind="prop")
    ground_shadow(s, cx, base, 52, 14)
    stone = "#cfc7b2"
    s.path(f"M{cx - 44} {base} L{cx - 44} {base - 22} L{cx + 44} {base - 22} L{cx + 44} {base} Q{cx} {base + 14} {cx - 44} {base} Z", s.lin(shade(stone, -0.05), shade(stone, -0.25), 0, 0, 1, 0), stroke=INK, sw=3)
    s.ellipse(cx, base - 22, 44, 12, shade(stone, 0.05), stroke=INK, sw=2.5)
    top = base - (220 if not broken else 110)
    jag = f"L{cx + 30} {top + 8} L{cx + 12} {top - 6} L{cx - 4} {top + 10} L{cx - 18} {top - 2} L{cx - 30} {top + 12}" if broken else f"L{cx + 30} {top} L{cx - 30} {top}"
    shaft = f"M{cx - 30} {base - 24} L{cx + 30} {base - 24} {jag} Z"
    s.path(shaft, s.lin(shade(stone, 0.12), shade(stone, -0.22), 0, 0, 1, 0), stroke=INK, sw=3)
    for dx in (-16, 0, 16):
        s.path(f"M{cx + dx} {base - 28} L{cx + dx} {top + 16}", "none", stroke=shade(stone, -0.3), sw=2)
    if not broken:
        s.path(f"M{cx - 40} {top} L{cx + 40} {top} L{cx + 40} {top - 18} L{cx - 40} {top - 18} Z", s.lin(shade(stone, 0.1), shade(stone, -0.2)), stroke=INK, sw=3)
    for _ in range(3):
        s.path(cloud(cx + random.uniform(-25, 25), base - random.uniform(30, 120), 9, 6, random.Random(len(name))), "#7fae45", stroke=INK, sw=1.8)
    return s


def build():
    out = []
    greens = ("#2e4a22", "#4d7434", "#8fae55")
    out.append(tree_round("tree_meadow_0", greens, 1))
    out.append(tree_round("tree_meadow_1", ("#34502a", "#557d3c", "#98b862"), 2, size=0.92))
    out.append(tree_round("tree_meadow_2", ("#6b3a18", "#b06a2a", "#e0a551"), 3))
    out.append(tree_pine("tree_forest_0", ("#1c3d2a", "#2f6b45", "#5ea36a"), 4))
    out.append(tree_pine("tree_forest_1", ("#1a3a30", "#2a5f4c", "#4f9577"), 5))
    out.append(tree_round("tree_forest_2", ("#22361a", "#3a5a2a", "#6e8f46"), 6, size=1.05))
    out.append(tree_willow("tree_swamp_0", 7))
    out.append(tree_dead("tree_swamp_1", 8, leaves=(("#4f6b2a", "#7a9a3c"), 5)))
    out.append(tree_dead("tree_ruins_0", 9))
    out.append(tree_dead("tree_ruins_1", 10, leaves=(("#8a7a2a", "#c9b050"), 7)))
    out.append(tree_round("tree_village_0", ("#2e4a22", "#4f7a36", "#94b45a"), 11, blossom=("#d9583f", 5)))
    out.append(tree_round("tree_village_1", ("#8a4a5e", "#c98aa0", "#efc9d4"), 12, blossom=("#fff4ec", 6)))
    out.append(rock("rock_0", 20))
    out.append(rock("rock_1", 21))
    out.append(rock("rock_moss_0", 22, moss=True))
    out.append(rock("rock_moss_1", 23, moss=True))
    for biome, kind in (("meadow", "berry"), ("village", "flower"), ("forest", "fern"), ("swamp", "reeds"), ("ruins", "dry")):
        for v in range(2):
            out.append(bush(f"bush_{biome}_{v}", kind, zlib.crc32(f"{biome}{v}".encode()) % 1000 + v))
    out.append(fence("fence_x", True))
    out.append(fence("fence_y", False))
    out.append(well("well"))
    out.append(pillar("pillar_0", False))
    out.append(pillar("pillar_1", True))
    from .scenery import build as more  # espécies extras, objetos de vila, detalhes de chão e casas
    return out + more()
