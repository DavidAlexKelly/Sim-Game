using Microsoft.Xna.Framework;
using SimGame.Entities;

namespace SimGame.Core
{
    public enum SelectionKind { None, Tile, Character }

    /// <summary>
    /// Holds whatever the player last clicked on.
    /// Only one thing can be selected at a time.
    /// </summary>
    public class SelectionState
    {
        public SelectionKind Kind      { get; private set; } = SelectionKind.None;
        public Character?    Character { get; private set; }
        public Point         TileCoord { get; private set; }

        public void SelectCharacter(Character c)
        {
            Kind      = SelectionKind.Character;
            Character = c;
            TileCoord = default;
        }

        public void SelectTile(int tx, int ty)
        {
            Kind      = SelectionKind.Tile;
            TileCoord = new Point(tx, ty);
            Character = null;
        }

        public void Clear()
        {
            Kind      = SelectionKind.None;
            Character = null;
            TileCoord = default;
        }
    }
}