"""Heróis jogáveis com proporções no estilo Dofus: ~4 cabeças de altura, esguios, rosto pequeno e expressivo."""
from .lib import Svg, INK, shade, f
from .characters import cel, ell

W, H = 200, 320
FEET = 300
LINE = "#241712"


def _leg(s, x, top, bottom, width, pants, boot, boot_h=26):
    s.limb([(x, top), (x + 1, bottom - boot_h)], width, pants, outline=LINE, ow=2.2)
    d = (f"M{f(x - width / 2 - 1)} {f(bottom - boot_h - 2)} L{f(x + width / 2 + 1)} {f(bottom - boot_h - 2)} "
         f"L{f(x + width / 2 + 1)} {f(bottom - 6)} Q{f(x + width / 2 + 12)} {f(bottom - 5)} {f(x + width / 2 + 12)} {f(bottom)} "
         f"L{f(x - width / 2 - 1)} {f(bottom)} Z")
    cel(s, d, boot, (x - width, bottom - boot_h, x + width + 12, bottom), sw=2.2, stroke=LINE)


def _arm(s, pts, width, sleeve, skin, glove=None):
    s.limb(pts, width, sleeve, outline=LINE, ow=2.2)
    hx, hy = pts[-1]
    s.circle(hx, hy + 2, width * 0.52, glove or skin, stroke=LINE, sw=2.2)


def _face(s, hx, hy, skin, hair, eyes="#3a2a1f", mask=None, beard=None, fierce=False):
    head, hb = ell(hx, hy, 27, 31)
    cel(s, head, skin, hb, stroke=LINE, sw=2.4, shadow=0.1, highlight=0.06)
    s.ellipse(hx - 24, hy + 4, 5, 8, shade(skin, -0.05), stroke=LINE, sw=2)  # orelha
    ey = hy + 3
    for x, w in ((hx + 3, 1.0), (hx + 17, 0.85)):
        s.ellipse(x, ey, 4.2 * w, 5.2, "#ffffff", stroke=LINE, sw=1.6)
        s.ellipse(x + 1, ey + 0.8, 2.8 * w, 3.8, eyes, stroke=None)
        s.circle(x + 1.8, ey - 1.2, 1.1, "#ffffff", stroke=None)
    brow = shade(hair, -0.15)
    tilt = 3 if fierce else -1
    s.path(f"M{hx - 3} {ey - 9 + tilt} L{hx + 9} {ey - 11}", "none", stroke=brow, sw=2.6)
    s.path(f"M{hx + 13} {ey - 11} L{hx + 23} {ey - 9 + tilt}", "none", stroke=brow, sw=2.6)
    if mask:
        d = f"M{hx - 22} {hy + 9} Q{hx + 4} {hy + 5} {hx + 27} {hy + 8} Q{hx + 25} {hy + 30} {hx + 2} {hy + 33} Q{hx - 18} {hy + 28} {hx - 22} {hy + 9} Z"
        cel(s, d, mask, (hx - 22, hy + 5, hx + 27, hy + 33), stroke=LINE, sw=2)
        return
    s.path(f"M{hx + 11} {ey + 5} L{hx + 13} {ey + 11} L{hx + 10} {ey + 12}", "none", stroke=shade(skin, -0.3), sw=1.6)
    s.path(f"M{hx + 6} {hy + 20} Q{hx + 11} {hy + 22} {hx + 16} {hy + 19}", "none", stroke="#6a3a2a", sw=2)
    if beard:
        d = f"M{hx - 18} {hy + 10} Q{hx - 12} {hy + 38} {hx + 6} {hy + 40} Q{hx + 24} {hy + 36} {hx + 26} {hy + 10} Q{hx + 16} {hy + 24} {hx + 4} {hy + 24} Q{hx - 8} {hy + 24} {hx - 18} {hy + 10} Z"
        cel(s, d, beard, (hx - 18, hy + 10, hx + 26, hy + 40), stroke=LINE, sw=2)


def guerreiro():
    s = Svg("char_guerreiro", W, H, pivot=(100, H - FEET), kind="char")
    ol = s.outline_filter(1.6, LINE)
    s.begin(f'filter="url(#{ol})"')
    steel, red, gold, skin, hair = "#aeb7c2", "#9c2f2b", "#d9b14a", "#e9c19c", "#b8612a"
    # capa
    d = "M78 132 Q56 200 52 286 Q100 300 142 284 Q132 200 122 132 Z"
    cel(s, d, "#7a2422", (52, 132, 142, 300), stroke=LINE, sw=2.2)
    # escudo nas costas / braço de trás
    _arm(s, [(80, 142), (70, 176), (68, 204)], 13, shade(steel, -0.12), skin, glove="#6b4a33")
    d, bb = ell(60, 188, 24, 32)
    cel(s, d, red, bb, stroke=LINE, sw=2.4)
    d, bb = ell(60, 188, 15, 21)
    cel(s, d, steel, bb, stroke=LINE, sw=1.8, shadow=0.1)
    s.path("M60 170 L60 206 M47 188 L73 188", "none", stroke=gold, sw=3)
    # pernas
    _leg(s, 90, 206, FEET - 2, 14, "#5a4a44", shade(steel, -0.1))
    _leg(s, 110, 206, FEET, 14, "#5a4a44", shade(steel, -0.05))
    # tronco (couraça) + tabardo
    d = "M76 134 Q72 170 78 210 Q100 218 124 210 Q130 170 124 134 Q100 126 76 134 Z"
    cel(s, d, steel, (72, 126, 130, 218), stroke=LINE, sw=2.4)
    d = "M90 150 L112 150 L114 236 L101 244 L88 236 Z"
    cel(s, d, red, (88, 150, 114, 244), stroke=LINE, sw=2, shadow=0.12)
    s.path("M101 170 l-6 8 l6 8 l6 -8 Z", gold, stroke=LINE, sw=1.4)
    s.path("M78 206 Q100 214 124 206", "none", stroke="#5a3a24", sw=6)
    for x in (80, 120):
        d, bb = ell(x, 140, 15, 11)
        cel(s, d, steel, bb, stroke=LINE, sw=2.2)
    # cabeça: cabelo espetado
    hx, hy = 100, 98
    d = f"M{hx - 30} {hy - 4} L{hx - 40} {hy - 30} L{hx - 20} {hy - 26} L{hx - 22} {hy - 46} L{hx - 4} {hy - 34} L{hx + 4} {hy - 52} L{hx + 14} {hy - 32} L{hx + 32} {hy - 40} L{hx + 26} {hy - 18} L{hx + 36} {hy - 10} L{hx + 24} {hy - 4} Z"
    cel(s, d, hair, (hx - 40, hy - 52, hx + 36, hy - 4), stroke=LINE, sw=2.2)
    _face(s, hx, hy, skin, hair, eyes="#2f5a8a", fierce=True)
    d = f"M{hx - 27} {hy - 6} Q{hx - 20} {hy - 26} {hx + 2} {hy - 26} Q{hx + 20} {hy - 26} {hx + 27} {hy - 8} Q{hx + 14} {hy - 16} {hx} {hy - 14} Q{hx - 14} {hy - 14} {hx - 27} {hy - 6} Z"
    cel(s, d, hair, (hx - 27, hy - 26, hx + 27, hy - 6), stroke=LINE, sw=2)
    # espada + braço da frente
    s.path("M128 206 L176 110 L184 106 L182 116 L136 210 Z", s.lin("#f2f5f8", "#8c97a3", 0, 0, 1, 0), stroke=LINE, sw=2.4)
    s.path("M140 190 L178 114", "none", stroke="#ffffff", sw=1.4, extra='opacity="0.7"')
    s.limb([(118, 204), (146, 214)], 6, gold, outline=LINE, ow=2, highlight=False)
    _arm(s, [(120, 142), (132, 174), (132, 204)], 13, steel, skin, glove="#6b4a33")
    s.end()
    return s


def arqueiro():
    s = Svg("char_arqueiro", W, H, pivot=(100, H - FEET), kind="char")
    ol = s.outline_filter(1.6, LINE)
    s.begin(f'filter="url(#{ol})"')
    green, leather, skin, hair = "#3f6b3a", "#8a5a34", "#e2b48c", "#c07a3a"
    # aljava
    s.path("M70 120 L88 114 L100 196 L82 202 Z", s.lin("#8a5a34", "#5e3b22"), stroke=LINE, sw=2)
    for k, col in enumerate(("#d9483a", "#eeeeee", "#d9483a")):
        x = 73 + k * 6
        s.limb([(x + 4, 120), (x - 2, 96)], 2, "#c8a060", outline=LINE, ow=1.2, highlight=False)
        s.path(f"M{x - 2} 96 l-4 -7 l7 2 Z", col, stroke=LINE, sw=1.2)
    # capa curta
    d = "M76 128 Q62 190 66 238 Q100 250 134 236 Q136 190 124 128 Z"
    cel(s, d, shade(green, -0.1), (62, 128, 136, 250), stroke=LINE, sw=2.2)
    # arco no braço de trás
    s.path("M58 110 Q18 200 60 290", "none", stroke=LINE, sw=9)
    s.path("M58 110 Q18 200 60 290", "none", stroke="#7a4e2a", sw=5)
    s.path("M58 110 L60 290", "none", stroke="#eee6d2", sw=1.2)
    _arm(s, [(82, 142), (66, 176), (52, 198)], 12, green, skin, glove="#6b4226")
    _leg(s, 91, 206, FEET - 2, 13, "#4f4234", "#6b4226", boot_h=40)
    _leg(s, 110, 206, FEET, 13, "#4f4234", "#6b4226", boot_h=40)
    d = "M78 134 Q74 172 80 212 Q100 220 122 212 Q128 172 122 134 Q100 126 78 134 Z"
    cel(s, d, green, (74, 126, 128, 220), stroke=LINE, sw=2.4)
    d = "M86 146 L116 146 L118 206 L84 206 Z"
    cel(s, d, leather, (84, 146, 118, 206), stroke=LINE, sw=2, shadow=0.1)
    s.path("M86 150 L116 196", "none", stroke="#4a2e1a", sw=4)
    s.path("M80 208 Q100 214 122 208", "none", stroke="#4a2e1a", sw=6)
    hx, hy = 100, 98
    _face(s, hx, hy, skin, hair, eyes="#3d6a2a")
    # capuz
    d = (f"M{hx - 34} {hy + 26} Q{hx - 40} {hy - 30} {hx - 2} {hy - 40} Q{hx - 30} {hy - 52} {hx - 52} {hy - 40} "
         f"Q{hx - 30} {hy - 62} {hx + 8} {hy - 44} Q{hx + 38} {hy - 34} {hx + 34} {hy + 18} "
         f"Q{hx + 26} {hy - 16} {hx + 4} {hy - 20} Q{hx - 22} {hy - 18} {hx - 26} {hy + 26} Z")
    cel(s, d, green, (hx - 52, hy - 62, hx + 38, hy + 26), stroke=LINE, sw=2.4)
    s.path(f"M{hx - 20} {hy - 16} Q{hx - 4} {hy - 24} {hx + 22} {hy - 12} L{hx + 22} {hy - 6} Q{hx + 4} {hy - 16} {hx - 20} {hy - 8} Z", hair, stroke=LINE, sw=1.6)
    _arm(s, [(118, 142), (132, 172), (138, 198)], 12, green, skin, glove="#6b4226")
    s.end()
    return s


def mago():
    s = Svg("char_mago", W, H, pivot=(100, H - FEET), kind="char")
    ol = s.outline_filter(1.6, LINE)
    s.begin(f'filter="url(#{ol})"')
    robe, trim, skin, hair, glow = "#2c2440", "#b58cff", "#e8cfc0", "#d9d4e6", "#c08cff"
    # cabelo longo atrás
    d = "M72 96 Q64 160 78 196 Q100 204 124 194 Q134 150 128 96 Z"
    cel(s, d, hair, (64, 96, 134, 204), stroke=LINE, sw=2.2)
    _arm(s, [(80, 144), (70, 176), (66, 202)], 13, shade(robe, -0.05), skin)
    # túnica longa
    d = "M76 134 Q70 190 62 286 Q100 300 140 286 Q130 190 124 134 Q100 126 76 134 Z"
    cel(s, d, robe, (62, 126, 140, 300), stroke=LINE, sw=2.4)
    s.path("M100 150 L100 292", "none", stroke=trim, sw=2.4)
    s.path("M64 280 Q100 298 138 280", "none", stroke=trim, sw=3)
    s.path("M78 196 Q100 204 124 196", "none", stroke=trim, sw=5)
    for (x, y) in ((88, 240), (112, 262), (84, 270)):
        s.add(f'<polygon points="{x},{y - 5} {x + 2},{y} {x},{y + 5} {x - 2},{y}" fill="{trim}" opacity="0.8"/>')
    hx, hy = 100, 100
    _face(s, hx, hy, skin, "#8a80a0", eyes="#8a4ad0", fierce=True)
    s.path(f"M{hx - 26} {hy - 4} Q{hx - 8} {hy - 22} {hx + 26} {hy - 8} L{hx + 28} {hy + 2} Q{hx + 4} {hy - 12} {hx - 26} {hy + 10} Z", hair, stroke=LINE, sw=1.8)
    # chapéu largo e pontudo
    d, bb = ell(hx + 2, hy - 20, 58, 13)
    cel(s, d, "#231c33", bb, stroke=LINE, sw=2.4)
    d = f"M{hx - 28} {hy - 24} Q{hx - 18} {hy - 80} {hx + 14} {hy - 104} Q{hx + 34} {hy - 112} {hx + 44} {hy - 96} Q{hx + 24} {hy - 96} {hx + 22} {hy - 72} Q{hx + 24} {hy - 44} {hx + 32} {hy - 24} Q{hx} {hy - 16} {hx - 28} {hy - 24} Z"
    cel(s, d, "#2a2140", (hx - 28, hy - 112, hx + 44, hy - 16), stroke=LINE, sw=2.4)
    s.path(f"M{hx - 26} {hy - 30} Q{hx + 2} {hy - 22} {hx + 30} {hy - 30}", "none", stroke=trim, sw=5)
    # cajado com orbe
    s.limb([(140, 296), (146, 96)], 5, "#3a2a22", outline=LINE, ow=2, highlight=False)
    s.path("M146 100 Q134 86 140 72 M146 100 Q158 86 152 72", "none", stroke="#3a2a22", sw=4)
    s.circle(146, 80, 22, glow, stroke=None, extra='opacity="0.22"')
    d, bb = ell(146, 80, 9, 9)
    cel(s, d, glow, bb, stroke=LINE, sw=1.8, shadow=0.05)
    s.circle(143, 77, 3, "#ffffff", stroke=None, extra='opacity="0.8"')
    _arm(s, [(120, 144), (132, 170), (142, 184)], 13, robe, skin)
    s.end()
    return s


def assassino():
    s = Svg("char_assassino", W, H, pivot=(100, H - FEET), kind="char")
    ol = s.outline_filter(1.6, LINE)
    s.begin(f'filter="url(#{ol})"')
    dark, leather, red, skin = "#2a2733", "#3b3441", "#a52a2f", "#dcb094"
    # cachecol esvoaçante
    d = "M84 130 Q56 128 36 150 Q58 146 64 156 Q46 170 40 186 Q66 166 88 150 Z"
    cel(s, d, red, (36, 128, 88, 186), stroke=LINE, sw=2)
    _arm(s, [(80, 144), (66, 172), (54, 188)], 12, dark, skin, glove="#1c1a22")
    s.path("M50 190 L22 166 L26 162 L56 184 Z", s.lin("#e8eef4", "#7a8692"), stroke=LINE, sw=2)
    _leg(s, 91, 206, FEET - 2, 13, leather, "#1f1c24", boot_h=44)
    _leg(s, 110, 206, FEET, 13, leather, "#1f1c24", boot_h=44)
    d = "M78 134 Q74 172 80 212 Q100 220 122 212 Q128 172 122 134 Q100 126 78 134 Z"
    cel(s, d, dark, (74, 126, 128, 220), stroke=LINE, sw=2.4)
    s.path("M84 150 L118 196 M118 150 L84 196", "none", stroke="#5a4e60", sw=3)
    s.path("M80 208 Q100 214 122 208", "none", stroke=red, sw=5)
    # sobrecasaca curta (abas)
    s.path("M80 210 L74 250 L94 240 Z M122 210 L128 250 L108 240 Z", leather, stroke=LINE, sw=2)
    hx, hy = 100, 98
    _face(s, hx, hy, skin, "#1c1a22", eyes="#c0303a", mask=red, fierce=True)
    d = (f"M{hx - 34} {hy + 22} Q{hx - 40} {hy - 34} {hx + 2} {hy - 42} Q{hx + 40} {hy - 36} {hx + 34} {hy + 16} "
         f"Q{hx + 24} {hy - 18} {hx + 2} {hy - 20} Q{hx - 22} {hy - 18} {hx - 26} {hy + 22} Z")
    cel(s, d, dark, (hx - 40, hy - 42, hx + 40, hy + 22), stroke=LINE, sw=2.4)
    _arm(s, [(118, 142), (134, 168), (146, 184)], 12, dark, skin, glove="#1c1a22")
    s.path("M148 186 L182 160 L180 170 L152 192 Z", s.lin("#e8eef4", "#7a8692"), stroke=LINE, sw=2)
    s.end()
    return s


def build():
    return [guerreiro(), arqueiro(), mago(), assassino()]
