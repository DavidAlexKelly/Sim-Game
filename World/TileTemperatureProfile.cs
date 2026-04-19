namespace SimGame.World
{
    public static class TileTemperatureProfile
    {
        public static float BaseTemp(TileType type) => type switch
        {
            // Water
            TileType.DeepWater         =>  8f,
            TileType.ShallowWater      => 12f,

            // Temperate
            TileType.Sand              => 22f,
            TileType.Grass             => 15f,
            TileType.Forest            => 12f,
            TileType.Stone             => 10f,
            TileType.Mountain          =>  2f,

            // Desert
            TileType.DesertSand        => 35f,
            TileType.DesertRock        => 30f,
            TileType.Oasis             => 22f,

            // Swamp
            TileType.SwampWater        => 18f,
            TileType.SwampGrass        => 20f,
            TileType.SwampForest       => 18f,

            // Tundra
            TileType.Tundra            => -8f,
            TileType.TundraRock        => -10f,
            TileType.FrozenWater       => -15f,

            // Jungle
            TileType.JungleFloor       => 26f,
            TileType.JungleUndergrowth => 25f,
            TileType.JungleCanopy      => 24f,

            // Mountain biome
            TileType.Alpine            =>  2f,
            TileType.Snowcap           => -5f,

            _                          => 15f
        };

        public static float DiurnalAmplitude(TileType type) => type switch
        {
            TileType.DeepWater         =>  2f,
            TileType.ShallowWater      =>  3f,
            TileType.Sand              => 12f,
            TileType.Grass             =>  8f,
            TileType.Forest            =>  5f,
            TileType.Stone             =>  6f,
            TileType.Mountain          =>  7f,

            TileType.DesertSand        => 18f, // extreme swing
            TileType.DesertRock        => 15f,
            TileType.Oasis             =>  8f,

            TileType.SwampWater        =>  3f,
            TileType.SwampGrass        =>  5f,
            TileType.SwampForest       =>  4f,

            TileType.Tundra            =>  6f,
            TileType.TundraRock        =>  5f,
            TileType.FrozenWater       =>  2f,

            TileType.JungleFloor       =>  4f, // canopy dampens swing
            TileType.JungleUndergrowth =>  5f,
            TileType.JungleCanopy      =>  6f,

            TileType.Alpine            =>  8f,
            TileType.Snowcap           =>  4f,

            _                          =>  6f
        };

        public static float ConductionRate(TileType type) => type switch
        {
            TileType.DeepWater         => 0.15f,
            TileType.ShallowWater      => 0.12f,
            TileType.Sand              => 0.05f,
            TileType.Grass             => 0.08f,
            TileType.Forest            => 0.06f,
            TileType.Stone             => 0.10f,
            TileType.Mountain          => 0.08f,

            TileType.DesertSand        => 0.04f,
            TileType.DesertRock        => 0.07f,
            TileType.Oasis             => 0.10f,

            TileType.SwampWater        => 0.14f,
            TileType.SwampGrass        => 0.09f,
            TileType.SwampForest       => 0.07f,

            TileType.Tundra            => 0.06f,
            TileType.TundraRock        => 0.08f,
            TileType.FrozenWater       => 0.12f,

            TileType.JungleFloor       => 0.07f,
            TileType.JungleUndergrowth => 0.06f,
            TileType.JungleCanopy      => 0.05f,

            TileType.Alpine            => 0.09f,
            TileType.Snowcap           => 0.05f,

            _                          => 0.08f
        };

        public static float MoistureFactor(TileType type) => type switch
        {
            TileType.DeepWater         => 0.90f,
            TileType.ShallowWater      => 0.92f,
            TileType.Sand              => 1.00f,
            TileType.Grass             => 0.97f,
            TileType.Forest            => 0.95f,
            TileType.Stone             => 1.00f,
            TileType.Mountain          => 0.98f,

            TileType.DesertSand        => 1.00f,
            TileType.DesertRock        => 1.00f,
            TileType.Oasis             => 0.88f,

            TileType.SwampWater        => 0.80f,
            TileType.SwampGrass        => 0.82f,
            TileType.SwampForest       => 0.80f,

            TileType.Tundra            => 0.96f,
            TileType.TundraRock        => 0.98f,
            TileType.FrozenWater       => 0.95f,

            TileType.JungleFloor       => 0.82f,
            TileType.JungleUndergrowth => 0.80f,
            TileType.JungleCanopy      => 0.78f,

            TileType.Alpine            => 0.95f,
            TileType.Snowcap           => 0.97f,

            _                          => 1.00f
        };

        public static float AltitudeOffset(TileType type) => type switch
        {
            TileType.DeepWater         =>  0.0f,
            TileType.ShallowWater      =>  0.0f,
            TileType.Sand              => -0.5f,
            TileType.Grass             => -1.0f,
            TileType.Forest            => -2.0f,
            TileType.Stone             => -4.0f,
            TileType.Mountain          => -8.0f,

            TileType.DesertSand        => -0.5f,
            TileType.DesertRock        => -2.0f,
            TileType.Oasis             => -0.5f,

            TileType.SwampWater        =>  0.0f,
            TileType.SwampGrass        => -0.5f,
            TileType.SwampForest       => -1.0f,

            TileType.Tundra            => -1.0f,
            TileType.TundraRock        => -3.0f,
            TileType.FrozenWater       =>  0.0f,

            TileType.JungleFloor       => -1.0f,
            TileType.JungleUndergrowth => -1.5f,
            TileType.JungleCanopy      => -2.0f,

            TileType.Alpine            => -6.0f,
            TileType.Snowcap           => -12.0f,

            _                          =>  0.0f
        };

        public static readonly float[] SeasonOffset = { 0f, 12f, 2f, -14f };
    }
}