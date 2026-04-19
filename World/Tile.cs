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

    // ── Food source layer ────────────────────────────────────────────────────

    public enum FoodSourceType
    {
        None,
        BerryBush,
        FruitTree
    }

    /// <summary>
    /// Optional food source sitting on top of a walkable tile.
    /// Tracks current yield and respawn cooldown independently of terrain.
    /// </summary>
    public struct FoodSource
    {
        public FoodSourceType Type;

        /// <summary>
        /// Portions remaining. A character eats one portion per Eating tick.
        /// When it hits 0 the source is depleted and starts respawning.
        /// </summary>
        public int Yield;

        /// <summary>
        /// Ticks remaining before the source regrows. 0 means available.
        /// </summary>
        public int RespawnTicksRemaining;

        public bool IsAvailable => Type != FoodSourceType.None
                                && Yield > 0
                                && RespawnTicksRemaining <= 0;

        public static FoodSource Empty => new FoodSource { Type = FoodSourceType.None };

        public static FoodSource Create(FoodSourceType type) => new FoodSource
        {
            Type                   = type,
            Yield                  = MaxYield(type),
            RespawnTicksRemaining  = 0
        };

        public static int MaxYield(FoodSourceType type) => type switch
        {
            FoodSourceType.BerryBush  => 4,
            FoodSourceType.FruitTree  => 8,
            _                         => 0
        };

        /// <summary>
        /// Ticks before a depleted source regrows to full yield.
        /// </summary>
        public static int RespawnTicks(FoodSourceType type) => type switch
        {
            FoodSourceType.BerryBush  => 120,
            FoodSourceType.FruitTree  => 200,
            _                         => 0
        };

        /// <summary>
        /// Display colour used by the renderer to tint food source tiles.
        /// </summary>
        public static Color OverlayColor(FoodSourceType type) => type switch
        {
            FoodSourceType.BerryBush => new Color(160, 60,  160),   // purple
            FoodSourceType.FruitTree => new Color(220, 140, 30),    // amber
            _                        => Color.Transparent
        };
    }

    // ── Tile ─────────────────────────────────────────────────────────────────

    public struct Tile
    {
        public TileType    Type;
        public bool        IsWalkable;
        public Color       Color;
        public FoodSource  Food;

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
            },
            Food = FoodSource.Empty
        };
    }
}