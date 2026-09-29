using System.Collections.Generic;
using Aldaria.Rules;
using UnityEngine;

namespace Aldaria.Game
{
    /// <summary>Desenha um GridMap: tiles, cenário, saídas e as camadas de destaque (alcance, caminho...).</summary>
    public sealed class WorldView : MonoBehaviour
    {
        public GridMap Map { get; private set; }
        public Transform Focus;

        readonly Dictionary<string, List<SpriteRenderer>> layers = new Dictionary<string, List<SpriteRenderer>>();
        readonly Stack<SpriteRenderer> pool = new Stack<SpriteRenderer>();
        readonly List<SpriteRenderer> props = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> exitMarkers = new List<SpriteRenderer>();
        Transform overlayRoot;

        public static WorldView Build(GridMap map, Transform parent)
        {
            var go = new GameObject($"Mapa [{map.MapX},{map.MapY}]");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<WorldView>();
            view.Map = map;
            view.overlayRoot = new GameObject("Destaques").transform;
            view.overlayRoot.SetParent(go.transform, false);

            var tiles = new GameObject("Tiles").transform;
            tiles.SetParent(go.transform, false);
            foreach (var c in map.Cells())
            {
                int variant = (int)(Painter.Hash(c.X, c.Y, map.MapX * 13 + map.MapY) * 1000f);
                var tile = new GameObject($"{c}").AddComponent<SpriteRenderer>();
                tile.transform.SetParent(tiles, false);
                tile.transform.position = Iso.ToWorld(c);
                tile.sprite = Art.TileSprite(map[c], variant);
                tile.sortingOrder = c.X + c.Y;

                Sprite prop = null;
                if (map[c] == Tile.Tree) prop = Art.Tree(variant % 11 == 0 ? 2 : variant % 2);
                else if (map[c] == Tile.Rock) prop = Art.Rock(variant);
                else if (map[c] == Tile.Bush) prop = Art.Bush(variant);
                if (prop != null)
                {
                    var r = new GameObject("Cenário").AddComponent<SpriteRenderer>();
                    r.transform.SetParent(tile.transform, false);
                    r.sprite = prop;
                    r.sortingOrder = Iso.SortOrder(tile.transform.position.y, 1);
                    view.props.Add(r);
                }
            }

            foreach (var exit in map.Exits)
            {
                var r = new GameObject("Saída").AddComponent<SpriteRenderer>();
                r.transform.SetParent(go.transform, false);
                r.transform.position = Iso.ToWorld(exit);
                r.sprite = Art.CellFill;
                r.sortingOrder = 650;
                view.exitMarkers.Add(r);
            }
            return view;
        }

        public void SetLayer(string layer, IEnumerable<Cell> cells, Color color, int order = 600, Sprite sprite = null)
        {
            ClearLayer(layer);
            if (cells == null) return;
            if (!layers.TryGetValue(layer, out var list)) layers[layer] = list = new List<SpriteRenderer>();
            foreach (var c in cells)
            {
                var r = pool.Count > 0 ? pool.Pop() : NewOverlay();
                r.gameObject.SetActive(true);
                r.transform.position = Iso.ToWorld(c);
                r.sprite = sprite != null ? sprite : Art.CellFill;
                r.color = color;
                r.sortingOrder = order;
                list.Add(r);
            }
        }

        public void ClearLayer(string layer)
        {
            if (!layers.TryGetValue(layer, out var list)) return;
            foreach (var r in list)
            {
                r.gameObject.SetActive(false);
                pool.Push(r);
            }
            list.Clear();
        }

        public void ShowGrid(bool on)
        {
            if (!on)
            {
                ClearLayer("grid");
                return;
            }
            var cells = new List<Cell>();
            foreach (var c in Map.Cells())
                if (Map.IsWalkable(c)) cells.Add(c);
            SetLayer("grid", cells, new Color(0.1f, 0.15f, 0.05f, 0.28f), 550, Art.CellFrame);
        }

        public void SetExitsVisible(bool on)
        {
            foreach (var r in exitMarkers) r.enabled = on;
        }

        SpriteRenderer NewOverlay()
        {
            var r = new GameObject("Destaque").AddComponent<SpriteRenderer>();
            r.transform.SetParent(overlayRoot, false);
            return r;
        }

        void Update()
        {
            float pulse = 0.35f + Mathf.Sin(Time.time * 3f) * 0.15f;
            foreach (var r in exitMarkers) r.color = new Color(1f, 0.82f, 0.3f, pulse);

            // Árvores na frente do personagem ficam transparentes para ele não sumir atrás delas.
            if (Focus == null) return;
            var fp = Focus.position;
            foreach (var r in props)
            {
                var b = r.bounds;
                bool inFront = r.transform.position.y < fp.y - 0.01f;
                bool covers = inFront && fp.x > b.min.x + 0.1f && fp.x < b.max.x - 0.1f && fp.y + 0.4f > b.min.y && fp.y < b.max.y;
                var col = r.color;
                col.a = Mathf.MoveTowards(col.a, covers ? 0.45f : 1f, Time.deltaTime * 3f);
                r.color = col;
            }
        }
    }
}
