using System.Collections.Generic;
using Aldaria.Rules;
using UnityEngine;
using static Aldaria.Game.Painter;

namespace Aldaria.Game
{
    /// <summary>
    /// Toda a arte do protótipo, desenhada por código e guardada em cache.
    /// Para usar arte feita à mão, basta trocar o que estes métodos devolvem por sprites importados.
    /// </summary>
    public static class Art
    {
        public const float TilePpu = 128f;
        public const float CharacterPpu = 128f;
        public const float PropPpu = 150f;

        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        static readonly Color Ink = Hex("#2a1d17");

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        static Sprite Cached(string key, System.Func<Sprite> make)
        {
            if (!sprites.TryGetValue(key, out var s))
            {
                s = make();
                s.name = key;
                s.texture.name = key;
                sprites[key] = s;
            }
            return s;
        }

        public static Color ElementColor(Element e)
        {
            switch (e)
            {
                case Element.Earth: return Hex("#b5773a");
                case Element.Fire: return Hex("#e8572e");
                case Element.Water: return Hex("#3a8fd9");
                case Element.Air: return Hex("#4fb866");
                default: return Hex("#a0a0a0");
            }
        }

        // ================================================================== terreno

        public static Sprite TileSprite(Tile kind, int variant)
        {
            if (kind == Tile.Tree || kind == Tile.Rock || kind == Tile.Bush) kind = Tile.Grass;
            variant = kind == Tile.Water ? 0 : variant % 4;
            return Cached($"tile_{kind}_{variant}", () => DrawTile(kind, variant));
        }

        // Textura 128x84: losango de 128x64 em cima e 20 px de "barranco" embaixo.
        static readonly Vector2 TL = new Vector2(0, 52), TT = new Vector2(64, 84), TR = new Vector2(128, 52), TB = new Vector2(64, 20);

        static Sprite DrawTile(Tile kind, int variant)
        {
            var p = new Painter(128, 84);
            int seed = (int)kind * 31 + variant * 7 + 3;

            // Barranco (só aparece nas bordas do mapa; no meio fica escondido pelo tile da frente).
            p.Fill(Polygon(TL, TB, new Vector2(64, 0), new Vector2(0, 32)), q => Color.Lerp(Hex("#6e4a2c"), Hex("#9a6b40"), q.y / 52f));
            p.Fill(Polygon(TB, TR, new Vector2(128, 32), new Vector2(64, 0)), q => Color.Lerp(Hex("#4f341f"), Hex("#77512f"), q.y / 52f));
            p.Fill(Polygon(TL, TB, TR, TR - new Vector2(0, 6), TB - new Vector2(0, 6), TL - new Vector2(0, 6)),
                kind == Tile.Path ? Hex("#8e7147") : kind == Tile.Water ? Hex("#2c5f86") : Hex("#3f6b22"));

            var top = Polygon(TL, TT, TR, TB);
            ColorFn surface;
            switch (kind)
            {
                case Tile.Path:
                    surface = q =>
                    {
                        float n = ValueNoise(q.x / 14f, q.y / 9f, seed) * 0.7f + Hash((int)q.x, (int)q.y, seed) * 0.3f;
                        return Color.Lerp(Hex("#c79f63"), Hex("#e2c283"), n);
                    };
                    break;
                case Tile.Water:
                    surface = q =>
                    {
                        float w = Mathf.Sin(q.x * 0.22f + q.y * 0.5f) * 0.5f + 0.5f;
                        float n = ValueNoise(q.x / 20f, q.y / 10f, seed);
                        return Color.Lerp(Hex("#2f7fb8"), Hex("#56aee0"), w * 0.35f + n * 0.5f);
                    };
                    break;
                default:
                    var dark = Color.Lerp(Hex("#5d9632"), Hex("#4f8a2e"), variant / 3f);
                    var light = Color.Lerp(Hex("#8bc34a"), Hex("#9ccc4f"), variant / 3f);
                    surface = q =>
                    {
                        float n = ValueNoise(q.x / 18f, q.y / 10f, seed) * 0.75f + Hash((int)q.x, (int)q.y, seed) * 0.25f;
                        return Color.Lerp(dark, light, n);
                    };
                    break;
            }
            p.Fill(top, surface, 0.7f);

            // Bisel: borda de cima clara, de baixo escura — dá volume ao tile.
            var rim = Subtract(top, Offset(top, -3f));
            p.Fill(rim, new Color(1f, 1f, 0.85f, 0.22f), 0f, Above(52));
            p.Fill(rim, new Color(0f, 0f, 0f, 0.18f), 0f, Below(52));

            var rng = new System.Random(seed);
            if (kind == Tile.Grass || kind == Tile.Flowers)
            {
                for (int i = 0; i < 14; i++)
                {
                    var c = RandomInside(top, rng, 6f);
                    float h = 3f + (float)rng.NextDouble() * 3f;
                    p.Fill(Capsule(c.x, c.y, c.x + (float)rng.NextDouble() * 2f - 1f, c.y + h, 0.8f), Alpha(Hex("#3e6e1f"), 0.7f));
                }
            }
            if (kind == Tile.Flowers)
            {
                string[] petals = { "#ffffff", "#ffe066", "#ff8fb1", "#b39ddb" };
                for (int i = 0; i < 7; i++)
                {
                    var c = RandomInside(top, rng, 8f);
                    var col = Hex(petals[rng.Next(petals.Length)]);
                    for (int k = 0; k < 4; k++)
                    {
                        float a = k * Mathf.PI / 2f;
                        p.Fill(Circle(c.x + Mathf.Cos(a) * 2f, c.y + Mathf.Sin(a) * 1.4f, 1.7f), col);
                    }
                    p.Fill(Circle(c.x, c.y, 1.2f), Hex("#f5a623"));
                }
            }
            if (kind == Tile.Path)
            {
                for (int i = 0; i < 6; i++)
                {
                    var c = RandomInside(top, rng, 6f);
                    p.Fill(Ellipse(c.x, c.y, 2.5f + (float)rng.NextDouble() * 2f, 1.6f), Alpha(Hex("#8f7048"), 0.8f));
                }
            }
            if (kind == Tile.Water)
            {
                p.Fill(Subtract(top, Offset(top, -5f)), new Color(0f, 0.1f, 0.2f, 0.25f), 0f, Above(52));
                for (int i = 0; i < 4; i++)
                {
                    var c = RandomInside(top, rng, 12f);
                    p.Fill(Capsule(c.x - 5f, c.y, c.x + 5f, c.y, 0.9f), new Color(1f, 1f, 1f, 0.45f));
                }
            }
            return p.ToSprite(new Vector2(64, 52), TilePpu);
        }

        static Vector2 RandomInside(Sdf shape, System.Random rng, float margin)
        {
            for (int i = 0; i < 50; i++)
            {
                var v = new Vector2((float)rng.NextDouble() * 128f, 20f + (float)rng.NextDouble() * 64f);
                if (shape(v) < -margin) return v;
            }
            return new Vector2(64, 52);
        }

        /// <summary>Losango preenchido e arredondado, branco (a cor vem do SpriteRenderer). Usado para alcance, caminho etc.</summary>
        public static Sprite CellFill => Cached("cell_fill", () =>
        {
            var p = new Painter(128, 64);
            var d = Polygon(new Vector2(12, 32), new Vector2(64, 58), new Vector2(116, 32), new Vector2(64, 6));
            p.Fill(d, new Color(1, 1, 1, 0.55f), 2.5f);
            p.Fill(Subtract(Offset(d, 2.5f), Offset(d, -0.5f)), Color.white);
            return p.ToSprite(new Vector2(64, 32), TilePpu);
        });

        /// <summary>Contorno fino de célula, para o quadriculado do modo de luta.</summary>
        public static Sprite CellFrame => Cached("cell_frame", () =>
        {
            var p = new Painter(128, 64);
            var d = Polygon(new Vector2(1, 32), new Vector2(64, 63), new Vector2(127, 32), new Vector2(64, 1));
            p.Fill(Subtract(d, Offset(d, -1.6f)), Color.white);
            return p.ToSprite(new Vector2(64, 32), TilePpu);
        });

        public static Sprite Shadow => Cached("shadow", () =>
        {
            var p = new Painter(80, 40);
            p.Fill(Ellipse(40, 20, 30, 13), new Color(0, 0, 0, 0.38f), -3f, null, 7f);
            return p.ToSprite(new Vector2(40, 20), TilePpu);
        });

        public static Sprite TeamRing => Cached("ring", () =>
        {
            var p = new Painter(100, 52);
            p.Fill(p2 => Mathf.Abs(Ellipse(50, 26, 42, 20)(p2)) - 2.6f, Color.white);
            p.Fill(Ellipse(50, 26, 42, 20), new Color(1, 1, 1, 0.22f));
            return p.ToSprite(new Vector2(50, 26), TilePpu);
        });

        public static Sprite Glow => Cached("glow", () =>
        {
            var p = new Painter(48, 48);
            p.Fill(Circle(24, 24, 10), Color.white, 0f, null, 18f);
            p.Fill(Circle(24, 24, 6), Color.white);
            return p.ToSprite(new Vector2(24, 24), 96f);
        });

        public static Texture2D White => Tex("white", () =>
        {
            var p = new Painter(4, 4);
            p.Fill(_ => -10f, Color.white);
            return p.ToTexture();
        });

        static Texture2D Tex(string key, System.Func<Texture2D> make)
        {
            if (!textures.TryGetValue(key, out var t))
            {
                t = make();
                t.name = key;
                textures[key] = t;
            }
            return t;
        }

        // ================================================================== cenário

        public static Sprite Tree(int variant) => Cached($"tree_{variant % 3}", () => DrawTree(variant % 3));

        static Sprite DrawTree(int variant)
        {
            var p = new Painter(128, 200);
            var rng = new System.Random(variant * 17 + 5);
            string[][] palettes =
            {
                new[] { "#2f5d1e", "#4f8f2a", "#7cc242" },
                new[] { "#23502e", "#3b7a43", "#63a860" },
                new[] { "#6b3d12", "#c46a1f", "#f0a33a" },
            };
            var pal = palettes[variant];
            var outline = Hex("#1c2a12");

            p.Fill(Ellipse(64, 16, 40, 13), new Color(0, 0, 0, 0.28f), -2f, null, 6f);

            var trunk = Box(64, 52, 9, 40, 5);
            var root1 = Ellipse(52, 16, 9, 5);
            var root2 = Ellipse(76, 16, 9, 5);
            p.Outline(Hex("#2d1a0e"), 3f, trunk, root1, root2);
            ColorFn bark = q => Color.Lerp(Hex("#6a4126"), Hex("#8f5b36"), Mathf.Clamp01((72f - q.x) / 16f));
            p.Fill(trunk, bark);
            p.Fill(root1, bark);
            p.Fill(root2, bark);

            var blobs = new List<Vector3>
            {
                new Vector3(64, 128, 44), new Vector3(34, 112, 28), new Vector3(94, 112, 28),
                new Vector3(46, 156, 30), new Vector3(84, 154, 30), new Vector3(64, 176, 22),
            };
            for (int i = 0; i < blobs.Count; i++)
            {
                var b = blobs[i];
                blobs[i] = new Vector3(b.x + rng.Next(-4, 5), b.y + rng.Next(-4, 5), b.z + rng.Next(-3, 3));
            }
            var shapes = blobs.ConvertAll(b => Circle(b.x, b.y, b.z)).ToArray();
            p.Outline(outline, 3f, shapes);
            ColorFn leaves = q => Color.Lerp(Hex(pal[0]), Hex(pal[1]), Mathf.Clamp01((q.y - 84f) / 90f));
            foreach (var s in shapes) p.Fill(s, leaves);
            foreach (var b in blobs)
                p.Fill(Circle(b.x - b.z * 0.25f, b.y + b.z * 0.3f, b.z * 0.62f), Alpha(Hex(pal[2]), 0.55f), 0f, Circle(b.x, b.y, b.z), 3f);
            for (int i = 0; i < 10; i++)
            {
                var b = blobs[rng.Next(blobs.Count)];
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = (float)rng.NextDouble() * b.z * 0.7f;
                p.Fill(Ellipse(b.x + Mathf.Cos(a) * r, b.y + Mathf.Sin(a) * r, 3f, 2f), Alpha(Hex(pal[2]), 0.8f));
            }
            return p.ToSprite(new Vector2(64, 16), PropPpu);
        }

        public static Sprite Rock(int variant) => Cached($"rock_{variant % 2}", () =>
        {
            var p = new Painter(96, 80);
            p.Fill(Ellipse(48, 16, 34, 11), new Color(0, 0, 0, 0.28f), -2f, null, 5f);
            var body = variant % 2 == 0
                ? Polygon(new Vector2(16, 14), new Vector2(12, 34), new Vector2(26, 56), new Vector2(52, 62), new Vector2(76, 50), new Vector2(84, 26), new Vector2(70, 10), new Vector2(34, 8))
                : Polygon(new Vector2(20, 12), new Vector2(18, 38), new Vector2(40, 50), new Vector2(66, 46), new Vector2(80, 22), new Vector2(64, 8));
            p.Fill(body, Hex("#33302e"), 3f);
            p.Fill(body, q => Color.Lerp(Hex("#6f6a66"), Hex("#a39d97"), Mathf.Clamp01((q.y - 8f) / 50f)));
            p.Fill(Circle(36, 44, 18), new Color(1, 1, 1, 0.18f), 0f, body, 4f);
            p.Fill(Capsule(46, 40, 54, 26, 1f), Alpha(Hex("#4a4542"), 0.8f));
            p.Fill(Capsule(54, 26, 62, 22, 1f), Alpha(Hex("#4a4542"), 0.8f));
            return p.ToSprite(new Vector2(48, 16), TilePpu);
        });

        public static Sprite Bush(int variant) => Cached($"bush_{variant % 2}", () =>
        {
            var p = new Painter(96, 72);
            var rng = new System.Random(variant + 40);
            p.Fill(Ellipse(48, 14, 36, 10), new Color(0, 0, 0, 0.25f), -2f, null, 5f);
            var shapes = new[] { Circle(30, 30, 16), Circle(50, 38, 20), Circle(68, 28, 16), Circle(48, 22, 16) };
            p.Outline(Hex("#1c2a12"), 3f, shapes);
            foreach (var s in shapes) p.Fill(s, q => Color.Lerp(Hex("#3b6e24"), Hex("#6aa83a"), Mathf.Clamp01((q.y - 10f) / 45f)));
            string berry = variant % 2 == 0 ? "#e53935" : "#7e57c2";
            for (int i = 0; i < 6; i++) p.Fill(Circle(24 + rng.Next(48), 20 + rng.Next(28), 2.6f), Hex(berry));
            return p.ToSprite(new Vector2(48, 14), TilePpu);
        });

        // ================================================================== personagens

        public static Sprite Character(string visual) => Cached("char_" + visual, () =>
        {
            switch (visual)
            {
                case "lanudo": return DrawLanudo();
                case "pipio": return DrawPipio();
                case "cogumelo": return DrawCogumelo();
                default: return DrawHero(visual);
            }
        });

        static Sprite DrawHero(string classId)
        {
            var p = new Painter(96, 128);
            Color skin, main, trim, legs = Hex("#4a3b30"), boots = Hex("#3a2a20");
            switch (classId)
            {
                case "sentinela": skin = Hex("#e8b98f"); main = Hex("#3f8f4e"); trim = Hex("#c89b5a"); break;
                case "druida": skin = Hex("#d9a37a"); main = Hex("#8a6a3c"); trim = Hex("#6fae3f"); break;
                default: skin = Hex("#f2c9a0"); main = Hex("#b8413e"); trim = Hex("#e0c068"); break;
            }

            var legL = Capsule(40, 10, 40, 26, 5.5f);
            var legR = Capsule(56, 10, 56, 26, 5.5f);
            var body = classId == "druida" ? Box(48, 34, 18, 21, 9) : Box(48, 38, 16, 15, 8);
            var armB = Capsule(32, 48, 27, 33, 5f);
            var armF = Capsule(64, 48, 70, 34, 5f);
            var handB = Circle(27, 31, 5.5f);
            var handF = Circle(70, 32, 5.5f);
            var head = Circle(48, 78, 26);

            // Peças específicas de cada classe
            Sdf[] extra;
            switch (classId)
            {
                case "sentinela":
                    extra = new[]
                    {
                        Circle(48, 80, 30),
                        Polygon(new Vector2(22, 86), new Vector2(38, 104), new Vector2(8, 110)),
                        Intersect(Ring(58, 40, 24, 2.4f), RightOf(70)),
                    };
                    break;
                case "druida":
                    extra = new[]
                    {
                        Circle(45, 80, 29),
                        Ellipse(48, 100, 36, 8),
                        Polygon(new Vector2(28, 100), new Vector2(68, 100), new Vector2(58, 126)),
                        Capsule(78, 8, 81, 90, 2.8f),
                        Circle(81, 96, 6.5f),
                    };
                    break;
                default:
                    extra = new[]
                    {
                        Intersect(Circle(48, 82, 29), Above(76)),
                        Ellipse(44, 110, 14, 6.5f),
                        Capsule(74, 38, 86, 80, 3.2f),
                        Circle(32, 50, 7.5f),
                        Circle(64, 50, 7.5f),
                    };
                    break;
            }

            // 1) contorno único da silhueta
            var all = new List<Sdf> { legL, legR, body, armB, armF, handB, handF, head };
            all.AddRange(extra);
            p.Outline(Ink, 2.6f, all.ToArray());

            // 2) preenchimentos
            if (classId == "druida") p.Fill(extra[0], Hex("#5b7a2e")); // cabelo atrás da cabeça
            if (classId == "sentinela") { p.Fill(extra[0], Hex("#2f6e3b")); p.Fill(extra[1], Hex("#2f6e3b")); }

            p.Fill(legL, legs);
            p.Fill(legR, legs);
            p.Fill(legL, boots, 0f, Below(16));
            p.Fill(legR, boots, 0f, Below(16));
            p.Fill(armB, Darken(main, 0.15f));
            p.Fill(handB, skin);
            p.Fill(body, main);
            p.Fill(body, new Color(0, 0, 0, 0.18f), 0f, Circle(64, 22, 20));
            p.Fill(body, new Color(1, 1, 1, 0.14f), 0f, Circle(38, 50, 10), 3f);
            p.Fill(body, trim, 0f, Box(48, classId == "druida" ? 30 : 29, 20, 2.6f));

            if (classId == "sentinela")
            {
                p.Fill(extra[2], Hex("#8b5a2b"));
                p.Fill(Capsule(71, 17, 71, 63, 0.8f), Hex("#efe6d0"));
            }
            if (classId == "druida")
            {
                p.Fill(extra[3], Hex("#7a4e2a"));
                p.Fill(extra[4], Hex("#8ef06a"));
                p.Fill(Circle(79, 98, 2f), new Color(1, 1, 1, 0.8f));
            }
            if (classId == "guardiao")
            {
                p.Fill(extra[2], q => Color.Lerp(Hex("#aab4be"), Hex("#eef3f7"), (q.x - 72f) / 14f));
                p.Fill(Capsule(66, 40, 80, 34, 2.8f), trim);
            }

            // cabeça
            if (classId == "sentinela")
            {
                p.Fill(Circle(50, 76, 21), skin);
                p.Fill(Circle(50, 76, 21), new Color(0, 0, 0, 0.12f), 0f, Subtract(Circle(50, 76, 21), Circle(46, 82, 21)));
            }
            else
            {
                p.Fill(head, skin);
                p.Fill(head, new Color(0, 0, 0, 0.1f), 0f, Subtract(head, Circle(43, 85, 26)));
            }

            float eyeY = 76f;
            DrawEyes(p, 45, 60, eyeY, 5f, 6.5f);
            p.Fill(Capsule(51, 64, 56, 65, 1.1f), Ink);
            p.Fill(Circle(38, 67, 4), new Color(1f, 0.45f, 0.45f, 0.35f));
            p.Fill(Circle(66, 67, 4), new Color(1f, 0.45f, 0.45f, 0.35f));

            if (classId == "guardiao")
            {
                p.Fill(extra[0], q => Color.Lerp(Hex("#8c96a0"), Hex("#d7dee5"), Mathf.Clamp01((q.y - 76f) / 30f)));
                p.Fill(Box(48, 78, 29, 2.6f, 1.5f), Hex("#6c7680"));
                p.Fill(Capsule(48, 78, 48, 104, 2.2f), Hex("#9aa4ae"));
                p.Fill(extra[1], Hex("#d43c35"));
                p.Fill(extra[3], trim);
                p.Fill(extra[4], trim);
            }
            if (classId == "druida")
            {
                p.Fill(extra[1], Hex("#6b4a2a"));
                p.Fill(extra[2], q => Color.Lerp(Hex("#6b4a2a"), Hex("#8c6538"), (q.y - 100f) / 26f));
                p.Fill(Ellipse(62, 106, 9, 4), trim);
                p.Fill(Ellipse(54, 104, 6, 3), Darken(trim, 0.2f));
            }

            p.Fill(armF, main);
            p.Fill(handF, skin);
            return p.ToSprite(new Vector2(48, 8), CharacterPpu);
        }

        static void DrawEyes(Painter p, float x1, float x2, float y, float rx, float ry)
        {
            foreach (var x in new[] { x1, x2 })
            {
                p.Fill(Ellipse(x, y, rx, ry), Color.white);
                p.Fill(Ellipse(x + rx * 0.3f, y - 0.5f, rx * 0.62f, ry * 0.7f), Ink);
                p.Fill(Circle(x + rx * 0.5f, y + ry * 0.25f, rx * 0.25f), Color.white);
            }
        }

        static Color Darken(Color c, float amount) => Color.Lerp(c, Color.black, amount);

        static Sprite DrawLanudo()
        {
            var p = new Painter(110, 96);
            var legs = new[] { Capsule(30, 8, 30, 22, 4.5f), Capsule(44, 8, 44, 22, 4.5f), Capsule(62, 8, 62, 22, 4.5f), Capsule(76, 8, 76, 22, 4.5f) };
            var wool = new[]
            {
                Circle(36, 40, 17), Circle(54, 46, 19), Circle(70, 38, 14), Circle(44, 58, 14),
                Circle(62, 60, 13), Circle(28, 30, 12), Circle(52, 28, 15), Circle(72, 28, 11),
            };
            var face = Ellipse(84, 44, 13, 14);
            var hornL = Ring(76, 58, 7, 2.6f);
            var hornR = Ring(94, 58, 6, 2.6f);
            var all = new List<Sdf>(legs) { face, hornL, hornR };
            all.AddRange(wool);
            p.Outline(Ink, 2.6f, all.ToArray());
            foreach (var l in legs) p.Fill(l, Hex("#3a2e2a"));
            foreach (var w in wool) p.Fill(w, q => Color.Lerp(Hex("#d8cfc0"), Hex("#fbf7ef"), Mathf.Clamp01((q.y - 16f) / 50f)));
            foreach (var w in wool) p.Fill(Offset(w, -8f), new Color(1, 1, 1, 0.45f), 0f, null, 5f);
            p.Fill(hornL, Hex("#a07845"));
            p.Fill(hornR, Hex("#a07845"));
            p.Fill(face, Hex("#4a3a35"));
            DrawEyes(p, 80, 91, 47, 3.6f, 4.6f);
            p.Fill(Capsule(84, 36, 90, 37, 1f), Hex("#1a1210"));
            return p.ToSprite(new Vector2(54, 8), CharacterPpu);
        }

        static Sprite DrawPipio()
        {
            var p = new Painter(96, 96);
            var body = Circle(46, 38, 27);
            var beak = Polygon(new Vector2(70, 46), new Vector2(88, 40), new Vector2(70, 32));
            var tuft = new[] { Ellipse(40, 68, 4, 8), Ellipse(47, 70, 4, 9), Ellipse(54, 68, 4, 8) };
            var feet = new[] { Capsule(38, 6, 40, 14, 2.2f), Capsule(54, 6, 52, 14, 2.2f) };
            var all = new List<Sdf> { body, beak };
            all.AddRange(tuft);
            all.AddRange(feet);
            p.Outline(Ink, 2.6f, all.ToArray());
            foreach (var f in feet) p.Fill(f, Hex("#ff8a3d"));
            foreach (var t in tuft) p.Fill(t, Hex("#f2b705"));
            p.Fill(body, q => Color.Lerp(Hex("#f2b705"), Hex("#ffe066"), Mathf.Clamp01((q.y - 12f) / 50f)));
            p.Fill(Ellipse(50, 28, 17, 12), Hex("#fff3b8"));
            p.Fill(Ellipse(32, 38, 9, 13), Hex("#e0a100"));
            p.Fill(beak, Hex("#ff8a3d"));
            DrawEyes(p, 56, 68, 48, 4.5f, 6f);
            return p.ToSprite(new Vector2(46, 8), CharacterPpu);
        }

        static Sprite DrawCogumelo()
        {
            var p = new Painter(96, 100);
            var stem = Box(48, 28, 15, 18, 8);
            var cap = Intersect(Ellipse(48, 56, 38, 28), Above(42));
            var feet = new[] { Ellipse(38, 10, 7, 4), Ellipse(58, 10, 7, 4) };
            p.Outline(Ink, 2.6f, stem, cap, feet[0], feet[1]);
            foreach (var f in feet) p.Fill(f, Hex("#8a6a4a"));
            p.Fill(stem, q => Color.Lerp(Hex("#d9c7a3"), Hex("#f4e8cf"), Mathf.Clamp01((q.y - 10f) / 30f)));
            p.Fill(cap, q => Color.Lerp(Hex("#b8322b"), Hex("#e5534a"), Mathf.Clamp01((q.y - 42f) / 36f)));
            p.Fill(Ellipse(48, 44, 30, 4), Hex("#caa98a"), 0f, Above(42));
            foreach (var s in new[] { Circle(30, 58, 5), Circle(50, 70, 6), Circle(68, 56, 5), Circle(40, 48, 3.5f), Circle(62, 46, 3f) })
                p.Fill(s, new Color(1f, 0.97f, 0.9f), 0f, cap);
            DrawEyes(p, 42, 55, 28, 3.8f, 4.8f);
            p.Fill(Capsule(37, 36, 45, 33, 1.3f), Ink);
            p.Fill(Capsule(51, 33, 59, 36, 1.3f), Ink);
            p.Fill(Capsule(46, 19, 52, 19, 1.1f), Ink);
            return p.ToSprite(new Vector2(48, 7), CharacterPpu);
        }

        // ================================================================== ícones de feitiço

        public static Texture2D SpellIcon(SpellDef s) => Tex("icon_" + s.Id, () =>
        {
            var p = new Painter(64, 64);
            var col = ElementColor(s.Element);
            var disc = Circle(32, 32, 27);
            p.Fill(disc, Ink, 2.5f);
            p.Fill(disc, q => Color.Lerp(Darken(col, 0.35f), Color.Lerp(col, Color.white, 0.25f), q.y / 64f));
            p.Fill(Circle(26, 42, 14), new Color(1, 1, 1, 0.2f), 0f, disc, 4f);
            var glyph = new Color(1f, 0.98f, 0.92f);
            if (s.Effect == SpellEffect.Heal)
            {
                p.Fill(Box(32, 32, 4, 13, 2), glyph);
                p.Fill(Box(32, 32, 13, 4, 2), glyph);
            }
            else if (s.Effect == SpellEffect.Teleport)
            {
                p.Fill(Capsule(20, 22, 40, 42, 3.2f), glyph);
                p.Fill(Polygon(new Vector2(46, 48), new Vector2(30, 46), new Vector2(44, 32)), glyph);
            }
            else if (s.Effect == SpellEffect.Buff)
            {
                p.Fill(Polygon(new Vector2(18, 26), new Vector2(32, 42), new Vector2(46, 26), new Vector2(46, 18), new Vector2(32, 34), new Vector2(18, 18)), glyph);
                p.Fill(Polygon(new Vector2(18, 38), new Vector2(32, 54), new Vector2(46, 38), new Vector2(46, 30), new Vector2(32, 46), new Vector2(18, 30)), glyph);
            }
            else if (s.Area > 0)
            {
                p.Fill(Circle(32, 32, 6), glyph);
                p.Fill(Ring(32, 32, 14, 2.4f), glyph);
            }
            else if (s.Push > 0)
            {
                p.Fill(Polygon(new Vector2(16, 18), new Vector2(30, 32), new Vector2(16, 46), new Vector2(22, 46), new Vector2(36, 32), new Vector2(22, 18)), glyph);
                p.Fill(Polygon(new Vector2(30, 18), new Vector2(44, 32), new Vector2(30, 46), new Vector2(36, 46), new Vector2(50, 32), new Vector2(36, 18)), glyph);
            }
            else if (s.MpSteal > 0)
            {
                p.Fill(Ring(26, 32, 8, 2.2f), glyph);
                p.Fill(Ring(38, 32, 8, 2.2f), glyph);
            }
            else if (s.MaxRange > 1)
            {
                p.Fill(Capsule(16, 16, 44, 44, 2.2f), glyph);
                p.Fill(Polygon(new Vector2(50, 50), new Vector2(36, 46), new Vector2(46, 36)), glyph);
            }
            else
            {
                p.Fill(Capsule(18, 18, 46, 46, 3.2f), glyph);
                p.Fill(Capsule(20, 34, 34, 20, 2.4f), glyph);
            }
            return p.ToTexture();
        });

        // ================================================================== interface

        /// <summary>Retângulo arredondado para painéis e botões da interface (usado com 9-slice).</summary>
        public static Texture2D UiBox(string key, Color fill, Color border, Color? fillBottom = null, float radius = 6f, float borderWidth = 2f) => Tex("ui_" + key, () =>
        {
            var p = new Painter(32, 32);
            var outer = Box(16, 16, 16, 16, radius);
            var inner = Box(16, 16, 16 - borderWidth, 16 - borderWidth, Mathf.Max(0f, radius - borderWidth));
            p.Fill(outer, border);
            var bottom = fillBottom ?? fill;
            p.Fill(inner, q => Color.Lerp(bottom, fill, q.y / 32f));
            p.Fill(Intersect(inner, Subtract(Box(16, 16, 16, 16), Box(16, 15, 16, 16))), new Color(1, 1, 1, 0.25f));
            return p.ToTexture();
        });
    }
}
