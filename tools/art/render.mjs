// Converte art/src/*.svg em PNG e coloca em Assets/Resources/Art/ (como .bytes, que a Unity
// carrega sem precisar de configuração de importação).
//
// Uso:  node render.mjs            -> renderiza tudo
//       node render.mjs tile_       -> só os arquivos cujo nome começa com "tile_"
//       node render.mjs --png pasta -> também salva cópias .png numa pasta (para conferir)
import { chromium } from "playwright-core";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, "..", "..");
const src = path.join(root, "art", "src");
const out = path.join(root, "Assets", "Resources", "Art");

const args = process.argv.slice(2);
let pngDir = null;
const filters = [];
for (let i = 0; i < args.length; i++) {
  if (args[i] === "--png") pngDir = args[++i];
  else filters.push(args[i]);
}

function findChromium() {
  if (process.env.CHROMIUM_PATH) return process.env.CHROMIUM_PATH;
  const base = process.env.PLAYWRIGHT_BROWSERS_PATH || "/opt/pw-browsers";
  if (fs.existsSync(base)) {
    for (const d of fs.readdirSync(base).sort().reverse()) {
      const p = path.join(base, d, "chrome-linux", "chrome");
      if (d.startsWith("chromium-") && fs.existsSync(p)) return p;
    }
  }
  return undefined; // deixa o playwright procurar sozinho
}

const manifest = JSON.parse(fs.readFileSync(path.join(src, "manifest.json"), "utf8"));
const sprites = manifest.sprites.filter((m) => filters.length === 0 || filters.some((f) => m.name.startsWith(f)));
fs.mkdirSync(out, { recursive: true });
if (pngDir) fs.mkdirSync(pngDir, { recursive: true });

const browser = await chromium.launch({ executablePath: findChromium() });
const page = await browser.newPage({ deviceScaleFactor: 1 });
let done = 0;
for (const m of sprites) {
  if (m.png) {
    // Sprite pronto (PixelLab): só copia o PNG.
    const png = fs.readFileSync(path.join(root, "art", "pixellab", m.png));
    fs.writeFileSync(path.join(out, m.name + ".bytes"), png);
    if (pngDir) fs.writeFileSync(path.join(pngDir, m.name + ".png"), png);
    done++;
    continue;
  }
  const svg = fs.readFileSync(path.join(src, m.file), "utf8");
  await page.setViewportSize({ width: m.width, height: m.height });
  await page.setContent(`<!doctype html><html><body style="margin:0;background:transparent;overflow:hidden">${svg}</body></html>`);
  const png = await page.screenshot({ omitBackground: true, clip: { x: 0, y: 0, width: m.width, height: m.height } });
  fs.writeFileSync(path.join(out, m.name + ".bytes"), png);
  if (pngDir) fs.writeFileSync(path.join(pngDir, m.name + ".png"), png);
  done++;
}
await browser.close();

// Manifesto que a Unity lê para saber pivô e tamanho de cada sprite.
const unityManifest = manifest.sprites.map(({ name, width, height, pivotX, pivotY, ppu, border, kind, filter }) => ({ name, width, height, pivotX, pivotY, ppu, border: border || [0, 0, 0, 0], kind, filter: filter || "smooth" }));
fs.writeFileSync(path.join(out, "manifest.json"), JSON.stringify({ sprites: unityManifest }, null, 1));
console.log(`${done} sprites renderizados em ${path.relative(root, out)}`);
