using Aldaria.Rules;
using UnityEngine;

namespace Aldaria.Game
{
    /// <summary>Conversão entre células do grid e posições no mundo em projeção isométrica 2:1.</summary>
    public static class Iso
    {
        public const float TileWidth = 1f;
        public const float TileHeight = 0.5f;

        public static Vector3 ToWorld(Cell c) => new Vector3((c.X - c.Y) * TileWidth * 0.5f, -(c.X + c.Y) * TileHeight * 0.5f, 0f);

        public static Cell ToCell(Vector2 world)
        {
            float fx = world.x / TileWidth - world.y / TileHeight;
            float fy = -world.y / TileHeight - world.x / TileWidth;
            return new Cell(Mathf.FloorToInt(fx + 0.5f), Mathf.FloorToInt(fy + 0.5f));
        }

        /// <summary>Ordem de desenho de objetos em pé: quanto mais "para baixo" na tela, mais na frente.</summary>
        public static int SortOrder(float worldY, int sub = 0) => 1000 + Mathf.RoundToInt(-worldY / TileHeight * 20f) + sub;
    }
}
