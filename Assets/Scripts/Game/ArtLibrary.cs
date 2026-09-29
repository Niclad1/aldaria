using System;
using System.Collections.Generic;
using Aldaria.Rules;
using UnityEngine;

namespace Aldaria.Game
{
    /// <summary>
    /// Carrega a arte feita em SVG (pasta art/ do projeto), já convertida em PNG pelo
    /// tools/art/render.mjs e guardada em Resources/Art como .bytes. Se algum desenho
    /// faltar, cai nos desenhos gerados por código (Art.cs), então o jogo nunca quebra.
    /// </summary>
    public static partial class Art
    {
#pragma warning disable 0649 // preenchidos pelo JsonUtility
        [Serializable]
        class SpriteMeta
        {
            public string name;
            public int width;
            public int height;
            public float pivotX;
            public float pivotY;
            public float ppu;
            public int[] border;
            public string kind;
            public string filter;
        }

        [Serializable]
        class Manifest
        {
            public SpriteMeta[] sprites;
        }
#pragma warning restore 0649

        static Dictionary<string, SpriteMeta> manifest;
        static readonly Dictionary<string, Sprite> library = new Dictionary<string, Sprite>();
        static readonly Dictionary<Sprite, float> contentTop = new Dictionary<Sprite, float>();
        static Font fontRegular, fontBold;
        static bool fontsLoaded;

        static void LoadManifest()
        {
            if (manifest != null) return;
            manifest = new Dictionary<string, SpriteMeta>();
            var asset = Resources.Load<TextAsset>("Art/manifest");
            if (asset == null) return;
            var m = JsonUtility.FromJson<Manifest>(asset.text);
            if (m?.sprites == null) return;
            foreach (var s in m.sprites) manifest[s.name] = s;
        }

        /// <summary>Sprite da arte nova pelo nome (ex.: "tree_meadow_0"), ou null se não existir.</summary>
        public static Sprite Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (library.TryGetValue(name, out var cached)) return cached;
            LoadManifest();
            Sprite sprite = null;
            if (manifest.TryGetValue(name, out var meta))
            {
                var data = Resources.Load<TextAsset>("Art/" + name);
                if (data != null)
                {
                    bool pixel = meta.filter == "point";
                    bool world = meta.kind != "ui" && meta.kind != "icon" && !pixel;
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, world)
                    {
                        name = name,
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = pixel ? FilterMode.Point : world ? FilterMode.Trilinear : FilterMode.Bilinear,
                        anisoLevel = 0,
                    };
                    if (tex.LoadImage(data.bytes, false))
                    {
                        if (world) tex.mipMapBias = -0.4f;
                        var pivot = new Vector2(meta.pivotX / tex.width, meta.pivotY / tex.height);
                        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, meta.ppu, 0, SpriteMeshType.FullRect);
                        sprite.name = name;
                        if (meta.kind == "char") contentTop[sprite] = MeasureTop(tex, meta);
                    }
                    Resources.UnloadAsset(data);
                }
            }
            library[name] = sprite;
            return sprite;
        }

        /// <summary>Altura (em unidades do mundo) do ponto mais alto desenhado acima do pivô. Usado para barras de vida e marcadores.</summary>
        public static float ContentTop(Sprite s)
        {
            if (s == null) return 1f;
            if (contentTop.TryGetValue(s, out var top)) return top;
            return s.bounds.max.y;
        }

        static float MeasureTop(Texture2D tex, SpriteMeta meta)
        {
            var px = tex.GetPixels32();
            for (int y = tex.height - 1; y >= 0; y--)
                for (int x = 0; x < tex.width; x += 2)
                    if (px[y * tex.width + x].a > 40) return (y - meta.pivotY) / meta.ppu;
            return tex.height / meta.ppu;
        }

        public static RectOffset UiBorder(string name)
        {
            LoadManifest();
            if (manifest.TryGetValue(name, out var m) && m.border != null && m.border.Length == 4)
                return new RectOffset(m.border[0], m.border[1], m.border[2], m.border[3]);
            return new RectOffset(9, 9, 9, 9);
        }

        // ------------------------------------------------------------------ atalhos usados pelo jogo

        public static Sprite Ground(GridMap map, Cell c)
        {
            var s = Get(Visuals.Ground(map, c));
            if (s != null) return s;
            // Sem a arte nova, o caminho volta a ser um tile inteiro.
            return LegacyTile(map[c], Visuals.Variant(map, c));
        }

        public static Sprite Prop(GridMap map, Cell c)
        {
            var name = Visuals.Prop(map, c);
            if (name == null) return null;
            var s = Get(name);
            if (s != null) return s;
            int v = Visuals.Variant(map, c);
            switch (map[c])
            {
                case Tile.Tree: return LegacyTree(v);
                case Tile.Bush: case Tile.Fence: return LegacyBush(v);
                default: return LegacyRock(v);
            }
        }

        public static Sprite Character(string visual) =>
            Get("char_" + visual) ?? LegacyCharacter(LegacyId(visual));

        static string LegacyId(string visual)
        {
            switch (visual)
            {
                case "guerreiro": return "guardiao";
                case "arqueiro": return "sentinela";
                case "mago": return "druida";
                case "assassino": return "sentinela";
                default: return visual.StartsWith("npc_") ? "guardiao" : visual;
            }
        }

        public static Texture2D SpellIcon(SpellDef s) => Get("icon_spell_" + s.Id)?.texture ?? LegacySpellIcon(s);

        public static Texture2D ItemIcon(ItemDef item)
        {
            if (item == null) return White;
            var s = Get("icon_item_" + item.Id);
            if (s != null) return s.texture;
            return Tex("legacy_item_" + item.Id, () =>
            {
                var p = new Painter(64, 64);
                var col = item.Kind == ItemKind.Consumable ? Hex("#e8453c") : item.Kind == ItemKind.Resource ? Hex("#b5773a") : Hex("#e8c14a");
                p.Fill(Painter.Circle(32, 32, 24), Ink, 2.5f);
                p.Fill(Painter.Circle(32, 32, 24), col);
                return p.ToTexture();
            });
        }

        /// <summary>Textura de interface (painel, botão...) da arte nova, ou a versão desenhada por código.</summary>
        public static Texture2D Ui(string name, Func<Texture2D> fallback) => Get(name)?.texture ?? fallback();

        public static Font UiFont(bool bold)
        {
            if (!fontsLoaded)
            {
                fontsLoaded = true;
                fontRegular = Resources.Load<Font>("Fonts/Fredoka-Medium");
                fontBold = Resources.Load<Font>("Fonts/Fredoka-SemiBold");
            }
            return bold ? (fontBold != null ? fontBold : fontRegular) : fontRegular;
        }

        /// <summary>Desenha só a cabeça/parte de cima do personagem (retratos da interface).</summary>
        public static void DrawPortrait(Rect r, Sprite s)
        {
            if (s == null) return;
            var tex = s.texture;
            float top = ContentTop(s) * s.pixelsPerUnit + s.pivot.y;   // em pixels
            float size = Mathf.Min(tex.width, top * 0.62f);
            float x0 = Mathf.Clamp(s.pivot.x - size * 0.5f, 0, tex.width - size);
            float y0 = Mathf.Max(0, top - size);
            GUI.DrawTextureWithTexCoords(r, tex, new Rect(x0 / tex.width, y0 / tex.height, size / tex.width, size / tex.height));
        }
    }
}
