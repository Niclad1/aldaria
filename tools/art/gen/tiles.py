"""Tiles isométricos de chão para cada bioma (losango 200x100 + barranco)."""
import random
import zlib
from .lib import Svg, INK, shade, mix, f, diamond, inside_diamond, blade_tuft

W, TOP, SKIRT = 200, 100, 36
H = TOP + SKIRT + 2
CX, CY = 100, 50

BIOMES = {
    "village": dict(grass=("#b3c777", "#9db565"), tuft="#6f8c3f", dirt="#9c7a52", path="flag", water=("#6fb7d8", "#3f8cb8"),
                    flowers=["#fff6d8", "#ffd86b", "#f5a3b8", "#c9b5ff"]),
    "meadow": dict(grass=("#a9c26c", "#90ad5a"), tuft="#667f38", dirt="#957250", path="dirt", water=("#6ab3d6", "#3a86b3"),
                   flowers=["#fff6d8", "#ffe07a", "#f5a3b8"]),
    "forest": dict(grass=("#7f9d52", "#678845"), tuft="#425e2c", dirt="#7a5a3c", path="darkdirt", water=("#4f97bf", "#2f6f99"),
                   flowers=["#f6ecc8", "#cfe3f0"]),
    "swamp": dict(grass=("#909b62", "#77834f"), tuft="#4c5731", dirt="#5f4d38", path="mud", water=("#789d7c", "#4d7058"),
                  flowers=["#e8d98a", "#cdb6e6"]),
    "ruins": dict(grass=("#c6bd83", "#aca36b"), tuft="#857f45", dirt="#8d7a58", path="slab", water=("#79b3c8", "#4b88a2"),
                  flowers=["#f3e6bb", "#e6a07a"]),
}


def _skirt(s, b, rng):
    dirt = b["dirt"]
    left = f"M0 {CY} L{CX} {TOP} L{CX} {TOP + SKIRT} L0 {CY + SKIRT} Z"
    right = f"M{CX} {TOP} L{W} {CY} L{W} {CY + SKIRT} L{CX} {TOP + SKIRT} Z"
    s.path(left, s.lin(shade(dirt, 0.06), shade(dirt, -0.1)), stroke=INK, sw=2)
    s.path(right, s.lin(shade(dirt, -0.1), shade(dirt, -0.22)), stroke=INK, sw=2)
    # camadas de terra e pedrinhas
    for k in (14, 25):
        s.path(f"M2 {CY + k} Q{CX * 0.5} {CY + k + 25 + rng.uniform(-3, 3)} {CX - 2} {TOP + k - 2}", "none", stroke=shade(dirt, -0.2), sw=2)
        s.path(f"M{CX + 2} {TOP + k - 2} Q{CX * 1.5} {CY + k + 25 + rng.uniform(-3, 3)} {W - 2} {CY + k}", "none", stroke=shade(dirt, -0.3), sw=2)
    for _ in range(5):
        x = rng.uniform(8, W - 8)
        top = CY + (x if x < CX else W - x) * 0.5
        y = top + rng.uniform(8, SKIRT - 6)
        s.ellipse(x, y, rng.uniform(2.5, 4.5), rng.uniform(1.8, 3), shade(dirt, 0.15 if x < CX else 0.02), stroke=shade(dirt, -0.35), sw=1.2)


def _grass_lip(s, b, rng):
    """Grama que "escorre" sobre a borda do barranco (só aparece nas bordas do mapa)."""
    g = b["grass"][1]
    pts = []
    for i in range(0, 21):
        x = i * W / 20
        y = CY + (x if x <= CX else W - x) * 0.5
        pts.append((x, y + (5 + rng.uniform(0, 5) if 0 < i < 20 else 0)))
    d = f"M0 {CY} L{CX} {TOP} L{W} {CY} " + " ".join(f"L{f(x)} {f(y)}" for x, y in reversed(pts)) + " Z"
    s.path(d, shade(g, -0.08), stroke=INK, sw=2)


def _blotches(s, rng, top, colors, n=6, blur=9, opacity=0.5):
    """Manchas desfocadas que quebram a repetição e dão cara de pintura."""
    cid = s.clip(f'<path d="{top}"/>')
    bl = s.soft_filter(blur)
    s.begin(f'clip-path="url(#{cid})"')
    s.begin(f'filter="url(#{bl})" opacity="{opacity}"')
    for _ in range(n):
        x, y = rng.uniform(10, 190), rng.uniform(5, 95)
        s.ellipse(x, y, rng.uniform(18, 40), rng.uniform(9, 18), rng.choice(colors), stroke=None)
    s.end()
    s.end()


def _surface_grass(s, b, seed, variant, flowers):
    import math
    rng = random.Random(seed)
    top = diamond(CX, CY, W, TOP, grow=1.2)
    g0, g1 = b["grass"]
    s.path(top, mix(g0, g1, 0.5), stroke=None)
    _blotches(s, rng, top, [g1, g0], n=4, blur=14, opacity=0.25)
    nz = s.noise_filter(freq=0.09, octaves=2, seed=seed % 97, amount=0.1, color=shade(g1, -0.18))
    s.path(top, "#000", stroke=None, extra=f'filter="url(#{nz})"')
    cid = s.clip(f'<path d="{top}"/>')
    s.begin(f'clip-path="url(#{cid})"')
    # trevos e folhinhas
    for _ in range(1):
        x, y = rng.uniform(20, 180), rng.uniform(15, 85)
        for k in range(3):
            a = k / 3 * math.tau + rng.uniform(0, 1)
            s.ellipse(x + math.cos(a) * 3, y + math.sin(a) * 2, 3, 2.2, shade(g0, -0.06), stroke=shade(g1, -0.18), sw=0.8)
    # tufos
    for _ in range(5 + variant * 2):
        x, y = rng.uniform(8, 192), rng.uniform(4, 98)
        if inside_diamond(x, y, CX, CY, W, TOP, 0.04):
            s.add(blade_tuft(rng, x, y, rng.uniform(6, 11), b["tuft"], n=3, spread=3.5, sw=2))
            s.add(blade_tuft(rng, x + 1.5, y - 0.5, rng.uniform(4, 7), shade(g0, 0.14), n=2, spread=3, sw=1.3))
    s.end()
    if flowers:
        for _ in range(9):
            x, y = rng.uniform(20, 180), rng.uniform(14, 86)
            if not inside_diamond(x, y, CX, CY, W, TOP, 0.15):
                continue
            col = rng.choice(b["flowers"])
            s.add(blade_tuft(rng, x, y + 6, 8, b["tuft"], n=1, sw=1.6))
            for k in range(5):
                a = k / 5 * math.tau
                s.ellipse(x + math.cos(a) * 3.4, y + math.sin(a) * 2.3, 2.8, 2.1, col, stroke=shade(col, -0.4), sw=0.8)
            s.circle(x, y, 1.8, "#f5a623", stroke=None)
    if rng.random() < 0.3:
        x, y = rng.uniform(40, 160), rng.uniform(25, 75)
        if inside_diamond(x, y, CX, CY, W, TOP, 0.2):
            s.ellipse(x, y, rng.uniform(3, 5), rng.uniform(2, 3), "#cfc8b6", stroke=shade("#8f887a", -0.1), sw=1)


def _surface_path(s, b, seed, variant):
    rng = random.Random(seed)
    top = diamond(CX, CY, W, TOP, grow=1.2)
    kind = b["path"]
    if kind in ("dirt", "darkdirt", "mud"):
        base = {"dirt": ("#e2c48a", "#c9a263"), "darkdirt": ("#c29a64", "#9f7a4a"), "mud": ("#8e7652", "#6d5a3d")}[kind]
        s.path(top, mix(base[0], base[1], 0.5), stroke=None)
        _blotches(s, rng, top, [base[0], base[1], shade(base[1], -0.05)], n=6, blur=9, opacity=0.7)
        nz = s.noise_filter(freq=0.08, octaves=3, seed=seed % 97, amount=0.2, color=shade(base[1], -0.3))
        s.path(top, "#000", stroke=None, extra=f'filter="url(#{nz})"')
        for _ in range(7):
            x, y = rng.uniform(25, 175), rng.uniform(15, 85)
            if inside_diamond(x, y, CX, CY, W, TOP, 0.15):
                s.ellipse(x, y, rng.uniform(2.5, 5), rng.uniform(1.8, 3), shade(base[0], 0.12), stroke=shade(base[1], -0.3), sw=1)
        if kind == "mud":
            for _ in range(2):
                x, y = rng.uniform(50, 150), rng.uniform(30, 70)
                s.ellipse(x, y, rng.uniform(10, 16), rng.uniform(5, 7), "#4d4a3a", stroke=None, extra='opacity="0.45"')
                s.ellipse(x - 3, y - 1.5, 4, 1.4, "#ffffff", stroke=None, extra='opacity="0.25"')
        # tufos nas bordas, para misturar com a grama vizinha
        for _ in range(14):
            x, y = rng.uniform(4, 196), rng.uniform(2, 98)
            if inside_diamond(x, y, CX, CY, W, TOP, 0.0) and not inside_diamond(x, y, CX, CY, W, TOP, 0.22):
                s.add(blade_tuft(rng, x, y, rng.uniform(5, 9), b["tuft"], n=3, spread=3, sw=1.8))
    else:
        # pedras: calçamento da vila ou lajes das ruínas
        stone = ("#c8c3b6", "#a39d90") if kind == "cobble" else ("#b8b19c", "#8f8873")
        s.path(top, s.lin("#8b8474", "#6f695c"), stroke=None)
        cid = s.clip(f'<path d="{top}"/>')
        s.begin(f'clip-path="url(#{cid})"')
        step = 22 if kind == "cobble" else 40
        for gy in range(-2, 8):
            for gx in range(-2, 12):
                ox = (gy % 2) * step * 0.5
                x = gx * step + ox + rng.uniform(-2, 2)
                y = gy * step * 0.5 + rng.uniform(-1, 1)
                if not inside_diamond(x, y, CX, CY, W, TOP, -0.15):
                    continue
                if kind == "cobble":
                    rx, ry = step * 0.46 + rng.uniform(-1, 1), step * 0.24 + rng.uniform(-1, 1)
                    col = mix(stone[0], stone[1], rng.random())
                    s.ellipse(x, y, rx, ry, s.lin(shade(col, 0.08), shade(col, -0.1)), stroke=shade(stone[1], -0.32), sw=1.3)
                else:
                    hw, hh = step * 0.46, step * 0.23
                    col = mix(stone[0], stone[1], rng.random())
                    s.path(diamond(x, y, hw * 2, hh * 2), s.lin(shade(col, 0.06), shade(col, -0.1)), stroke=shade(stone[1], -0.35), sw=1.3)
                    if rng.random() < 0.3:
                        s.path(f"M{f(x - hw * 0.4)} {f(y - hh * 0.2)} L{f(x + hw * 0.1)} {f(y + hh * 0.3)}", "none", stroke=shade(stone[1], -0.35), sw=1)
        s.end()
        for _ in range(5):
            x, y = rng.uniform(20, 180), rng.uniform(10, 90)
            if inside_diamond(x, y, CX, CY, W, TOP, 0.1):
                s.add(blade_tuft(rng, x, y, rng.uniform(4, 7), b["tuft"], n=2, spread=3, sw=1.6))


def _surface_water(s, b, seed):
    rng = random.Random(seed)
    top = diamond(CX, CY, W, TOP, grow=1.2)
    s.path(top, mix(b["water"][0], b["water"][1], 0.45), stroke=None)
    _blotches(s, rng, top, [b["water"][0], b["water"][1]], n=5, blur=10, opacity=0.6)
    nz = s.noise_filter(freq=0.03, octaves=2, seed=seed % 97, amount=0.2, color=shade(b["water"][1], -0.2))
    s.path(top, "#000", stroke=None, extra=f'filter="url(#{nz})"')
    for _ in range(5):
        x, y = rng.uniform(40, 160), rng.uniform(20, 80)
        if inside_diamond(x, y, CX, CY, W, TOP, 0.2):
            w = rng.uniform(8, 16)
            s.path(f"M{f(x - w)} {f(y)} Q{f(x)} {f(y - 3)} {f(x + w)} {f(y)}", "none", stroke="#ffffff", sw=1.8, extra='stroke-opacity="0.55"')
    if b is BIOMES["swamp"]:
        for _ in range(2):
            x, y = rng.uniform(55, 145), rng.uniform(30, 70)
            s.ellipse(x, y, 9, 4.5, "#5f9a45", stroke=INK, sw=1.2)
            s.path(f"M{f(x)} {f(y)} L{f(x + 7)} {f(y - 3)}", "none", stroke=INK, sw=1)


def _blob(cx, cy, rx, ry, rng, n=18, jitter=0.07):
    """Forma arredondada com borda irregular (mancha de caminho)."""
    import math
    pts = []
    for i in range(n):
        a = i / n * math.tau
        # mistura de losango e elipse: pontas mais cheias nos 4 cantos do losango
        k = 1 + rng.uniform(-jitter, jitter)
        pts.append((cx + math.cos(a) * rx * k, cy + math.sin(a) * ry * k))
    d = f"M{f(pts[0][0])} {f(pts[0][1])}"
    for i in range(n):
        x1, y1 = pts[i]
        x2, y2 = pts[(i + 1) % n]
        mx, my = (x1 + x2) / 2, (y1 + y2) / 2
        d += f" Q{f(x1)} {f(y1)} {f(mx)} {f(my)}"
    return d + " Z"


def _squircle(cx, cy, a, bb, rng, p=1.45, n=64, jitter=0.025):
    """Losango arredondado: vizinhos em diagonal viram uma faixa reta, sem bolhas."""
    import math
    pts = []
    for i in range(n):
        t = i / n * math.tau
        c, s_ = math.cos(t), math.sin(t)
        k = 1 + rng.uniform(-jitter, jitter)
        x = cx + a * k * math.copysign(abs(c) ** (2 / p), c)
        y = cy + bb * k * math.copysign(abs(s_) ** (2 / p), s_)
        pts.append((x, y))
    return "M" + " L".join(f"{f(x)} {f(y)}" for x, y in pts) + " Z"


def path_decal(biome, b, v):
    """Caminho desenhado por cima da grama, maior que a célula: vizinhos se fundem numa trilha contínua."""
    DW, DH = 260, 140
    cx, cy = DW / 2, DH / 2
    name = f"decal_{biome}_path_{v}"
    s = Svg(name, DW, DH, pivot=(cx, DH - cy), kind="tile")
    seed = zlib.crc32(name.encode()) % 100000
    rng = random.Random(seed)
    shape = _squircle(cx, cy, 114, 57, rng)
    kind = b["path"]
    cid = s.clip(f'<path d="{shape}"/>')
    bl = s.soft_filter(1.2)
    if kind in ("dirt", "darkdirt", "mud"):
        base = {"dirt": ("#e2c48a", "#c9a263"), "darkdirt": ("#c29a64", "#9f7a4a"), "mud": ("#8e7652", "#6d5a3d")}[kind]
        s.path(shape, mix(base[0], base[1], 0.5), stroke=None, extra=f'filter="url(#{bl})"')
        s.begin(f'clip-path="url(#{cid})"')
        _blotches(s, rng, shape, [base[0], base[1], shade(base[1], -0.05)], n=6, blur=9, opacity=0.7)
        nz = s.noise_filter(freq=0.08, octaves=3, seed=seed % 97, amount=0.2, color=shade(base[1], -0.3))
        s.path(shape, "#000", stroke=None, extra=f'filter="url(#{nz})"')
        for _ in range(8):
            x, y = rng.uniform(40, 220), rng.uniform(25, 115)
            s.ellipse(x, y, rng.uniform(2.5, 5), rng.uniform(1.8, 3), shade(base[0], 0.12), stroke=shade(base[1], -0.3), sw=1)
        s.end()
    else:
        stone = {"cobble": ("#c8c3b6", "#a39d90"), "flag": ("#eadcb4", "#d6c496")}.get(kind, ("#b8b19c", "#8f8873"))
        s.path(shape, "#b9a77e" if kind == "flag" else "#857e6f", stroke=None, extra=f'filter="url(#{bl})"')
        s.begin(f'clip-path="url(#{cid})"')
        step = 22 if kind == "cobble" else 48 if kind == "flag" else 40
        for gy in range(-2, 10):
            for gx in range(-2, 16):
                x = gx * step + (gy % 2) * step * 0.5 + rng.uniform(-2, 2)
                y = gy * step * 0.5 + rng.uniform(-1, 1)
                col = mix(stone[0], stone[1], rng.random())
                if kind == "cobble":
                    s.ellipse(x, y, step * 0.46, step * 0.24, s.lin(shade(col, 0.08), shade(col, -0.1)), stroke=shade(stone[1], -0.32), sw=1.3)
                else:
                    s.path(diamond(x, y, step * 0.96, step * 0.48), s.lin(shade(col, 0.06), shade(col, -0.08)), stroke=shade(stone[1], -0.28), sw=1.2)
        s.end()
    # borda: sombra leve e tufos de grama invadindo o caminho
    s.path(shape, "none", stroke="#000000", sw=2, extra='stroke-opacity="0.08"')
    import math
    for i in range(16):
        a = i / 16 * math.tau + rng.uniform(-0.1, 0.1)
        x, y = cx + math.cos(a) * 100, cy + math.sin(a) * 52
        s.add(blade_tuft(rng, x, y + 2, rng.uniform(5, 9), b["tuft"], n=3, spread=3, sw=1.8))
    return s


PATCHES = {
    "village": ["shade", "sun", "flowers", "stones"],
    "meadow": ["shade", "sun", "flowers", "stones"],
    "forest": ["shade", "leaves", "sun", "stones"],
    "swamp": ["shade", "moss", "flowers", "stones"],
    "ruins": ["shade", "sand", "stones", "sun"],
}


def ground_patch(biome, b, kind, v):
    """Mancha grande e macia desenhada sobre o chão (cobre umas 3 células), para o chão não parecer tabuleiro."""
    import math
    PW, PH = 440, 240
    cx, cy = PW / 2, PH / 2
    name = f"patch_{biome}_{kind}_{v}"
    s = Svg(name, PW, PH, pivot=(cx, PH - cy), kind="tile")
    rng = random.Random(zlib.crc32(name.encode()))
    g0, g1 = b["grass"]
    blur = s.soft_filter(10)
    shape = _blob(cx, cy, rng.uniform(150, 190), rng.uniform(70, 90), rng, n=14, jitter=0.18)
    if kind in ("shade", "moss"):
        col = shade(g1, -0.07) if kind == "shade" else "#6f7d43"
        s.path(shape, col, stroke=None, extra=f'filter="url(#{blur})" opacity="0.75"')
        for _ in range(26):
            a, r = rng.uniform(0, math.tau), rng.uniform(0, 1) ** 0.5
            x, y = cx + math.cos(a) * r * 150, cy + math.sin(a) * r * 70
            s.add(blade_tuft(rng, x, y, rng.uniform(7, 12), shade(b["tuft"], -0.05), n=3, spread=3.5, sw=2))
            s.add(blade_tuft(rng, x + 2, y, rng.uniform(5, 8), shade(g0, 0.08), n=2, spread=3, sw=1.4))
    elif kind in ("sun", "sand"):
        col = shade(g0, 0.07) if kind == "sun" else "#d6c38f"
        s.path(shape, col, stroke=None, extra=f'filter="url(#{blur})" opacity="{0.55 if kind == "sun" else 0.8}"')
        for _ in range(10):
            a, r = rng.uniform(0, math.tau), rng.uniform(0, 1) ** 0.5
            s.add(blade_tuft(rng, cx + math.cos(a) * r * 150, cy + math.sin(a) * r * 70, rng.uniform(5, 8), b["tuft"], n=2, spread=3, sw=1.6))
    elif kind == "flowers":
        s.path(shape, shade(g0, 0.03), stroke=None, extra=f'filter="url(#{blur})" opacity="0.5"')
        for _ in range(34):
            a, r = rng.uniform(0, math.tau), rng.uniform(0, 1) ** 0.5
            x, y = cx + math.cos(a) * r * 150, cy + math.sin(a) * r * 70
            col = rng.choice(b["flowers"])
            s.add(blade_tuft(rng, x, y + 6, 8, b["tuft"], n=1, sw=1.5))
            for k in range(5):
                t = k / 5 * math.tau
                s.ellipse(x + math.cos(t) * 3, y + math.sin(t) * 2, 2.6, 2, col, stroke=shade(col, -0.35), sw=0.7)
            s.circle(x, y, 1.6, "#e8a23a", stroke=None)
    elif kind == "leaves":
        s.path(shape, "#8a7a45", stroke=None, extra=f'filter="url(#{blur})" opacity="0.35"')
        for _ in range(40):
            a, r = rng.uniform(0, math.tau), rng.uniform(0, 1) ** 0.5
            x, y = cx + math.cos(a) * r * 150, cy + math.sin(a) * r * 70
            col = rng.choice(["#c9892f", "#a8662a", "#d9a647", "#7f8f3a"])
            s.ellipse(x, y, 4.5, 2.4, col, stroke=shade(col, -0.35), sw=0.8, extra=f'transform="rotate({rng.randint(0, 180)} {f(x)} {f(y)})"')
    else:  # stones
        for _ in range(9):
            a, r = rng.uniform(0, math.tau), rng.uniform(0, 1) ** 0.5
            x, y = cx + math.cos(a) * r * 140, cy + math.sin(a) * r * 62
            rx = rng.uniform(4, 11)
            s.ellipse(x + 1, y + 2, rx, rx * 0.55, "#000000", stroke=None, extra='opacity="0.18"')
            s.ellipse(x, y, rx, rx * 0.6, s.lin("#d8d2c2", "#a39c8c"), stroke=shade("#8f887a", -0.25), sw=1.2)
        for _ in range(8):
            a, r = rng.uniform(0, math.tau), rng.uniform(0, 1) ** 0.5
            s.add(blade_tuft(rng, cx + math.cos(a) * r * 150, cy + math.sin(a) * r * 70, rng.uniform(5, 8), b["tuft"], n=3, spread=3, sw=1.6))
    return s


def build():
    out = []
    for biome, b in BIOMES.items():
        kinds = [("grass", v) for v in range(3)] + [("flowers", 0), ("path", 0), ("path", 1), ("water", 0)]
        for kind, v in kinds:
            name = f"tile_{biome}_{kind}_{v}"
            s = Svg(name, W, H, pivot=(CX, H - CY), kind="tile")
            seed = zlib.crc32(f"{biome}-{kind}-{v}".encode()) % 100000
            rng = random.Random(seed)
            _skirt(s, b, rng)
            if kind != "water":
                _grass_lip(s, b, rng)
            if kind in ("grass", "flowers"):
                _surface_grass(s, b, seed, v, kind == "flowers")
            elif kind == "path":
                _surface_path(s, b, seed, v)
            else:
                _surface_water(s, b, seed)
            out.append(s)
        for v in range(2):
            out.append(path_decal(biome, b, v))
        for kind in PATCHES[biome]:
            for v in range(2):
                out.append(ground_patch(biome, b, kind, v))
    return out
