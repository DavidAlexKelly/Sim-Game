using System;
using Microsoft.Xna.Framework;

namespace SimGame.World
{
    /// <summary>
    /// Generates climate temperature and precipitation per tile.
    ///
    /// At our scale (1 degree of latitude) the planetary baseline is
    /// essentially constant across the map. All meaningful variation comes
    /// from elevation (lapse rate) and small local noise.
    ///
    /// This means:
    ///   - A tropical seed → whole map is hot/wet, jungle in valleys,
    ///     alpine on peaks
    ///   - A temperate seed → whole map is moderate, forest/grass in
    ///     valleys, stone/snowcap on peaks
    ///   - A desert seed → whole map is hot/dry, desert in valleys,
    ///     rocky highlands, alpine peaks
    ///   - A polar seed → whole map is cold, tundra in valleys,
    ///     snowcap on peaks
    /// </summary>
    public class ClimateMap
    {
        private readonly float[,] _temperature;
        private readonly float[,] _precipitation;

        private readonly int _width;
        private readonly int _height;

        // Lapse rate: how much climate temperature drops per unit elevation
        // above sea level. Stronger than before so peaks are genuinely cold.
        // At 0.65 a tile at max elevation (1.0) loses 0.65 * (1-0.35)/0.65 = 0.65
        // of its base temperature — enough to push tropical peaks into alpine.
        private const float LapseRate = 0.65f;

        // Local noise is intentionally weak — at 1-degree scale the climate
        // is nearly uniform. Noise just adds texture, not biome-changing variation.
        private const float LocalNoiseWeight = 0.12f;

        // Orographic lift — slopes get slightly more precipitation
        private const float OrographicLift = 0.15f;

        public ClimateMap(
            int               width,
            int               height,
            int               seed,
            ElevationMap      elevation,
            PlanetarySettings planet)
        {
            _width         = width;
            _height        = height;
            _temperature   = new float[width, height];
            _precipitation = new float[width, height];

            Generate(seed, elevation, planet);
        }

        public float GetTemperature(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height) return 0.5f;
            return _temperature[x, y];
        }

        public float GetPrecipitation(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height) return 0.5f;
            return _precipitation[x, y];
        }

        private void Generate(
            int               seed,
            ElevationMap      elevation,
            PlanetarySettings planet)
        {
            // Tiny local variation noise — domain warped for organic feel
            var localTempNoise = new FastNoiseLite(seed + 5000);
            localTempNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            localTempNoise.SetFrequency(0.008f);
            localTempNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            localTempNoise.SetFractalOctaves(2);

            var localPrecipNoise = new FastNoiseLite(seed + 6000);
            localPrecipNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            localPrecipNoise.SetFrequency(0.010f);
            localPrecipNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            localPrecipNoise.SetFractalOctaves(2);

            // Domain warp for local noise only
            var climWarpX = new FastNoiseLite(seed + 8000);
            climWarpX.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            climWarpX.SetFrequency(0.015f);

            var climWarpY = new FastNoiseLite(seed + 9000);
            climWarpY.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            climWarpY.SetFrequency(0.015f);

            float lonOffset = planet.CentreLongitude * 0.3f;

            // The planetary baseline for this map — essentially constant
            // since we span only 1 degree of latitude
            float centreTemp  = PlanetarySettings.BaseTemperature(planet.CentreLatitude);
            float centrePrec  = PlanetarySettings.BasePrecipitation(planet.CentreLatitude);

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    float elev = elevation.Get(x, y);

                    // Tiny latitude variation across the map
                    // At 1 degree span this is negligible but physically correct
                    float lat        = planet.GetLatitude(y, _height);
                    float planetTemp = PlanetarySettings.BaseTemperature(lat);
                    float planetPrec = PlanetarySettings.BasePrecipitation(lat);

                    // Domain warp local noise
                    float wx = climWarpX.GetNoise(x + lonOffset, y) * 20f;
                    float wy = climWarpY.GetNoise(x + lonOffset, y) * 20f;

                    float localTemp  = (localTempNoise.GetNoise(x + lonOffset + wx, y + wy)  + 1f) * 0.5f;
                    float localPrec  = (localPrecipNoise.GetNoise(x + lonOffset + wx, y + wy) + 1f) * 0.5f;

                    // Blend: planetary is dominant, local adds texture only
                    float blendedTemp = MathHelper.Lerp(planetTemp, localTemp, LocalNoiseWeight);
                    float blendedPrec = MathHelper.Lerp(planetPrec, localPrec, LocalNoiseWeight);

                    // ── Elevation lapse rate ───────────────────────────────────
                    // This is the key driver of within-map biome variation.
                    // A tropical map at sea level is jungle; the same map's
                    // peaks are alpine because temperature drops with elevation.
                    float elevAboveSea   = MathHelper.Max(0f, elev - 0.35f) / 0.65f;
                    float lapseReduction = elevAboveSea * LapseRate;

                    _temperature[x, y] = MathHelper.Clamp(
                        blendedTemp - lapseReduction, 0f, 1f);

                    // Orographic lift — slopes get more rain
                    float slope      = elevation.GetSlope(x, y);
                    float orographic = slope * OrographicLift;

                    // High peaks are drier (rain shadow above cloud level)
                    float highElevDry = MathHelper.Max(0f, elevAboveSea - 0.70f) * 0.30f;

                    _precipitation[x, y] = MathHelper.Clamp(
                        blendedPrec + orographic - highElevDry, 0f, 1f);
                }
            }
        }
    }
}