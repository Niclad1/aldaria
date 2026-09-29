"""
Gera os arquivos SVG da arte do Aldaria em art/src/.

Os SVGs são a "fonte" da arte: podem ser abertos e editados no Inkscape, Illustrator
ou Figma. Depois rode `node render.mjs` para convertê-los em PNG para a Unity.

Atenção: rodar este gerador de novo SOBRESCREVE os SVGs. Se você editou algum
à mão, gere só os grupos que quiser: python3 generate.py tiles props
"""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))

from gen import tiles  # noqa: E402

GROUPS = {"tiles": tiles.build}
for mod in ("props", "characters", "monsters", "icons", "ui"):
    try:
        GROUPS[mod] = __import__(f"gen.{mod}", fromlist=["build"]).build
    except ModuleNotFoundError:
        pass

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "art", "src")


def main():
    wanted = sys.argv[1:] or list(GROUPS)
    os.makedirs(SRC, exist_ok=True)
    manifest_path = os.path.join(SRC, "manifest.json")
    manifest = {}
    if os.path.exists(manifest_path):
        for m in json.load(open(manifest_path))["sprites"]:
            manifest[m["name"]] = m
    # os SVGs de personagens jogáveis foram substituídos pelos do PixelLab
    for group in wanted:
        for svg in GROUPS[group]():
            with open(os.path.join(SRC, svg.name + ".svg"), "w") as fh:
                fh.write(svg.to_string())
            meta = svg.meta()
            meta["group"] = group
            manifest[svg.name] = meta
        print(f"{group}: ok")
    with open(manifest_path, "w") as fh:
        json.dump({"sprites": sorted(manifest.values(), key=lambda m: m["name"])}, fh, indent=1, ensure_ascii=False)


if __name__ == "__main__":
    main()
