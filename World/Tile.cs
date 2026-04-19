using Microsoft.Xna.Framework;

namespace SimGame.World
{
    public enum TileType
    {
        // ── Shared / Water ────────────────────────────────────────────────────
        DeepWater,
        ShallowWater,

        // ── Temperate ─────────────────────────────────────────────────────────
        Sand,
        Grass,
        Forest,
        Stone,
        Mountain,

        // ── Desert ───────────────────────────────────────────────────────────
        DesertSand,
        DesertRock,
        Oasis,

        // ── Swamp ─────────────────────────────────────────────────────────────
        SwampWater,
        SwampGrass,
        SwampForest,

        // ── Tundra ────────────────────────────────────────────────────────────
        Tundra,
        TundraRock,
        FrozenWater,

        // ── Jungle ────────────────────────────────────────────────────────────
        JungleFloor,
        JungleUndergrowth,
        JungleCanopy,

        // ── Mountain biome ────────────────────────────────────────────────────
        Alpine,
        Snowcap
    }

    public enum FoodSourceType
    {
        None,
        BerryBush,
        FruitTree,
        CactusFruit,
        SwampBerry,
        TundraLichen,
        JungleFruit
    }

    public struct FoodSource
    {
        public FoodSourceType Type;
        public int            Yield;
        public int            RespawnTicksRemaining;

        public bool IsAvailable => Type != FoodSourceType.None
                                && Yield > 0
                                && RespawnTicksRemaining <= 0;

        public static FoodSource Empty => new FoodSource { Type = FoodSourceType.None };

        public static FoodSource Create(FoodSourceType type) => new FoodSource
        {
            Type                  = type,
            Yield                 = MaxYield(type),
            RespawnTicksRemaining = 0
        };

        public static int MaxYield(FoodSourceType type) => type switch
        {
            FoodSourceType.BerryBush     => 4,
            FoodSourceType.FruitTree     => 8,
            FoodSourceType.CactusFruit   => 3,
            FoodSourceType.SwampBerry    => 5,
            FoodSourceType.TundraLichen  => 2,
            FoodSourceType.JungleFruit   => 10,
            _                            => 0
        };

        public static int RespawnTicks(FoodSourceType type) => type switch
        {
            FoodSourceType.BerryBush     => 120,
            FoodSourceType.FruitTree     => 200,
            FoodSourceType.CactusFruit   => 300, // slow — desert is harsh
            FoodSourceType.SwampBerry    => 80,
            FoodSourceType.TundraLichen  => 400, // very slow
            FoodSourceType.JungleFruit   => 60,  // fast — jungle is abundant
            _                            => 0
        };

        public static Color OverlayColor(FoodSourceType type) => type switch
        {
            FoodSourceType.BerryBush    => new Color(160, 60,  160),
            FoodSourceType.FruitTree    => new Color(220, 140, 30),
            FoodSourceType.CactusFruit  => new Color(80,  180, 80),
            FoodSourceType.SwampBerry   => new Color(100, 180, 120),
            FoodSourceType.TundraLichen => new Color(180, 180, 100),
            FoodSourceType.JungleFruit  => new Color(220, 60,  60),
            _                           => Color.Transparent
        };
    }

    public struct Tile
    {
        public TileType   Type;
        public bool       IsWalkable;
        public Color      Color;
        public FoodSource Food;
        public BiomeType  Biome;

        // ── Stage 1 ───────────────────────────────────────────────────────────
        public float Elevation;
        public float Temperature;
        public float BaseTemperature;

        // ── Stage 2 ───────────────────────────────────────────────────────────
        public float SurfaceMoisture;
        public int   DroughtTicks;
        public float WaterTable;
        public float SnowCover;

        public static Tile FromType(TileType type, BiomeType biome = BiomeType.Temperate)
            => new Tile
            {
                Type       = type,
                Biome      = biome,
                IsWalkable = IsWalkableType(type),
                Color      = ColourForType(type),
                Food            = FoodSource.Empty,
                Elevation       = 0f,
                Temperature     = 0f,
                BaseTemperature = 0f,
                SurfaceMoisture = 0f,
                DroughtTicks    = 0,
                WaterTable      = 0f,
                SnowCover       = 0f
            };

        public static bool IsWalkableType(TileType type) => type switch
        {
            TileType.DeepWater   => false,
            TileType.ShallowWater => false,
            TileType.Mountain    => false,
            TileType.SwampWater  => false,
            TileType.FrozenWater => false,
            TileType.Snowcap     => false,
            _                    => true
        };

        public static Color ColourForType(TileType type) => type switch
        {
            // Water
            TileType.DeepWater         => new Color(20,  60,  120),
            TileType.ShallowWater      => new Color(50,  110, 170),

            // Temperate
            TileType.Sand              => new Color(210, 195, 130),
            TileType.Grass             => new Color(85,  145, 70),
            TileType.Forest            => new Color(40,  100, 45),
            TileType.Stone             => new Color(130, 125, 120),
            TileType.Mountain          => new Color(160, 155, 150),

            // Desert
            TileType.DesertSand        => new Color(220, 185, 100),
            TileType.DesertRock        => new Color(175, 140, 90),
            TileType.Oasis             => new Color(60,  160, 100),

            // Swamp
            TileType.SwampWater        => new Color(50,  80,  50),
            TileType.SwampGrass        => new Color(80,  110, 60),
            TileType.SwampForest       => new Color(45,  75,  40),

            // Tundra
            TileType.Tundra            => new Color(170, 185, 170),
            TileType.TundraRock        => new Color(140, 145, 140),
            TileType.FrozenWater       => new Color(180, 210, 230),

            // Jungle
            TileType.JungleFloor       => new Color(50,  100, 40),
            TileType.JungleUndergrowth => new Color(35,  120, 45),
            TileType.JungleCanopy      => new Color(20,  80,  25),

            // Mountain biome
            TileType.Alpine            => new Color(120, 140, 110),
            TileType.Snowcap           => new Color(235, 240, 245),

            _                          => Color.Magenta
        };
    }
}