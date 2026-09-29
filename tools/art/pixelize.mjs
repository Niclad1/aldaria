// Converte a arte do jogo para pixel art com UM tamanho de pixel fixo: 64 pixels por célula.
// 1) renderiza cada SVG (chão, cenário, monstros, NPCs) em resolução baixa
// 2) passa pelo Aseprite (linha de comando) com a paleta fixa art/palette.gpl
// 3) grava em Assets/Resources/Art com filtro nítido (point)
// Os heróis do PixelLab (art/pixellab) já estão nessa escala e só recebem a mesma paleta.
//
// Uso: node pixelize.mjs [--aseprite caminho/do/aseprite] [--palette]  (--palette recria a paleta)
import { chromium } from "playwright-core";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";

export const PIXELS_PER_CELL = 64;
const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, "..", "..");
const src = path.join(root, "art", "src");
const out = path.join(root, "Assets", "Resources", "Art");
const palettePath = path.join(root, "art", "palette.gpl");
const args = process.argv.slice(2);
const asepriteBin = args.includes("--aseprite") ? args[args.indexOf("--aseprite") + 1] : process.env.ASEPRITE || "aseprite";
const tmp = fs.mkdtempSync(path.join(os.tmpdir(), "aldaria-px-"));

const manifest = JSON.parse(fs.readFileSync(path.join(src, "manifest.json"), "utf8"));
const WORLD = new Set(["tile", "prop", "char"]);

function findChromium() {
  const base = process.env.PLAYWRIGHT_BROWSERS_PATH || "/opt/pw-browsers";
  if (fs.existsSync(base))
    for (const d of fs.readdirSync(base).sort().reverse()) {
      const p = path.join(base, d, "chrome-linux", "chrome");
      if (d.startsWith("chromium-") && fs.existsSync(p)) return p;
    }
  return process.env.CHROMIUM_PATH;
}

const browser = await chromium.launch({ executablePath: findChromium() });
const page = await browser.newPage({ deviceScaleFactor: 1 });
const results = [];
for (const m of manifest.sprites) {
  if (!WORLD.has(m.kind)) continue;
  let file = path.join(tmp, m.name + ".src.png"); // sem número no fim: o Aseprite trataria como sequência de quadros
  let scale;
  if (m.png) {
    // sprite do PixelLab: já é pixel art; só ajusta a escala para 64 px por célula
    scale = PIXELS_PER_CELL / m.ppu;
    fs.copyFileSync(path.join(root, "art", "pixellab", m.png), file);
    if (Math.abs(scale - 1) > 0.01) {
      // reamostra com vizinho mais próximo usando o próprio navegador
      const data = fs.readFileSync(file).toString("base64");
      const w = Math.round(m.width * scale), h = Math.round(m.height * scale);
      await page.setViewportSize({ width: w, height: h });
      await page.setContent(`<body style="margin:0;background:transparent"><img src="data:image/png;base64,${data}" style="width:${w}px;height:${h}px;image-rendering:pixelated"></body>`);
      await page.waitForTimeout(50);
      fs.writeFileSync(file, await page.screenshot({ omitBackground: true, clip: { x: 0, y: 0, width: w, height: h } }));
    }
  } else {
    scale = PIXELS_PER_CELL / m.ppu;
    const w = Math.max(1, Math.round(m.width * scale)), h = Math.max(1, Math.round(m.height * scale));
    const svg = fs.readFileSync(path.join(src, m.file), "utf8");
    await page.setViewportSize({ width: w, height: h });
    await page.setContent(`<body style="margin:0;background:transparent;overflow:hidden"><div style="width:${m.width}px;height:${m.height}px;transform:scale(${w / m.width},${h / m.height});transform-origin:0 0">${svg}</div></body>`);
    fs.writeFileSync(file, await page.screenshot({ omitBackground: true, clip: { x: 0, y: 0, width: w, height: h } }));
  }
  results.push({ m, file, scale, keepColors: !!m.png });
}
await browser.close();

if (args.includes("--palette") || !fs.existsSync(palettePath)) {
  execFileSync("python3", [path.join(here, "make_palette.py"), palettePath, ...results.filter((r) => !r.keepColors).map((r) => r.file)], { stdio: "inherit" });
}

const unity = JSON.parse(fs.readFileSync(path.join(out, "manifest.json"), "utf8"));
const byName = Object.fromEntries(unity.sprites.map((s) => [s.name, s]));
let n = 0;
for (const { m, file, scale, keepColors } of results) {
  const px = path.join(tmp, m.name + ".px.png");
  if (keepColors) fs.copyFileSync(file, px); // heróis do PixelLab: já são pixel art, mantêm as cores originais
  else execFileSync(asepriteBin, ["-b", "--script-param", `in=${file}`, "--script-param", `out=${px}`, "--script-param", `palette=${palettePath}`, "--script", path.join(here, "pixelize.lua")], { stdio: "pipe" });
  fs.copyFileSync(px, path.join(out, m.name + ".bytes"));
  const w = Math.round(m.width * scale), h = Math.round(m.height * scale);
  byName[m.name] = { name: m.name, width: w, height: h, pivotX: Math.round(m.pivotX * scale), pivotY: Math.round(m.pivotY * scale), ppu: PIXELS_PER_CELL, border: [0, 0, 0, 0], kind: m.kind, filter: "point" };
  n++;
}
fs.writeFileSync(path.join(out, "manifest.json"), JSON.stringify({ sprites: Object.values(byName) }, null, 1));
fs.rmSync(tmp, { recursive: true, force: true });
console.log(`${n} sprites convertidos para pixel art (${PIXELS_PER_CELL} px por célula)`);
