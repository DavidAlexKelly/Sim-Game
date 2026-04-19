namespace SimGame.World
{
    public enum BiomeType
    {
        Ocean,
        Temperate,
        Desert,
        Swamp,
        Tundra,
        Jungle,
        Mountain
    }

    /// <summary>
    /// Static data describing each biome's character.
    /// Used by WorldGen and TileTemperatureProfile.
    /// </summary>
    public static class BiomeData
    {
        /// <summary>
        /// Display name for UI.
        /// </summary>
        public static string Name(BiomeType b) => b switch
        {
            BiomeType.Ocean     => "Ocean",
            BiomeType.Temperate => "Temperate",
            BiomeType.Desert    => "Desert",
            BiomeType.Swamp     => "Swamp",
            BiomeType.Tundra    => "Tundra",
            BiomeType.Jungle    => "Jungle",
            BiomeType.Mountain  => "Mountain",
            _                   => "Unknown"
        };

        /// <summary>
        /// Base ambient temperature offset for this biome in °C.
        /// Added on top of tile-type base temperature.
        /// </summary>
        public static float TemperatureOffset(BiomeType b) => b switch
        {
            BiomeType.Ocean     =>  0f,
            BiomeType.Temperate =>  0f,
            BiomeType.Desert    => 12f,
            BiomeType.Swamp     =>  4f,
            BiomeType.Tundra    => -18f,
            BiomeType.Jungle    =>  8f,
            BiomeType.Mountain  => -8f,
            _                   =>  0f
        };

        /// <summary>
        /// Base moisture level tiles start at in this biome.
        /// </summary>
        public static float BaseMoisture(BiomeType b) => b switch
        {
            BiomeType.Ocean     => 1.0f,
            BiomeType.Temperate => 0.40f,
            BiomeType.Desert    => 0.03f,
            BiomeType.Swamp     => 0.85f,
            BiomeType.Tundra    => 0.20f,
            BiomeType.Jungle    => 0.75f,
            BiomeType.Mountain  => 0.15f,
            _                   => 0.30f
        };

        /// <summary>
        /// Base water table level for this biome.
        /// </summary>
        public static float BaseWaterTable(BiomeType b) => b switch
        {
            BiomeType.Ocean     => 1.0f,
            BiomeType.Temperate => 0.45f,
            BiomeType.Desert    => 0.05f,
            BiomeType.Swamp     => 0.90f,
            BiomeType.Tundra    => 0.25f,
            BiomeType.Jungle    => 0.70f,
            BiomeType.Mountain  => 0.20f,
            _                   => 0.35f
        };
    }
}