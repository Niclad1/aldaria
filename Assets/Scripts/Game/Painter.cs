using UnityEngine;

namespace Aldaria.Game
{
    /// <summary>
    /// Mini rasterizador para desenhar sprites por código: formas com anti-aliasing,
    /// recortes e sombreamento. Assim o jogo não precisa de nenhum arquivo de imagem
    /// para rodar (fica leve), e a arte definitiva pode substituir isso depois.
    /// </summary>
    public sealed class Painter
    {
        public delegate float Sdf(Vector2 p);
        public delegate Color ColorFn(Vector2 p);

        public readonly int Width;
        public readonly int Height;
        readonly Color[] px;

        public Painter(int width, int height)
        {
            Width = width;
            Height = height;
            px = new Color[width * height];
        }

        // ------------------------------------------------------------------ desenho

        public void Fill(Sdf shape, Color color, float grow = 0f, Sdf clip = null, float soft = 1f) =>
            Fill(shape, _ => color, grow, clip, soft);

        public void Fill(Sdf shape, ColorFn shader, float grow = 0f, Sdf clip = null, float soft = 1f)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float cov = Mathf.Clamp01(0.5f - (shape(p) - grow) / soft);
                    if (cov <= 0f) continue;
                    if (clip != null) cov = Mathf.Min(cov, Mathf.Clamp01(0.5f - clip(p)));
                    if (cov <= 0f) continue;
                    Blend(y * Width + x, shader(p), cov);
                }
            }
        }

        /// <summary>Desenha várias formas com um contorno único em volta de todas (estilo "adesivo").</summary>
        public void Outline(Color color, float width, params Sdf[] shapes)
        {
            foreach (var s in shapes) Fill(s, color, width);
        }

        void Blend(int i, Color c, float coverage)
        {
            var d = px[i];
            float sa = c.a * coverage;
            float oa = sa + d.a * (1f - sa);
            if (oa <= 0f) return;
            px[i] = new Color(
                (c.r * sa + d.r * d.a * (1f - sa)) / oa,
                (c.g * sa + d.g * d.a * (1f - sa)) / oa,
                (c.b * sa + d.b * d.a * (1f - sa)) / oa,
                oa);
        }

        public Texture2D ToTexture()
        {
            BleedTransparentEdges();
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels(px);
            tex.Apply(false, false);
            return tex;
        }

        public Sprite ToSprite(Vector2 pivotPixels, float pixelsPerUnit)
        {
            var tex = ToTexture();
            return Sprite.Create(tex, new Rect(0, 0, Width, Height), new Vector2(pivotPixels.x / Width, pivotPixels.y / Height), pixelsPerUnit, 0, SpriteMeshType.FullRect);
        }

        /// <summary>Copia a cor dos vizinhos para pixels transparentes, evitando bordas escuras com filtro bilinear.</summary>
        void BleedTransparentEdges()
        {
            var copy = (Color[])px.Clone();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    if (copy[i].a > 0f) continue;
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0);
                        int ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                        var n = copy[ny * Width + nx];
                        if (n.a <= 0f) continue;
                        px[i] = new Color(n.r, n.g, n.b, 0f);
                        break;
                    }
                }
            }
        }

        // ------------------------------------------------------------------ formas (funções de distância com sinal)

        public static Sdf Circle(float cx, float cy, float r) => p => Vector2.Distance(p, new Vector2(cx, cy)) - r;

        public static Sdf Ellipse(float cx, float cy, float rx, float ry) => p =>
        {
            var q = new Vector2((p.x - cx) / rx, (p.y - cy) / ry);
            return (q.magnitude - 1f) * Mathf.Min(rx, ry);
        };

        public static Sdf Box(float cx, float cy, float hw, float hh, float radius = 0f) => p =>
        {
            float qx = Mathf.Abs(p.x - cx) - (hw - radius);
            float qy = Mathf.Abs(p.y - cy) - (hh - radius);
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        };

        public static Sdf Capsule(float ax, float ay, float bx, float by, float r) => p =>
        {
            var a = new Vector2(ax, ay);
            var ba = new Vector2(bx, by) - a;
            var pa = p - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        };

        public static Sdf Ring(float cx, float cy, float r, float halfWidth) => p =>
            Mathf.Abs(Vector2.Distance(p, new Vector2(cx, cy)) - r) - halfWidth;

        public static Sdf Polygon(params Vector2[] v) => p =>
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                var e = v[j] - v[i];
                var w = p - v[i];
                var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        };

        public static Sdf Above(float y) => p => y - p.y;
        public static Sdf Below(float y) => p => p.y - y;
        public static Sdf RightOf(float x) => p => x - p.x;
        public static Sdf Union(Sdf a, Sdf b) => p => Mathf.Min(a(p), b(p));
        public static Sdf Intersect(Sdf a, Sdf b) => p => Mathf.Max(a(p), b(p));
        public static Sdf Subtract(Sdf a, Sdf b) => p => Mathf.Max(a(p), -b(p));
        public static Sdf Offset(Sdf a, float d) => p => a(p) - d;

        // ------------------------------------------------------------------ ruído

        public static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 144665);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        public static float ValueNoise(float x, float y, int seed)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(ix, iy, seed), b = Hash(ix + 1, iy, seed);
            float c = Hash(ix, iy + 1, seed), d = Hash(ix + 1, iy + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }
    }
}
