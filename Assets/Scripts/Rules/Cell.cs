using System;
using System.Collections.Generic;

namespace Aldaria.Rules
{
    /// <summary>Coordenada de uma célula no grid do mapa (x cresce para baixo-direita, y para baixo-esquerda na tela).</summary>
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int X;
        public readonly int Y;

        public Cell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static readonly Cell[] Directions =
        {
            new Cell(1, 0), new Cell(-1, 0), new Cell(0, 1), new Cell(0, -1)
        };

        /// <summary>Distância em "passos" (Manhattan), igual à contagem de PM no Dofus.</summary>
        public int DistanceTo(Cell other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

        public bool IsInLineWith(Cell other) => X == other.X || Y == other.Y;

        public IEnumerable<Cell> Neighbors()
        {
            foreach (var d in Directions) yield return this + d;
        }

        public static Cell operator +(Cell a, Cell b) => new Cell(a.X + b.X, a.Y + b.Y);
        public static Cell operator -(Cell a, Cell b) => new Cell(a.X - b.X, a.Y - b.Y);
        public static bool operator ==(Cell a, Cell b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(Cell a, Cell b) => !(a == b);

        public bool Equals(Cell other) => this == other;
        public override bool Equals(object obj) => obj is Cell c && this == c;
        public override int GetHashCode() => (X * 73856093) ^ (Y * 19349663);
        public override string ToString() => $"({X},{Y})";
    }
}
