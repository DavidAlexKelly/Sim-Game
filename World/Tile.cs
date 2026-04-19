using Microsoft.Xna.Framework;

namespace SimGame.World
{
    public enum TileType
    {
        DeepWater,
        ShallowWater,
        Sand,
        Grass,
        Forest,
        Stone,
        Mountain
    }

    public struct Tile
    {
        public TileType Type;
        public bool     IsWalkable;
        public Color    Color;

        public static Tile FromType(TileType type) => new Tile
        {
            Type       = type,
            IsWalkable = type is not TileType.DeepWater
                              and not TileType.ShallowWater
                              and not TileType.Mountain,
            Color = type switch
            {
                TileType.DeepWater    => new Color(30,  80,  140),
                TileType.ShallowWater => new Color(60,  120, 180),
                TileType.Sand         => new Color(210, 195, 130),
                TileType.Grass        => new Color(85,  145, 70),
                TileType.Forest       => new Color(40,  100, 45),
                TileType.Stone        => new Color(130, 125, 120),
                TileType.Mountain     => new Color(180, 175, 170),
                _                     => Color.Magenta
            }
        };
    }
}
