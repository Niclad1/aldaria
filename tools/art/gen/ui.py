"""Interface no estilo Dofus: painéis azul-marinho com borda fina, destaques dourados e ícones de PV/PA/PM."""
import math
from .lib import Svg, INK, shade, f
from .characters import rrect

NAVY = "#1c2036"
NAVY_DARK = "#12152a"
EDGE = "#3d4670"
GOLD = "#f2c94c"


def _box(name, w, h, fill_top, fill_bottom, edge, radius=8, border=None, edge_w=2, glow=None):
    s = Svg(name, w, h, pivot=(w / 2, h / 2), ppu=100, border=border or [18, 18, 18, 18], kind="ui")
    d, _ = rrect(1.5, 1.5, w - 3, h - 3, radius)
    s.path(d, s.lin(fill_top, fill_bottom), stroke=edge, sw=edge_w)
    d2, _ = rrect(4, 4, w - 8, h - 8, max(2, radius - 3))
    s.path(d2, "none", stroke="#ffffff", sw=1, extra='stroke-opacity="0.06"')
    if glow:
        s.path(f"M{radius + 2} 3 L{w - radius - 2} 3", "none", stroke=glow, sw=1.5, extra='stroke-opacity="0.6"')
    return s


def heart():
    s = Svg("ui_heart", 96, 96, pivot=(48, 48), ppu=96, kind="icon")
    d = "M48 86 C18 64 6 46 10 30 C14 14 34 10 48 26 C62 10 82 14 86 30 C90 46 78 64 48 86 Z"
    s.path(d, s.rad("#ff7a7a", "#a3161f", 0.35, 0.3, 0.9), stroke="#3a0b10", sw=4)
    s.path("M22 32 C24 22 34 20 40 26", "none", stroke="#ffffff", sw=5, extra='stroke-opacity="0.5" stroke-linecap="round"')
    return s


def star():
    s = Svg("ui_star", 96, 96, pivot=(48, 48), ppu=96, kind="icon")
    pts = " ".join(f"{f(48 + math.cos(math.radians(-90 + k * 36)) * (42 if k % 2 == 0 else 19))},{f(50 + math.sin(math.radians(-90 + k * 36)) * (42 if k % 2 == 0 else 19))}" for k in range(10))
    s.add(f'<polygon points="{pts}" fill="{s.rad("#8fd0ff", "#1f5fb8", 0.4, 0.3, 0.9)}" stroke="#0b1d3a" stroke-width="4" stroke-linejoin="round"/>')
    return s


def diamond_icon():
    s = Svg("ui_diamond", 96, 96, pivot=(48, 48), ppu=96, kind="icon")
    s.path("M48 6 L88 48 L48 90 L8 48 Z", s.rad("#9cf07a", "#2a8a2a", 0.4, 0.3, 0.9), stroke="#0d2a0d", sw=4)
    s.path("M48 16 L30 40", "none", stroke="#ffffff", sw=4, extra='stroke-opacity="0.45" stroke-linecap="round"')
    return s


def build():
    return [
        _box("ui_panel", 64, 64, "#232842", NAVY_DARK, EDGE, glow="#5a6699"),
        _box("ui_dark", 48, 48, "#171a2e", "#0f1122", "#2c3354", radius=6, border=[14, 14, 14, 14]),
        _box("ui_button", 64, 40, "#343c63", "#252b4a", "#56609a", radius=8, border=[16, 16, 16, 16]),
        _box("ui_button_hover", 64, 40, "#414b7a", "#2e365c", GOLD, radius=8, border=[16, 16, 16, 16]),
        _box("ui_button_down", 64, 40, "#1f2440", "#2a3152", GOLD, radius=8, border=[16, 16, 16, 16]),
        _box("ui_slot", 48, 48, "#161a2e", "#0e1020", "#39426a", radius=6, border=[14, 14, 14, 14]),
        _box("ui_slot_hover", 48, 48, "#1d2240", "#12152a", "#8e9ad0", radius=6, border=[14, 14, 14, 14]),
        _box("ui_slot_on", 48, 48, "#2a2a1e", "#16150f", GOLD, radius=6, border=[14, 14, 14, 14], edge_w=3),
        heart(), star(), diamond_icon(),
    ]
