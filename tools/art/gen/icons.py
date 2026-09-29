"""Ícones de feitiços (medalhões) e de itens (objetos soltos), 96x96."""
import math
from .lib import Svg, INK, shade, f
from .characters import cel, ell, rrect

S = 96
ELEMENT = {"earth": "#b5773a", "fire": "#e8572e", "water": "#3a8fd9", "air": "#4fb866", "neutral": "#9a8a7a"}
CREAM = "#fff8e8"


def star(cx, cy, r1, r2, n=5, rot=-90):
    pts = []
    for k in range(n * 2):
        a = math.radians(rot + k * 180 / n)
        r = r1 if k % 2 == 0 else r2
        pts.append(f"{f(cx + math.cos(a) * r)},{f(cy + math.sin(a) * r)}")
    return " ".join(pts)


# ---------------------------------------------------------------------- glifos (desenhados em branco-creme)

def g_sword(s):
    s.path("M30 66 L62 26 L70 22 L66 30 L34 70 Z", CREAM, sw=3)
    s.path("M26 58 L38 70 M24 72 L30 66", "none", stroke=INK, sw=8)
    s.path("M26 58 L38 70 M24 72 L30 66", "none", stroke="#e8c14a", sw=4)

def g_push(s):
    s.circle(34, 50, 13, CREAM, sw=3)
    for x in (52, 64):
        s.path(f"M{x} 36 L{x + 12} 50 L{x} 64", "none", stroke=INK, sw=9)
        s.path(f"M{x} 36 L{x + 12} 50 L{x} 64", "none", stroke=CREAM, sw=5)

def g_jump(s):
    s.path("M24 68 Q40 14 70 44", "none", stroke=INK, sw=10)
    s.path("M24 68 Q40 14 70 44", "none", stroke=CREAM, sw=6)
    s.path("M64 32 L76 50 L58 52 Z", CREAM, sw=3)

def g_flame(s):
    s.path("M48 76 Q24 70 30 48 Q34 58 40 56 Q34 36 50 20 Q52 36 62 42 Q66 34 64 28 Q80 48 70 64 Q64 76 48 76 Z", CREAM, sw=3)
    s.path("M48 72 Q38 66 42 56 Q48 60 50 50 Q60 62 56 68 Q54 72 48 72 Z", "#ffd76a", sw=2)

def g_arrow(s, through=False):
    if through:
        s.path("M48 18 L48 78", "none", stroke=INK, sw=6, extra='stroke-dasharray="6 6"')
    s.path("M20 72 L66 30", "none", stroke=INK, sw=9)
    s.path("M20 72 L66 30", "none", stroke=CREAM, sw=5)
    s.path("M58 26 L76 20 L70 38 Z", CREAM, sw=3)
    s.path("M20 72 L16 62 M20 72 L30 76", "none", stroke=INK, sw=6)

def g_burst(s):
    s.add(f'<polygon points="{star(48, 48, 30, 14, 8)}" fill="{CREAM}" stroke="{INK}" stroke-width="3" stroke-linejoin="round"/>')
    s.circle(48, 48, 9, "#ffd76a", sw=2)

def g_chevrons(s):
    for x in (26, 44):
        s.path(f"M{x} 30 L{x + 16} 48 L{x} 66", "none", stroke=INK, sw=11)
        s.path(f"M{x} 30 L{x + 16} 48 L{x} 66", "none", stroke=CREAM, sw=6)

def g_thorns(s):
    for x, h in ((30, 30), (48, 42), (66, 30)):
        s.path(f"M{x - 9} 74 L{x} {74 - h} L{x + 9} 74 Z", CREAM, sw=3)

def g_heal(s):
    s.path("M48 18 Q70 44 70 58 Q70 78 48 78 Q26 78 26 58 Q26 44 48 18 Z", CREAM, sw=3)
    s.path("M48 46 L48 68 M37 57 L59 57", "none", stroke=ELEMENT["air"], sw=6)

def g_roots(s):
    for x0, c in ((30, 1), (48, -1), (66, 1)):
        d = f"M{x0} 78 Q{x0 + c * 14} 60 {x0} 46 Q{x0 - c * 14} 32 {x0 + c * 4} 20"
        s.path(d, "none", stroke=INK, sw=9)
        s.path(d, "none", stroke=CREAM, sw=5)

def g_seed(s):
    d, bb = ell(48, 52, 16, 22)
    s.path(d, CREAM, sw=3)
    for a in range(0, 360, 45):
        r = math.radians(a)
        s.path(f"M{f(48 + math.cos(r) * 26)} {f(52 + math.sin(r) * 26)} L{f(48 + math.cos(r) * 34)} {f(52 + math.sin(r) * 34)}", "none", stroke=CREAM, sw=4)

def g_fireball(s):
    s.path("M70 26 Q60 50 30 64", "none", stroke="#ffd76a", sw=10, extra='opacity="0.9"')
    s.circle(36, 60, 16, CREAM, sw=3)
    s.circle(36, 60, 8, "#ffd76a", stroke=None)

def g_snow(s):
    for a in (0, 60, 120):
        r = math.radians(a)
        dx, dy = math.cos(r) * 30, math.sin(r) * 30
        s.path(f"M{f(48 - dx)} {f(48 - dy)} L{f(48 + dx)} {f(48 + dy)}", "none", stroke=INK, sw=9)
        s.path(f"M{f(48 - dx)} {f(48 - dy)} L{f(48 + dx)} {f(48 + dy)}", "none", stroke=CREAM, sw=5)
    s.circle(48, 48, 7, CREAM, sw=2.5)

def g_bolt(s):
    s.path("M54 16 L30 52 L46 52 L38 80 L66 40 L50 40 Z", CREAM, sw=3)

def g_rune(s):
    s.add(f'<polygon points="{star(48, 48, 30, 12, 4, rot=0)}" fill="{CREAM}" stroke="{INK}" stroke-width="3" stroke-linejoin="round"/>')
    s.circle(48, 48, 8, ELEMENT["water"], sw=2)

def g_dagger_drop(s):
    s.path("M26 70 L58 30 L66 26 L62 34 L30 74 Z", CREAM, sw=3)
    s.path("M68 50 Q76 62 70 70 Q62 70 62 62 Q62 56 68 50 Z", "#9ae04a", sw=2.5)

def g_fangs(s):
    s.path("M22 34 Q48 46 74 34 L74 40 Q48 52 22 40 Z", CREAM, sw=3)
    s.path("M32 42 L38 70 L44 44 Z", CREAM, sw=3)
    s.path("M52 44 L58 70 L64 42 Z", CREAM, sw=3)

def g_shadow_step(s):
    for x, y, o in ((30, 66, 0.5), (46, 52, 0.75), (62, 38, 1)):
        s.ellipse(x, y, 8, 12, CREAM, sw=2.5, extra=f'opacity="{o}" transform="rotate(30 {x} {y})"')

def g_knife(s):
    s.path("M22 48 L30 44 L30 52 Z M34 48 L40 44 L40 52 Z", CREAM, sw=2, extra='opacity="0.6"')
    s.path("M44 48 L72 40 L78 48 L72 56 Z", CREAM, sw=3)
    s.path("M36 42 L44 48 L36 54", "none", stroke="#e8c14a", sw=5)

def g_palm(s):
    d = "M34 76 L32 44 Q30 36 36 36 Q40 36 40 44 L40 30 Q40 22 46 22 Q52 22 52 30 L52 26 Q52 18 58 20 Q63 22 62 30 L62 38 Q64 32 69 34 Q73 36 72 44 L68 64 Q64 76 54 78 Z"
    s.path(d, CREAM, sw=3)

def g_pull(s):
    for x in (54, 70):
        s.path(f"M{x} 30 L{x - 16} 48 L{x} 66", "none", stroke=INK, sw=11)
        s.path(f"M{x} 30 L{x - 16} 48 L{x} 66", "none", stroke=CREAM, sw=6)
    s.circle(26, 48, 8, CREAM, sw=2.5)

def g_quake(s):
    d, bb = rrect(32, 18, 32, 34, 10)
    s.path(d, CREAM, sw=3)
    s.path("M14 70 L30 62 L40 72 L52 60 L62 72 L80 64", "none", stroke=INK, sw=8)
    s.path("M14 70 L30 62 L40 72 L52 60 L62 72 L80 64", "none", stroke=CREAM, sw=4)

def g_lotus(s):
    for rot in (-50, -25, 0, 25, 50):
        s.ellipse(48, 42, 9, 22, CREAM, sw=2.5, extra=f'transform="rotate({rot} 48 64)"')
    s.path("M26 68 Q48 78 70 68", "none", stroke=INK, sw=4)


SPELL_GLYPHS = {
    "golpe": g_sword, "pancada": g_push, "salto": g_jump, "furia": g_flame,
    "flecha": g_arrow, "explosiva": g_burst, "recuo": g_chevrons, "tiro": lambda s: g_arrow(s, True),
    "espinhos": g_thorns, "seiva": g_heal, "raizes": g_roots, "semente": g_seed,
    "bola_fogo": g_fireball, "nova": g_snow, "faisca": g_bolt, "foco": g_rune,
    "adaga": g_dagger_drop, "vampiro": g_fangs, "passo": g_shadow_step, "arremesso": g_knife,
    "palma": g_palm, "atracao": g_pull, "sismico": g_quake, "meditacao": g_lotus,
}
SPELL_ELEMENT = {
    "golpe": "earth", "pancada": "earth", "salto": "air", "furia": "fire", "flecha": "air", "explosiva": "fire", "recuo": "air", "tiro": "water",
    "espinhos": "earth", "seiva": "water", "raizes": "earth", "semente": "water", "bola_fogo": "fire", "nova": "water", "faisca": "air", "foco": "neutral",
    "adaga": "air", "vampiro": "fire", "passo": "air", "arremesso": "earth", "palma": "air", "atracao": "water", "sismico": "earth", "meditacao": "neutral",
}


def spell_icon(spell_id):
    s = Svg(f"icon_spell_{spell_id}", S, S, pivot=(48, 48), ppu=96, kind="icon")
    col = ELEMENT[SPELL_ELEMENT[spell_id]]
    s.circle(48, 48, 44, s.lin("#f2d27a", "#a8762a"), stroke=INK, sw=3)
    s.circle(48, 48, 37, s.rad(shade(col, 0.2), shade(col, -0.25), 0.35, 0.3, 0.9), stroke=INK, sw=2.5)
    s.ellipse(38, 30, 20, 10, "#ffffff", stroke=None, extra='opacity="0.22"')
    SPELL_GLYPHS[spell_id](s)
    return s


# ---------------------------------------------------------------------- itens

def item_base(item_id):
    s = Svg(f"icon_item_{item_id}", S, S, pivot=(48, 48), ppu=96, kind="icon")
    ol = s.outline_filter(1.6)
    s.begin(f'filter="url(#{ol})"')
    return s


def i_sword(s, blade="#e8eef4", hilt="#e8c14a", big=False):
    w = 8 if big else 6
    s.path(f"M24 72 L66 22 L{74} 18 L70 26 L28 76 Z", s.lin(blade, shade(blade, -0.3), 0, 0, 1, 0), sw=3)
    s.path("M30 60 L40 70 M22 74 L28 68", "none", stroke=INK, sw=9)
    s.path("M30 60 L40 70 M22 74 L28 68", "none", stroke=hilt, sw=5)
    if big:
        s.path("M40 50 Q56 36 64 26", "none", stroke="#fffaf0", sw=3)

def i_bow(s):
    s.path("M30 14 Q84 48 30 82", "none", stroke=INK, sw=10)
    s.path("M30 14 Q84 48 30 82", "none", stroke="#9a6a3c", sw=6)
    s.path("M30 14 L30 82", "none", stroke="#f0e6d0", sw=2)

def i_staff(s, top="#8ef06a", wood="#7a4e2a"):
    s.limb([(26, 80), (62, 30)], 7, wood, ow=2.5)
    d, bb = ell(66, 26, 12, 12)
    cel(s, d, top, bb, shadow=0.1)
    s.path("M58 36 Q48 30 52 22", "none", stroke="#6fae3f", sw=4)

def i_wand(s):
    s.limb([(26, 76), (58, 36)], 6, "#5a3a22", ow=2.5)
    s.add(f'<polygon points="62,16 74,34 64,44 54,32" fill="#7fd3ff" stroke="{INK}" stroke-width="3"/>')

def i_daggers(s):
    for x0, y0, x1, y1 in ((22, 74, 60, 30), (74, 74, 36, 30)):
        s.path(f"M{x0} {y0} L{x1} {y1} L{x1 + (4 if x1 > x0 else -4)} {y1 + 6} Z", s.lin("#e8eef4", "#8a96a2"), sw=2.5)

def i_wraps(s):
    d, bb = rrect(28, 30, 40, 40, 14)
    cel(s, d, "#f2ead8", bb)
    for y in (40, 50, 60):
        s.path(f"M30 {y} L66 {y - 4}", "none", stroke="#c9bfa8", sw=2.5)

def i_beanie(s):
    d = "M18 62 Q16 22 48 20 Q80 22 78 62 Z"
    cel(s, d, "#e8e0d0", (16, 20, 80, 62))
    d, bb = rrect(14, 56, 68, 16, 7)
    cel(s, d, "#c9a27a", bb)
    s.circle(48, 16, 9, "#f6f1e6", sw=2.5)

def i_mushroom_hat(s):
    d = "M12 62 Q14 16 48 14 Q82 16 84 62 Q48 52 12 62 Z"
    cel(s, d, "#d93a2f", (12, 14, 84, 62))
    for x, y, r in ((32, 34, 7), (54, 26, 8), (68, 44, 6)):
        s.circle(x, y, r, "#fff8ec", sw=2)
    s.path("M14 62 Q48 74 82 62 Q48 66 14 62 Z", "#caa98a", sw=2)

def i_helm(s):
    d = "M18 66 Q16 18 48 16 Q80 18 78 66 L66 66 Q64 44 48 44 Q32 44 30 66 Z"
    cel(s, d, "#8f8a82", (16, 16, 80, 66))
    s.path("M48 18 L48 44", "none", stroke=shade("#8f8a82", -0.3), sw=4)
    s.path("M26 40 L40 40 M56 40 L70 40", "none", stroke="#6ae8ff", sw=3)

def i_cloak(s, color):
    d = "M34 18 L62 18 Q66 40 78 78 Q48 86 18 78 Q30 40 34 18 Z"
    cel(s, d, color, (18, 18, 78, 86))
    s.path("M30 22 Q48 30 66 22", "none", stroke=INK, sw=8)
    s.path("M30 22 Q48 30 66 22", "none", stroke="#e8c14a", sw=4)

def i_amulet(s, gem):
    s.path("M22 16 Q48 60 74 16", "none", stroke=INK, sw=6)
    s.path("M22 16 Q48 60 74 16", "none", stroke="#e8c14a", sw=3, extra='stroke-dasharray="5 3"')
    d, bb = ell(48, 62, 16, 18)
    cel(s, d, "#e8c14a", bb, shadow=0.1)
    d, bb = ell(48, 62, 9, 11)
    cel(s, d, gem, bb, shadow=0.1)

def i_ring(s, band, gem):
    s.circle(48, 58, 20, "none", stroke=INK, sw=13)
    s.circle(48, 58, 20, "none", stroke=band, sw=8)
    s.add(f'<polygon points="{star(48, 32, 14, 9, 6)}" fill="{gem}" stroke="{INK}" stroke-width="2.5" stroke-linejoin="round"/>')
    s.circle(44, 29, 3, "#ffffff", stroke=None, extra='opacity="0.8"')

def i_belt(s):
    d = "M10 40 Q48 52 86 40 L86 56 Q48 68 10 56 Z"
    cel(s, d, "#7a4a28", (10, 40, 86, 68))
    d, bb = rrect(38, 40, 20, 22, 4)
    s.path(d, "none", stroke=INK, sw=8)
    s.path(d, "none", stroke="#e8c14a", sw=4)

def i_boots(s, color):
    for dx in (0, 22):
        d = f"M{24 + dx} 20 L{44 + dx} 20 L{44 + dx} 58 L{60 + dx} 62 Q{64 + dx} 76 {54 + dx} 76 L{24 + dx} 76 Z"
        cel(s, d, color if dx else shade(color, -0.12), (24 + dx, 20, 64 + dx, 76))
        s.path(f"M{24 + dx} 28 L{44 + dx} 28", "none", stroke=shade(color, 0.25), sw=4)

def i_bread(s):
    d, bb = ell(48, 54, 34, 22)
    cel(s, d, "#d99a4a", bb)
    for x in (34, 48, 62):
        s.path(f"M{x - 6} 44 Q{x} 52 {x + 6} 44", "none", stroke="#8a5a2a", sw=3)

def i_potion(s, color, big=False):
    r = 24 if big else 19
    d, bb = ell(48, 62, r, r)
    cel(s, d, color, bb, shadow=0.1)
    s.ellipse(40, 54, 6, 9, "#ffffff", stroke=None, extra='opacity="0.55"')
    d, bb = rrect(40, 20, 16, 24, 4)
    s.path(d, "#e8f4ff", sw=3)
    d, bb = rrect(38, 14, 20, 10, 3)
    cel(s, d, "#a0703a", bb, shadow=0)

def i_wool(s):
    d, bb = ell(48, 50, 30, 28)
    cel(s, d, "#f6f1e6", bb)
    for k in range(4):
        s.path(f"M{24 + k * 4} {36 + k * 8} Q48 {30 + k * 10} {72 - k * 4} {36 + k * 8}", "none", stroke="#d8cfbf", sw=3)

def i_feather(s):
    d = "M22 78 Q30 30 74 16 Q70 50 22 78 Z"
    cel(s, d, "#ffd43b", (22, 16, 74, 78))
    s.path("M22 78 Q44 46 70 20", "none", stroke="#c98a10", sw=3)

def i_spore(s):
    d = "M16 54 Q18 20 48 18 Q78 20 80 54 Q48 46 16 54 Z"
    cel(s, d, "#9a6ad0", (16, 18, 80, 54))
    s.path("M36 52 L38 78 L58 78 L60 52", "#e8dcc0", sw=3)
    for x, y in ((30, 36), (52, 28), (64, 40)):
        s.circle(x, y, 4, "#f2e8ff", sw=1.5)

def i_leather(s):
    d = "M20 30 Q30 16 48 22 Q66 16 76 30 Q84 54 70 74 Q48 82 26 74 Q12 54 20 30 Z"
    cel(s, d, "#9a6a3c", (12, 16, 84, 82))
    s.path("M30 36 Q48 44 66 36", "none", stroke="#6b4226", sw=2.5, extra='stroke-dasharray="4 4"')

def i_tusk(s):
    d = "M24 78 Q20 30 66 16 Q48 36 42 78 Z"
    cel(s, d, "#fffaf0", (20, 16, 66, 78))

def i_fur(s):
    for k, x in enumerate((30, 44, 58, 70)):
        s.path(f"M{x - 10} 78 Q{x - 12} 40 {x + 4} {20 + k * 4} Q{x + 6} 50 {x + 8} 78 Z", "#9aa0a8" if k % 2 else "#b8bec6", sw=2.5)

def i_slime(s):
    d = "M16 76 Q18 48 34 44 Q40 22 56 30 Q78 30 78 56 Q84 76 70 78 Z"
    cel(s, d, "#7fd04a", (16, 22, 84, 78))
    s.circle(40, 50, 5, "#ffffff", stroke=None, extra='opacity="0.6"')

def i_wing(s):
    d = "M14 30 Q48 14 82 30 Q74 44 76 62 Q64 50 56 66 Q48 52 40 66 Q32 50 20 62 Q22 44 14 30 Z"
    cel(s, d, "#6b4a8c", (14, 14, 82, 66))

def i_bone(s):
    s.limb([(28, 68), (68, 28)], 10, "#efe8d6", ow=2.5, highlight=False)
    for x, y in ((22, 66), (30, 76), (66, 20), (76, 30)):
        s.circle(x, y, 8, "#efe8d6", sw=2.5)

def i_fragment(s):
    s.path("M22 60 L30 26 L58 16 L78 40 L70 72 L38 80 Z", s.lin("#b0aaa0", "#6f6a62"), sw=3)
    s.path("M40 38 L50 50 L42 62", "none", stroke="#6ae8ff", sw=4)

def i_crown(s):
    d = "M16 70 L14 30 L32 46 L48 20 L64 46 L82 30 L80 70 Z"
    cel(s, d, "#f6f1e6", (14, 20, 82, 70))
    for x, col in ((32, "#e8453c"), (48, "#3b82f6"), (64, "#e8453c")):
        s.circle(x, 60, 5, col, sw=2)


ITEM_ART = {
    "espada_treino": lambda s: i_sword(s), "arco_curto": i_bow, "cajado_galho": lambda s: i_staff(s),
    "varinha_aprendiz": i_wand, "adagas_gemeas": i_daggers, "faixas_treino": i_wraps,
    "lamina_javali": lambda s: i_sword(s, "#fffaf0", "#8a5a36", big=True), "cajado_raiz": lambda s: i_staff(s, "#e8c14a", "#4a3020"),
    "gorro_la": i_beanie, "chapeu_cogumelo": i_mushroom_hat, "elmo_pedra": i_helm,
    "capa_penas": lambda s: i_cloak(s, "#ffd43b"), "capa_lobo": lambda s: i_cloak(s, "#8a8f98"),
    "amuleto_la": lambda s: i_amulet(s, "#f6f1e6"), "amuleto_noite": lambda s: i_amulet(s, "#6b4a8c"), "amuleto_rei": lambda s: i_amulet(s, "#e8453c"),
    "anel_cobre": lambda s: i_ring(s, "#c87a3a", "#e8a060"), "anel_pantano": lambda s: i_ring(s, "#8a9a5a", "#7fd04a"), "anel_osso": lambda s: i_ring(s, "#efe8d6", "#6ae8ff"),
    "cinto_couro": i_belt, "botas_simples": lambda s: i_boots(s, "#9a6a3c"), "botas_viajante": lambda s: i_boots(s, "#3b6fb0"),
    "pao": i_bread, "pocao_pequena": lambda s: i_potion(s, "#e8453c"), "pocao_media": lambda s: i_potion(s, "#d63a8a", big=True),
    "la": i_wool, "pena": i_feather, "esporo": i_spore, "couro": i_leather, "presa_javali": i_tusk, "pelo_lobo": i_fur,
    "gosma": i_slime, "asa_morcego": i_wing, "osso": i_bone, "fragmento": i_fragment, "coroa_la": i_crown,
}


def item_icon(item_id):
    s = item_base(item_id)
    ITEM_ART[item_id](s)
    s.end()
    return s


def build():
    return [spell_icon(k) for k in SPELL_GLYPHS] + [item_icon(k) for k in ITEM_ART]
