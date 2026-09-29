"""Tiles isométricos de chão para cada bioma (losango 200x100 + barranco)."""
import random
import zlib
from .lib import Svg, INK, shade, mix, f, diamond, inside_diamond, blade_tuft

W, TOP, SKIRT = 200, 100, 36
H = TOP + SKIRT + 2
CX, CY = 100, 50

BIOMES = {
    "village": dict(grass=("#96cc5a", "#72ad3e"), tuft="#4f8a2c", dirt="#9c6b3e", path="cobble", water=("#5bb4e6", "#2f86c2"),
                    flowers=["#ffffff", "#ffd84d", "#ff8fb4", "#b9a0ff"]),
    "meadow": dict(grass=("#8fc653", "#6aa53a"), tuft="#4a8429", dirt="#9a693c", path="dirt", water=("#56b0e3", "#2c7fbd"),
                   flowers=["#ffffff", "#ffe066", "#ff9ec0"]),
    "forest": dict(grass=("#5e9d3f", "#437f2f"), tuft="#2d5e22", dirt="#7c5230", path="darkdirt", water=("#3f95c9", "#246aa0"),
                   flowers=["#fff3c4", "#c8e6ff"]),
    "swamp": dict(grass=("#86994b", "#627a38"), tuft="#3f5626", dirt="#5e4a33", path="mud", water=("#6b9a73", "#3d6b52"),
                  flowers=["#e6d27a", "#c3a5e8"]),
    "ruins": dict(grass=("#b9bb6d", "#949a52"), tuft="#737a36", dirt="#8a7552", path="slab", water=("#6fb0c9", "#3f84a3"),
                  flowers=["#f0e2b0", "#e89a6a"]),
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
    s.path(top, mix(g0, g1, 0.45), stroke=None)
    _blotches(s, rng, top, [shade(g1, -0.04), shade(g0, 0.05), g1, g0], n=7, blur=10, opacity=0.65)
    nz = s.noise_filter(freq=0.07, octaves=3, seed=seed % 97, amount=0.16, color=shade(g1, -0.22))
    s.path(top, "#000", stroke=None, extra=f'filter="url(#{nz})"')
    cid = s.clip(f'<path d="{top}"/>')
    s.begin(f'clip-path="url(#{cid})"')
    # trevos e folhinhas
    for _ in range(4):
        x, y = rng.uniform(20, 180), rng.uniform(15, 85)
        for k in range(3):
            a = k / 3 * math.tau + rng.uniform(0, 1)
            s.ellipse(x + math.cos(a) * 3, y + math.sin(a) * 2, 3, 2.2, shade(g0, -0.06), stroke=shade(g1, -0.18), sw=0.8)
    # tufos
    for _ in range(16 + variant * 3):
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
    return out
