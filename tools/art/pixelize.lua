-- Aseprite (linha de comando): transforma um PNG em pixel art com a paleta fixa do jogo.
-- Uso: aseprite -b --script-param in=entrada.png --script-param out=saida.png --script-param palette=palette.gpl --script pixelize.lua
local p = app.params
local spr = app.open(p["in"])
local img = spr.cels[1].image
local pc = app.pixelColor
-- sem transparência parcial: cada pixel é cheio ou vazio
for it in img:pixels() do
  local c = it()
  if pc.rgbaA(c) < 110 then
    it(pc.rgba(0, 0, 0, 0))
  else
    it(pc.rgba(pc.rgbaR(c), pc.rgbaG(c), pc.rgbaB(c), 255))
  end
end
spr:setPalette(Palette{ fromFile = p["palette"] })
app.command.ChangePixelFormat{ format = "indexed", dithering = "none" }
spr:saveCopyAs(p["out"])
spr:close()
