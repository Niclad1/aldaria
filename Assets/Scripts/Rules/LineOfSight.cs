using System;

namespace Aldaria.Rules
{
    public static class LineOfSight
    {
        /// <summary>
        /// Percorre as células cortadas pela reta entre os centros de "from" e "to".
        /// Qualquer célula intermediária bloqueante corta a visão. Quando a reta passa
        /// exatamente por uma quina, só bloqueia se as duas células vizinhas bloquearem.
        /// </summary>
        public static bool Has(Cell from, Cell to, Func<Cell, bool> blocks)
        {
            int dx = Math.Abs(to.X - from.X);
            int dy = Math.Abs(to.Y - from.Y);
            int sx = Math.Sign(to.X - from.X);
            int sy = Math.Sign(to.Y - from.Y);
            int x = from.X;
            int y = from.Y;
            int err = dx - dy;
            dx *= 2;
            dy *= 2;

            while (x != to.X || y != to.Y)
            {
                if (err > 0)
                {
                    x += sx;
                    err -= dy;
                }
                else if (err < 0)
                {
                    y += sy;
                    err += dx;
                }
                else
                {
                    var a = new Cell(x + sx, y);
                    var b = new Cell(x, y + sy);
                    if (a != to && b != to && blocks(a) && blocks(b)) return false;
                    x += sx;
                    y += sy;
                    err += dx - dy;
                }

                var c = new Cell(x, y);
                if (c != to && blocks(c)) return false;
            }
            return true;
        }
    }
}
