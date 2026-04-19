namespace SimGame.World
{
    /// <summary>
    /// Classifies biome from elevation + climate temperature + precipitation.
    ///
    /// At our scale (1 degree of latitude) the entire map shares essentially
    /// the same base climate. Biome variety therefore comes primarily from
    /// ELEVATION within that climate — the same latitude produces:
    ///
    ///   Tropical latitude:
    ///     Low elev  → Jungle / Swamp
    ///     Mid elev  → Jungle / Forest
    ///     High elev → Alpine / Snowcap
    ///
    ///   Temperate latitude:
    ///     Low elev  → Grass / Swamp (if wet)
    ///     Mid elev  → Forest / Grass
    ///     High elev → Stone / Alpine / Snowcap
    ///
    ///   Arid/subtropical latitude:
    ///     Low elev  → Desert / Sand
    ///     Mid elev  → Desert Rock / Stone
    ///     High elev → Alpine / Snowcap
    ///
    ///   Polar/tundra latitude:
    ///     Low elev  → Tundra / Frozen Water
    ///     Mid elev  → Tundra Rock
    ///     High elev → Snowcap
    ///
    /// The lapse rate in ClimateMap already reduces temperature with
    /// elevation, so high-elevation tiles naturally get colder climate
    /// values fed into this classifier.
    /// </summary>
    public static class BiomeClassifier
    {
        // ── Elevation thresholds ──────────────────────────────────────────────
        private const float WaterMax    = 0.35f; // below = water (lake/ocean)
        private const float LowlandMax  = 0.50f; // low elevation land
        private const float MidlandMax  = 0.68f; // mid elevation
        private const float HighlandMax = 0.80f; // high elevation
                                                  // above = mountain peaks

        // ── Climate temperature thresholds ────────────────────────────────────
        // These represent the EFFECTIVE temperature after lapse rate applied
        // so high-elevation tiles already have lower values here
        private const float PolarMax      = 0.22f; // genuinely polar
        private const float ColdMax       = 0.38f; // cold temperate
        private const float TemperateMax  = 0.58f; // temperate
        private const float WarmMax       = 0.70f; // warm / subtropical
                                                    // above = tropical/hot

        // ── Precipitation thresholds ──────────────────────────────────────────
        private const float AridMax    = 0.28f;
        private const float DryMax     = 0.42f;
        private const float WetMin     = 0.60f;
        private const float VeryWetMin = 0.72f;

        public static BiomeType Classify(
            float elevation,
            float climateTemp,
            float precipitation)
        {
            // ── Water ─────────────────────────────────────────────────────────
            if (elevation < WaterMax) return BiomeType.Ocean;

            // ── Polar — cold at any elevation ─────────────────────────────────
            // Because lapse rate is applied in ClimateMap, high-elevation
            // tiles in temperate climates will also fall into this range
            if (climateTemp < PolarMax)
                return BiomeType.Tundra;

            // ── Mountain peaks — high elevation in any non-polar climate ──────
            if (elevation > HighlandMax)
                return BiomeType.Mountain;

            // ── Cold temperate ────────────────────────────────────────────────
            if (climateTemp < ColdMax)
            {
                // Cold + wet highlands → tundra-like
                if (elevation > MidlandMax)
                    return BiomeType.Tundra;

                // Cold + wet lowlands → temperate forest/swamp
                if (precipitation > WetMin && elevation < LowlandMax)
                    return BiomeType.Swamp;

                return BiomeType.Temperate;
            }

            // ── Temperate ─────────────────────────────────────────────────────
            if (climateTemp < TemperateMax)
            {
                // Very wet + low → swamp
                if (precipitation > VeryWetMin && elevation < LowlandMax)
                    return BiomeType.Swamp;

                // Wet highlands → temperate forest (handled by tile selection)
                // Dry → still temperate but tile selection gives less forest
                return BiomeType.Temperate;
            }

            // ── Warm / subtropical ────────────────────────────────────────────
            if (climateTemp < WarmMax)
            {
                // Warm + very dry → desert
                if (precipitation < AridMax)
                    return BiomeType.Desert;

                // Warm + very wet + low → swamp
                if (precipitation > VeryWetMin && elevation < LowlandMax)
                    return BiomeType.Swamp;

                // Warm + wet → jungle-like temperate
                if (precipitation > WetMin)
                    return BiomeType.Jungle;

                return BiomeType.Temperate;
            }

            // ── Hot / tropical ────────────────────────────────────────────────
            // Hot + dry → desert
            if (precipitation < AridMax)
                return BiomeType.Desert;

            // Hot + very wet + low → swamp
            if (precipitation > VeryWetMin && elevation < LowlandMax)
                return BiomeType.Swamp;

            // Hot + wet → jungle
            if (precipitation > DryMax)
                return BiomeType.Jungle;

            // Hot + moderate moisture → desert scrub / temperate
            return BiomeType.Desert;
        }
    }
}