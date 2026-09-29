"""Peças da interface (9-slice): pergaminho, couro escuro, botões de madeira e espaços de item."""
from .lib import Svg, INK, shade, f
from .characters import rrect


def panel():
    s = Svg("ui_panel", 96, 96, pivot=(48, 48), ppu=100, border=[30, 30, 30, 30], kind="ui")
    d, _ = rrect(3, 3, 90, 90, 14)
    s.path(d, s.lin("#8a5a30", "#5a3518"), stroke=INK, sw=3)
    d2, _ = rrect(11, 11, 74, 74, 8)
    s.path(d2, s.lin("#fbf1d8", "#ead8b0"), stroke="#5a3518", sw=2)
    nz = s.noise_filter(freq=0.08, octaves=3, seed=4, amount=0.12, color="#b08a50")
    s.path(d2, "#000", stroke=None, extra=f'filter="url(#{nz})"')
    s.path("M8 10 Q48 4 88 10", "none", stroke="#c89060", sw=2, extra='opacity="0.7"')
    for x, y in ((10, 10), (86, 10), (10, 86), (86, 86)):
        s.circle(x, y, 5, s.rad("#fff0b0", "#b8862a"), stroke=INK, sw=2)
    return s


def dark():
    s = Svg("ui_dark", 64, 64, pivot=(32, 32), ppu=100, border=[20, 20, 20, 20], kind="ui")
    d, _ = rrect(2, 2, 60, 60, 12)
    s.path(d, "#b08d57", stroke=INK, sw=2.5)
    d2, _ = rrect(6, 6, 52, 52, 9)
    s.path(d2, s.lin("#3a2c22", "#231a14"), stroke=None, extra='opacity="0.95"')
    return s


def button(name, top, bottom, pressed=False):
    s = Svg(name, 96, 48, pivot=(48, 24), ppu=100, border=[20, 20, 20, 20], kind="ui")
    d, _ = rrect(2, 2 + (2 if pressed else 0), 92, 42, 12)
    s.path(d, s.lin(top, bottom), stroke=INK, sw=3)
    if not pressed:
        d2, _ = rrect(8, 6, 80, 14, 7)
        s.path(d2, "#ffffff", stroke=None, extra='opacity="0.25"')
    s.path("M10 40 Q48 46 86 40", "none", stroke=shade(bottom, -0.25), sw=2, extra='opacity="0.6"')
    return s


def slot(name, rim, fill_top="#4c3828", fill_bottom="#2e2219", width=3):
    s = Svg(name, 64, 64, pivot=(32, 32), ppu=100, border=[18, 18, 18, 18], kind="ui")
    d, _ = rrect(2, 2, 60, 60, 11)
    s.path(d, s.lin(shade(rim, 0.15), shade(rim, -0.2)), stroke=INK, sw=2.5)
    d2, _ = rrect(2 + width + 2, 2 + width + 2, 60 - 2 * (width + 2), 60 - 2 * (width + 2), 8)
    s.path(d2, s.lin(fill_top, fill_bottom), stroke=INK, sw=1.5)
    s.path("M12 14 Q32 8 52 14", "none", stroke="#000000", sw=4, extra='opacity="0.25"')
    return s


def build():
    return [panel(), dark(),
            button("ui_button", "#f0a24a", "#b8661d"), button("ui_button_hover", "#ffb866", "#c97a2c"), button("ui_button_down", "#c8741e", "#9a5516", pressed=True),
            slot("ui_slot", "#b08d57"), slot("ui_slot_hover", "#f0d08a", "#5e4632", "#3a2a1f"), slot("ui_slot_on", "#ffd54f", "#7a5530", "#4d3520", width=4)]
