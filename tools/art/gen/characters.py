"""Heróis (classes) e NPCs: bonecos chibi com cabeça grande, no estilo dos MMOs táticos 2D."""
import math
import random
from .lib import Svg, INK, shade, mix, f

PAD = 50
W, H = 220, 290 + PAD
FEET = H - 18 - PAD


def cel(s, d, base, bbox, light=0.1, dark=-0.1, sw=3.0, shadow=0.16, highlight=0.14, stroke=INK):
    """Forma com contorno + sombra (canto de baixo-direita) e brilho (canto de cima-esquerda)."""
    x0, y0, x1, y1 = bbox
    w, h = x1 - x0, y1 - y0
    s.path(d, s.lin(shade(base, light), shade(base, dark), 0.2, 0, 0.8, 1), stroke=stroke, sw=sw)
    cid = s.clip(f'<path d="{d}"/>')
    parts = []
    if shadow:
        mid = s.uid("mk")
        s.defs.append(f'<mask id="{mid}"><rect x="{f(x0 - 20)}" y="{f(y0 - 20)}" width="{f(w + 40)}" height="{f(h + 40)}" fill="white"/>'
                      f'<path d="{d}" fill="black" transform="translate({f(-w * 0.13)},{f(-h * 0.1)})"/></mask>')
        parts.append(f'<rect x="{f(x0 - 20)}" y="{f(y0 - 20)}" width="{f(w + 40)}" height="{f(h + 40)}" fill="{shade(base, -0.28)}" opacity="{shadow * 3:.2f}" mask="url(#{mid})"/>')
    if highlight:
        mid = s.uid("mk")
        s.defs.append(f'<mask id="{mid}"><rect x="{f(x0 - 20)}" y="{f(y0 - 20)}" width="{f(w + 40)}" height="{f(h + 40)}" fill="black"/>'
                      f'<path d="{d}" fill="white" transform="translate({f(-w * 0.09)},{f(-h * 0.1)}) scale(1)"/>'
                      f'<path d="{d}" fill="black" transform="translate({f(w * 0.02)},{f(h * 0.05)})"/></mask>')
        parts.append(f'<rect x="{f(x0 - 20)}" y="{f(y0 - 20)}" width="{f(w + 40)}" height="{f(h + 40)}" fill="#ffffff" opacity="{highlight * 2.5:.2f}" mask="url(#{mid})"/>')
    s.add(f'<g clip-path="url(#{cid})">{"".join(parts)}</g>')


def ell(cx, cy, rx, ry):
    return f"M{f(cx - rx)} {f(cy)} A{f(rx)} {f(ry)} 0 1 0 {f(cx + rx)} {f(cy)} A{f(rx)} {f(ry)} 0 1 0 {f(cx - rx)} {f(cy)} Z", (cx - rx, cy - ry, cx + rx, cy + ry)


def rrect(x, y, w, h, r):
    return (f"M{f(x + r)} {f(y)} L{f(x + w - r)} {f(y)} Q{f(x + w)} {f(y)} {f(x + w)} {f(y + r)} L{f(x + w)} {f(y + h - r)} "
            f"Q{f(x + w)} {f(y + h)} {f(x + w - r)} {f(y + h)} L{f(x + r)} {f(y + h)} Q{f(x)} {f(y + h)} {f(x)} {f(y + h - r)} "
            f"L{f(x)} {f(y + r)} Q{f(x)} {f(y)} {f(x + r)} {f(y)} Z"), (x, y, x + w, y + h)


def eyes(s, x1, x2, y, rx=8.5, ry=11.5, iris="#2b1a10", closed=False, angry=False, sleepy=False):
    for i, x in enumerate((x1, x2)):
        k = 0.86 if i == 1 else 1.0  # olho de longe um pouco mais estreito (vista 3/4)
        if closed:
            s.path(f"M{f(x - rx * k)} {f(y)} Q{f(x)} {f(y + 5)} {f(x + rx * k)} {f(y)}", "none", stroke=INK, sw=3)
            continue
        s.ellipse(x, y, rx * k, ry, "#ffffff", stroke=INK, sw=2.5)
        s.ellipse(x + 1.8, y + 1, rx * k * 0.72, ry * 0.76, iris, stroke=None)
        if iris != "#2b1a10":
            s.ellipse(x + 2.2, y + 1.5, rx * k * 0.4, ry * 0.45, "#1a0f08", stroke=None)
        s.circle(x + 3.8, y - 3.5, 2.8, "#ffffff", stroke=None)
        s.circle(x - 1.5, y + 4.5, 1.4, "#ffffff", stroke=None)
        if sleepy:
            s.path(f"M{f(x - rx * k - 1)} {f(y - 2)} L{f(x + rx * k + 1)} {f(y - 2)} L{f(x + rx * k + 1)} {f(y - ry - 2)} L{f(x - rx * k - 1)} {f(y - ry - 2)} Z", "#e9b98f", stroke=None)
            s.path(f"M{f(x - rx * k - 1)} {f(y - 2)} L{f(x + rx * k + 1)} {f(y - 2)}", "none", stroke=INK, sw=2.5)
    if angry:
        s.path(f"M{f(x1 - 9)} {f(y - 17)} L{f(x1 + 8)} {f(y - 12)}", "none", stroke=INK, sw=3.5)
        s.path(f"M{f(x2 - 7)} {f(y - 12)} L{f(x2 + 8)} {f(y - 17)}", "none", stroke=INK, sw=3.5)


def hero(name, spec):
    rng = random.Random(name)
    s = Svg(name, W, H, pivot=(110, 18), kind="char")
    ol = s.outline_filter(2.2)
    skin = spec.get("skin", "#f2c9a0")
    main = spec["main"]
    second = spec.get("second", shade(main, -0.2))
    trim = spec.get("trim", "#e0c068")
    pants = spec.get("pants", "#4a3b30")
    boots = spec.get("boots", "#5a3a22")
    hair = spec.get("hair", "#6b3e1f")
    extras = spec.get("extras", [])
    robe = "robe" in extras or "dress" in extras
    hx, hy = 106, 102  # centro da cabeça
    s.begin(f'filter="url(#{ol})" transform="translate(0,{PAD})"')

    # ---------------------------------------------------------------- atrás do corpo
    if "cape" in extras:
        c = spec.get("cape", "#7a2d2d")
        d = f"M78 150 Q60 200 58 250 Q100 262 150 250 Q146 200 134 150 Z"
        cel(s, d, c, (58, 150, 150, 262))
    if "quiver" in extras:
        s.path("M62 118 L84 112 L100 190 L78 196 Z", s.lin("#9a6a3c", "#6b4226"), stroke=INK, sw=3)
        for k, col in enumerate(("#e8453c", "#f2f2f2", "#e8453c")):
            x = 66 + k * 7
            s.limb([(x + 4, 118), (x - 2, 96)], 2.5, "#c8a060", ow=1.5, highlight=False)
            s.path(f"M{x - 2} 96 l-5 -8 l8 2 Z", col, stroke=INK, sw=1.5)
    if "backpack" in extras:
        d, bb = rrect(50, 128, 50, 66, 14)
        cel(s, d, "#9a6a3c", bb)
        s.path("M56 150 L96 150", "none", stroke=INK, sw=2.5)
        s.ellipse(66, 118, 16, 10, "#cfa46a", stroke=INK, sw=2.5)
    hair_back = spec.get("hair_style")
    if hair_back in ("long", "ponytail", "bun_long"):
        if hair_back == "long":
            d = f"M58 104 Q52 170 70 196 Q108 206 146 192 Q160 160 154 104 Z"
            cel(s, d, hair, (52, 104, 160, 206))
        if hair_back == "ponytail":
            d = "M56 96 Q30 120 40 170 Q50 186 58 170 Q54 140 70 110 Z"
            cel(s, d, hair, (30, 96, 70, 186))

    # ---------------------------------------------------------------- braço de trás
    s.limb([(84, 162), (72, 186), (70, 200)], 17, shade(main, -0.12) if not spec.get("bare_arms") else shade(skin, -0.05))
    held_back = spec.get("back_hand")
    if held_back == "shield":
        d, bb = ell(64, 190, 26, 30)
        cel(s, d, spec.get("shield", "#b8413e"), bb)
        d2, bb2 = ell(64, 190, 17, 20)
        cel(s, d2, trim, bb2, shadow=0.1)
        s.path("M64 174 L64 206 M50 190 L78 190", "none", stroke=shade(trim, -0.4), sw=3)
    if held_back == "bow":
        s.path("M52 118 Q12 196 58 262", "none", stroke=INK, sw=11)
        s.path("M52 118 Q12 196 58 262", "none", stroke="#8b5a2b", sw=6)
        s.path("M52 118 L58 262", "none", stroke="#f0e6d0", sw=1.6)
    s.circle(70, 202, 10, skin if not spec.get("gloves") else spec["gloves"], stroke=INK, sw=3)

    # ---------------------------------------------------------------- pernas
    if not robe:
        for lx, fy in ((96, FEET - 2), (122, FEET)):
            s.limb([(lx, 206), (lx, fy - 12)], 17, pants)
            d, bb = ell(lx + 4, fy - 6, 14, 9)
            cel(s, d, boots, bb, shadow=0.12)

    # ---------------------------------------------------------------- tronco
    if robe:
        d = "M76 150 Q72 170 64 244 Q66 256 108 258 Q150 256 152 244 Q144 170 138 150 Q108 142 76 150 Z"
        cel(s, d, main, (64, 142, 152, 258))
        s.path("M108 160 Q106 210 108 256", "none", stroke=shade(main, -0.3), sw=2.5)
        if "robe" in extras and spec.get("robe_trim", True):
            s.path("M66 240 Q108 262 150 240", "none", stroke=trim, sw=5)
        for lx in (94, 124):
            d, bb = ell(lx, FEET - 4, 14, 8)
            cel(s, d, boots, bb, shadow=0.1)
    else:
        d = "M78 150 Q72 180 74 214 Q108 222 144 214 Q146 180 138 150 Q108 142 78 150 Z"
        cel(s, d, main, (72, 142, 146, 222))
    if "tabard" in extras:
        d = "M96 154 L122 154 L126 222 L108 232 L92 222 Z"
        cel(s, d, second, (92, 154, 126, 232), shadow=0.1)
        s.path("M108 172 l-8 10 l8 10 l8 -10 Z", trim, stroke=INK, sw=2)
    if "vest" in extras:
        s.path("M80 152 Q76 185 80 212 L100 214 L100 156 Z", second, stroke=INK, sw=2.5)
        s.path("M136 152 Q140 185 138 212 L118 214 L118 156 Z", second, stroke=INK, sw=2.5)
    if "apron" in extras:
        d = "M88 176 L130 176 L134 236 Q110 244 86 236 Z"
        cel(s, d, spec.get("apron", "#f2ead8"), (86, 176, 134, 244), shadow=0.1)
        s.ellipse(110, 214, 9, 7, shade(spec.get("apron", "#f2ead8"), -0.15), stroke=INK, sw=2)
    belt_y = 208 if not robe else 196
    if spec.get("belt", True):
        s.path(f"M{76 if not robe else 72} {belt_y} Q108 {belt_y + 8} {142 if not robe else 146} {belt_y}", "none", stroke=INK, sw=10)
        s.path(f"M{76 if not robe else 72} {belt_y} Q108 {belt_y + 8} {142 if not robe else 146} {belt_y}", "none", stroke=spec.get("belt_color", "#6b4226"), sw=6)
        s.path(f"M102 {belt_y - 2} L114 {belt_y - 2} L114 {belt_y + 9} L102 {belt_y + 9} Z", trim, stroke=INK, sw=2)
    if "pauldrons" in extras:
        for x in (84, 134):
            d, bb = ell(x, 158, 16, 12)
            cel(s, d, spec.get("armor", "#b8c2cc"), bb)
    if "scarf" in extras:
        sc = spec.get("scarf", "#c0392b")
        s.path("M80 146 Q108 162 138 146 Q140 158 134 164 Q108 172 82 164 Q76 156 80 146 Z", sc, stroke=INK, sw=2.5)
        s.path("M84 160 Q70 180 54 186 Q62 170 66 162 Z", shade(sc, -0.08), stroke=INK, sw=2.5)
    if "beads" in extras:
        for k in range(9):
            a = math.pi * (0.15 + 0.7 * k / 8)
            s.circle(108 + math.cos(a) * 28, 150 + math.sin(a) * 18, 4.2, "#8a4a2a" if k != 4 else "#e8c14a", stroke=INK, sw=1.5)

    # ---------------------------------------------------------------- cabeça
    ear, ebb = ell(hx - 50, hy + 10, 9, 12)
    cel(s, ear, skin, ebb, shadow=0.1, highlight=0)
    head, hbb = ell(hx, hy, 56, 52)
    cel(s, head, skin, hbb, shadow=0.12, highlight=0.08)
    s.ellipse(hx + 30, hy + 20, 7, 4.5, "#ff8a7a", stroke=None, extra='opacity="0.45"')
    s.ellipse(hx - 6, hy + 22, 8, 5, "#ff8a7a", stroke=None, extra='opacity="0.45"')

    style = spec.get("hair_style", "short")
    headwear = spec.get("headwear")
    ey = hy + 6
    if "mask" in extras:
        mk = spec.get("scarf", "#2c2a4a")
        s.path(f"M{hx - 50} {hy + 14} Q{hx + 10} {hy + 6} {hx + 56} {hy + 12} Q{hx + 50} {hy + 48} {hx} {hy + 52} Q{hx - 44} {hy + 44} {hx - 50} {hy + 14} Z", mk, stroke=INK, sw=2.5)
    eyes(s, hx + 2, hx + 30, ey, iris=spec.get("iris", "#2b1a10"), sleepy=spec.get("sleepy", False), angry=spec.get("angry", False))
    if spec.get("brows", True) and not spec.get("angry") and "mask" not in extras:
        bc = shade(hair, -0.1) if style != "bald" else "#c9a27a"
        s.path(f"M{hx - 6} {ey - 17} Q{hx + 2} {ey - 21} {hx + 10} {ey - 17}", "none", stroke=bc, sw=3.5)
        s.path(f"M{hx + 23} {ey - 17} Q{hx + 30} {ey - 21} {hx + 37} {ey - 16}", "none", stroke=bc, sw=3.5)
    if "mask" not in extras and "beard" not in extras:
        mouth = spec.get("mouth", "smile")
        if mouth == "smile":
            s.path(f"M{hx + 12} {hy + 30} Q{hx + 18} {hy + 36} {hx + 25} {hy + 30}", "none", stroke=INK, sw=2.5)
        elif mouth == "open":
            s.path(f"M{hx + 12} {hy + 29} Q{hx + 18} {hy + 42} {hx + 26} {hy + 29} Z", "#a8323a", stroke=INK, sw=2.2)
        else:
            s.path(f"M{hx + 13} {hy + 32} L{hx + 24} {hy + 31}", "none", stroke=INK, sw=2.5)
    if "beard" in extras:
        bc = spec.get("beard_color", "#e8e4dc")
        d = f"M{hx - 34} {hy + 18} Q{hx - 30} {hy + 70} {hx + 14} {hy + 92} Q{hx + 50} {hy + 64} {hx + 50} {hy + 18} Q{hx + 34} {hy + 36} {hx + 10} {hy + 34} Q{hx - 14} {hy + 34} {hx - 34} {hy + 18} Z"
        cel(s, d, bc, (hx - 34, hy + 18, hx + 50, hy + 92), shadow=0.1)
        s.path(f"M{hx - 2} {hy + 30} Q{hx + 16} {hy + 20} {hx + 34} {hy + 30}", "none", stroke=bc, sw=7)
        s.path(f"M{hx - 2} {hy + 30} Q{hx + 16} {hy + 20} {hx + 34} {hy + 30}", "none", stroke=INK, sw=1.5, extra='opacity="0.5"')

    # cabelo / chapéu
    if style == "short" or style == "ponytail":
        d = (f"M{hx - 56} {hy + 6} Q{hx - 60} {hy - 52} {hx} {hy - 56} Q{hx + 58} {hy - 56} {hx + 56} {hy - 4} "
             f"Q{hx + 44} {hy - 26} {hx + 24} {hy - 28} Q{hx + 20} {hy - 14} {hx + 4} {hy - 12} Q{hx + 2} {hy - 26} {hx - 16} {hy - 26} "
             f"Q{hx - 30} {hy - 12} {hx - 44} {hy - 6} Q{hx - 46} {hy + 6} {hx - 56} {hy + 6} Z")
        cel(s, d, hair, (hx - 60, hy - 58, hx + 58, hy + 8))
    elif style == "long":
        d = (f"M{hx - 58} {hy + 30} Q{hx - 64} {hy - 56} {hx} {hy - 57} Q{hx + 60} {hy - 56} {hx + 57} {hy + 4} "
             f"Q{hx + 40} {hy - 30} {hx + 10} {hy - 24} Q{hx - 20} {hy - 22} {hx - 40} {hy} Q{hx - 46} {hy + 20} {hx - 58} {hy + 30} Z")
        cel(s, d, hair, (hx - 64, hy - 58, hx + 60, hy + 30))
    elif style == "bun":
        d = (f"M{hx - 56} {hy + 4} Q{hx - 58} {hy - 54} {hx} {hy - 56} Q{hx + 58} {hy - 54} {hx + 56} {hy - 4} "
             f"Q{hx + 30} {hy - 30} {hx - 4} {hy - 26} Q{hx - 34} {hy - 20} {hx - 56} {hy + 4} Z")
        cel(s, d, hair, (hx - 58, hy - 56, hx + 58, hy + 4))
        d2, bb2 = ell(hx - 18, hy - 60, 22, 18)
        cel(s, d2, hair, bb2)
    elif style == "bald":
        s.ellipse(hx - 16, hy - 30, 16, 9, "#ffffff", stroke=None, extra='opacity="0.35"')
        if spec.get("topknot"):
            d2, bb2 = ell(hx - 6, hy - 58, 11, 10)
            cel(s, d2, "#2b1a10", bb2)

    hw = headwear
    if hw == "helmet":
        steel = spec.get("armor", "#b8c2cc")
        d = f"M{hx - 60} {hy + 2} Q{hx - 62} {hy - 62} {hx} {hy - 62} Q{hx + 62} {hy - 62} {hx + 60} {hy + 2} L{hx + 46} {hy + 4} Q{hx + 40} {hy - 16} {hx + 16} {hy - 16} L{hx - 10} {hy - 16} Q{hx - 36} {hy - 14} {hx - 44} {hy + 6} Z"
        cel(s, d, steel, (hx - 62, hy - 62, hx + 62, hy + 6))
        s.path(f"M{hx + 4} {hy - 60} L{hx + 4} {hy - 16}", "none", stroke=shade(steel, -0.3), sw=4)
        for k in range(4):
            s.circle(hx - 40 + k * 8, hy - 8 - k * 2, 2.5, shade(steel, 0.25), stroke=INK, sw=1)
        pl = spec.get("plume", "#d43c35")
        d = f"M{hx - 4} {hy - 60} Q{hx - 30} {hy - 96} {hx - 70} {hy - 84} Q{hx - 46} {hy - 74} {hx - 50} {hy - 54} Q{hx - 30} {hy - 70} {hx - 4} {hy - 60} Z"
        cel(s, d, pl, (hx - 70, hy - 96, hx, hy - 54))
    elif hw == "hood":
        hc = spec.get("hood", shade(main, -0.1))
        d = (f"M{hx - 62} {hy + 26} Q{hx - 70} {hy - 50} {hx - 10} {hy - 64} Q{hx - 44} {hy - 80} {hx - 78} {hy - 70} "
             f"Q{hx - 50} {hy - 94} {hx + 10} {hy - 66} Q{hx + 64} {hy - 56} {hx + 62} {hy + 20} "
             f"Q{hx + 50} {hy - 22} {hx + 10} {hy - 26} Q{hx - 36} {hy - 24} {hx - 46} {hy + 26} Z")
        cel(s, d, hc, (hx - 78, hy - 94, hx + 64, hy + 26))
        if spec.get("hood_trim"):
            s.path(f"M{hx - 46} {hy + 24} Q{hx - 36} {hy - 24} {hx + 10} {hy - 26} Q{hx + 50} {hy - 22} {hx + 60} {hy + 16}", "none", stroke=spec["hood_trim"], sw=4)
    elif hw == "witch":
        hc = spec.get("hat", "#6b4a2a")
        d, bb = ell(hx + 2, hy - 38, 78, 18)
        cel(s, d, hc, bb)
        d = f"M{hx - 38} {hy - 42} Q{hx - 30} {hy - 110} {hx + 30} {hy - 130} Q{hx + 16} {hy - 100} {hx + 40} {hy - 44} Q{hx} {hy - 30} {hx - 38} {hy - 42} Z"
        cel(s, d, hc, (hx - 38, hy - 130, hx + 40, hy - 30))
        s.path(f"M{hx - 36} {hy - 50} Q{hx} {hy - 38} {hx + 38} {hy - 52}", "none", stroke=spec.get("band", "#6fae3f"), sw=8)
        for k, (lx, ly) in enumerate(((hx + 26, hy - 60), (hx + 38, hy - 66), (hx + 32, hy - 48))):
            s.ellipse(lx, ly, 12, 6, "#74b83f" if k != 1 else "#9ad35a", stroke=INK, sw=2, extra=f'transform="rotate({-30 + k * 25} {lx} {ly})"')
        s.circle(hx + 20, hy - 58, 6, "#ff8fb4", stroke=INK, sw=1.8)
        s.circle(hx + 20, hy - 58, 2.2, "#ffe066", stroke=None)
    elif hw == "wizard":
        hc = spec.get("hat", "#4a3a8c")
        d, bb = ell(hx + 2, hy - 36, 72, 16)
        cel(s, d, hc, bb)
        d = f"M{hx - 42} {hy - 40} Q{hx - 20} {hy - 120} {hx + 44} {hy - 150} Q{hx + 20} {hy - 104} {hx + 44} {hy - 42} Q{hx} {hy - 28} {hx - 42} {hy - 40} Z"
        cel(s, d, hc, (hx - 42, hy - 150, hx + 44, hy - 28))
        s.path(f"M{hx - 40} {hy - 48} Q{hx} {hy - 36} {hx + 42} {hy - 50}", "none", stroke="#e8c14a", sw=7)
        for sx, sy, r in ((hx - 4, hy - 80, 6), (hx + 18, hy - 110, 4.5), (hx - 18, hy - 60, 3.5)):
            pts = " ".join(f"{f(sx + math.cos(a) * (r if k % 2 == 0 else r * 0.45))},{f(sy + math.sin(a) * (r if k % 2 == 0 else r * 0.45))}" for k, a in enumerate([-math.pi / 2 + k * math.pi / 5 for k in range(10)]))
            s.add(f'<polygon points="{pts}" fill="#ffe066" stroke="{INK}" stroke-width="1.2"/>')
    elif hw == "headband":
        s.path(f"M{hx - 56} {hy - 18} Q{hx} {hy - 36} {hx + 56} {hy - 20}", "none", stroke=INK, sw=13)
        s.path(f"M{hx - 56} {hy - 18} Q{hx} {hy - 36} {hx + 56} {hy - 20}", "none", stroke=spec.get("band", "#d43c35"), sw=8)
        s.path(f"M{hx - 54} {hy - 16} Q{hx - 80} {hy - 4} {hx - 88} {hy + 14} Q{hx - 72} {hy} {hx - 56} {hy - 6}", spec.get("band", "#d43c35"), stroke=INK, sw=2.5)
    elif hw == "cap":
        hc = spec.get("hat", "#c0392b")
        d = f"M{hx - 58} {hy - 10} Q{hx - 58} {hy - 72} {hx + 4} {hy - 72} Q{hx + 60} {hy - 70} {hx + 60} {hy - 12} Q{hx} {hy - 26} {hx - 58} {hy - 10} Z"
        cel(s, d, hc, (hx - 58, hy - 72, hx + 60, hy - 10))
        s.path(f"M{hx + 20} {hy - 18} Q{hx + 60} {hy - 30} {hx + 84} {hy - 12} Q{hx + 60} {hy - 6} {hx + 30} {hy - 8} Z", shade(hc, -0.15), stroke=INK, sw=2.5)
        s.path(f"M{hx - 20} {hy - 70} Q{hx - 50} {hy - 110} {hx - 20} {hy - 120}", "none", stroke=INK, sw=6)
        s.path(f"M{hx - 20} {hy - 70} Q{hx - 50} {hy - 110} {hx - 20} {hy - 120}", "none", stroke="#f5d547", sw=3)
    elif hw == "flowers":
        for k in range(6):
            a = math.pi * (1.1 + 0.8 * k / 5)
            x, y = hx + math.cos(a) * 52, hy - 18 + math.sin(a) * 42
            col = ("#ff8fb4", "#ffe066", "#ffffff")[k % 3]
            for j in range(5):
                b = j / 5 * math.tau
                s.ellipse(x + math.cos(b) * 5, y + math.sin(b) * 5, 4.5, 4.5, col, stroke=INK, sw=1.2)
            s.circle(x, y, 3, "#f5a623", stroke=None)

    if "glasses" in extras:
        for x in (hx + 2, hx + 30):
            s.circle(x, ey, 12, "none", stroke=INK, sw=2.5)
        s.path(f"M{hx + 14} {ey} L{hx + 18} {ey}", "none", stroke=INK, sw=2.5)

    # ---------------------------------------------------------------- braço da frente + arma
    weapon = spec.get("weapon")
    if weapon == "sword":
        s.path("M150 196 L180 64 L190 58 L192 70 L160 200 Z", s.lin("#f4f8fb", "#9aa6b2", 0, 0, 1, 0), stroke=INK, sw=3)
        s.path("M166 140 L184 66", "none", stroke="#ffffff", sw=2, extra='opacity="0.7"')
        s.limb([(138, 196), (172, 204)], 7, trim, ow=2.5, highlight=False)
    elif weapon in ("staff", "cane", "spear", "rootstaff"):
        top = 58 if weapon != "cane" else 150
        s.limb([(156, 262), (160, top)], 8, spec.get("wood", "#7a4e2a"), ow=2.5)
        if weapon == "staff":
            s.path(f"M160 {top + 10} Q140 {top - 10} 152 {top - 26} Q168 {top - 8} 160 {top + 10}", "#6fae3f", stroke=INK, sw=2)
            d, bb = ell(162, top - 8, 13, 13)
            cel(s, d, spec.get("orb", "#8ef06a"), bb, shadow=0.08)
            s.circle(158, top - 12, 4, "#ffffff", stroke=None, extra='opacity="0.8"')
        if weapon == "spear":
            s.path(f"M160 {top - 34} L170 {top + 4} L160 {top + 10} L150 {top + 4} Z", s.lin("#f0f4f8", "#9aa6b2"), stroke=INK, sw=2.5)
            s.path(f"M152 {top + 12} L168 {top + 12}", "none", stroke=spec.get("scarf", "#3b6fb0"), sw=6)
        if weapon == "cane":
            s.path(f"M160 {top} Q162 {top - 22} 146 {top - 20}", "none", stroke=INK, sw=13)
            s.path(f"M160 {top} Q162 {top - 22} 146 {top - 20}", "none", stroke=spec.get("wood", "#7a4e2a"), sw=8)
    elif weapon == "wand":
        s.limb([(150, 200), (178, 142)], 6, "#5a3a22", ow=2.5, highlight=False)
        pts = "178,120 188,138 180,150 170,138"
        s.add(f'<polygon points="{pts}" fill="{spec.get("orb", "#7fd3ff")}" stroke="{INK}" stroke-width="2.5"/>')
        s.circle(178, 134, 18, spec.get("orb", "#7fd3ff"), stroke=None, extra='opacity="0.25"')
    elif weapon == "daggers":
        for x0, y0, x1, y1 in ((146, 196, 180, 164), (70, 204, 38, 176)):
            s.path(f"M{x0} {y0} L{x1} {y1} L{x1 + 6} {y1 + 6} Z", s.lin("#e8eef4", "#8a96a2"), stroke=INK, sw=2.5)
            s.limb([(x0 - 6, y0 + 2), (x0 + 6, y0 - 6)], 5, "#3a2a4a", ow=2, highlight=False)
    elif weapon == "lantern":
        s.limb([(150, 198), (150, 214)], 2.5, INK, ow=0, highlight=False)
        d, bb = rrect(138, 214, 24, 30, 6)
        cel(s, d, "#ffcf5a", bb, shadow=0.1)
        s.circle(150, 229, 22, "#ffd76a", stroke=None, extra='opacity="0.25"')
        s.path("M136 214 L164 214 L158 206 L142 206 Z", "#5a4a3a", stroke=INK, sw=2)
    elif weapon == "basket":
        d = "M134 196 L170 196 L166 226 L138 226 Z"
        cel(s, d, "#c89456", (134, 196, 170, 226))
        s.path("M136 196 Q152 170 168 196", "none", stroke=INK, sw=5)
        s.path("M136 196 Q152 170 168 196", "none", stroke="#a87438", sw=2.5)
        for x in (144, 152, 160):
            s.circle(x, 194, 5, rng.choice(["#e8453c", "#74b83f", "#ffe066"]), stroke=INK, sw=1.5)
    elif weapon == "tape":
        s.path("M146 198 Q160 230 150 250 Q140 236 146 198", "none", stroke="#f5d547", sw=5)
    elif weapon == "scroll":
        d, bb = rrect(136, 188, 34, 22, 8)
        cel(s, d, "#f2e6c8", bb, shadow=0.08)
        s.path("M140 194 L166 194 M140 200 L160 200", "none", stroke="#8a6a45", sw=1.5)
    s.limb([(132, 162), (146, 184), (150, 196)], 17, main if not spec.get("bare_arms") else skin)
    if spec.get("bare_arms") and "wraps" in extras:
        s.path("M144 184 L152 186 M146 190 L154 192", "none", stroke="#f2ead8", sw=4)
    s.circle(150, 198, 10.5, skin if not spec.get("gloves") else spec["gloves"], stroke=INK, sw=3)
    if weapon == "wraps":
        s.circle(150, 198, 10.5, "#f2ead8", stroke=INK, sw=3)
        s.path("M142 194 L158 198 M142 201 L157 204", "none", stroke="#c9bfa8", sw=2)
    s.end()
    return s


CLASSES = {
    "guerreiro": dict(main="#8a929c", second="#b8413e", trim="#e0c068", skin="#f2c9a0", hair="#7a4a28", headwear="helmet", armor="#c3ccd6",
                      weapon="sword", back_hand="shield", shield="#b8413e", extras=["tabard", "pauldrons", "cape"], cape="#8e2f2b",
                      pants="#5a4a40", boots="#4a3a30", mouth="smile"),
    "arqueiro": dict(main="#3f8f4e", second="#2f6e3b", trim="#c89b5a", skin="#e8b98f", hair="#a0522d", headwear="hood", hood="#2f7040",
                     hood_trim="#c89b5a", back_hand="bow", extras=["quiver"], pants="#5a4a3a", boots="#6b4a2a", gloves="#8a5a30", iris="#3f7a3a"),
    "mago": dict(main="#2e2640", second="#1d1828", trim="#b58cff", skin="#e6c8b4", hair="#dcd6ea", hair_style="long", headwear="wizard", hat="#231c33",
                 weapon="wand", orb="#c08cff", extras=["robe"], boots="#1a1524", iris="#9a5cff", angry=True),
    "assassino": dict(main="#2f2d4a", second="#1f1e33", trim="#9a8ad0", skin="#e0b090", hair="#1f1e33", headwear="hood", hood="#26243c",
                      extras=["mask", "scarf"], scarf="#b8323a", weapon="daggers", pants="#26243c", boots="#1f1e2e", gloves="#26243c", iris="#b8323a", angry=True),
}

NPCS = {
    "anciao": dict(main="#7a5a3a", trim="#c8a060", skin="#e8c0a0", hair="#e8e4dc", hair_style="bald", extras=["robe", "beard"], weapon="cane",
                   wood="#6b4226", boots="#4a3a2a", sleepy=True, robe_trim=False, belt_color="#c8a060"),
    "mercador": dict(main="#3f7a4a", second="#c9a24a", trim="#e8c14a", skin="#f2c9a0", hair="#8a4a2a", headwear="cap", hat="#c0392b",
                     extras=["vest", "backpack"], weapon="scroll", pants="#6b4a3a", boots="#4a3020", mouth="open"),
    "costureira": dict(main="#d9728f", second="#b8506f", trim="#fff0f5", skin="#f5d0b5", hair="#8a3a1f", hair_style="bun", extras=["dress", "apron"],
                       weapon="tape", boots="#8a3a4a", robe_trim=False, iris="#4a7ab0"),
    "herbalista": dict(main="#5f8f5a", second="#3f6e3a", trim="#e0c068", skin="#c98a60", hair="#3a2a1f", hair_style="long", headwear="flowers",
                       extras=["dress", "apron"], apron="#e8dcc0", weapon="basket", boots="#5a3a22", robe_trim=False, iris="#3a6a2a"),
    "capita": dict(main="#3b5f9a", second="#e8c14a", trim="#e8c14a", skin="#e8b98f", hair="#c0582a", hair_style="ponytail", extras=["pauldrons", "tabard", "cape"],
                   armor="#aab4c0", cape="#2a4a7a", weapon="spear", scarf="#3b6fb0", pants="#4a4a5a", boots="#3a3a4a", mouth="flat", gloves="#6b4a2a"),
    "eremita": dict(main="#6b6a70", second="#4a4a50", trim="#9a9a8a", skin="#e0b89a", hair="#dcd8d0", headwear="hood", hood="#5a5960", extras=["robe", "beard"],
                    beard_color="#dcd8d0", weapon="lantern", boots="#3a3530", robe_trim=False),
}


def build():
    # As 4 classes jogáveis agora vêm do PixelLab (art/pixellab). CLASSES fica como referência/plano B.
    return [hero(f"char_npc_{k}", v) for k, v in NPCS.items()]

