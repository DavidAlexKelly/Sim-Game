using System;
using Microsoft.Xna.Framework;

namespace SimGame.World
{
    /// <summary>
    /// Generates large coherent biome regions using Voronoi cells.
    ///
    /// Border blending is disabled when either side of a border is Ocean.
    /// Water boundaries are always hard elevation-driven cuts.
    /// </summary>
    public class VoronoiBiomeMap
    {
        private readonly int         _width;
        private readonly int         _height;
        private readonly SeedPoint[] _seeds;

        private const int SeedCount = 48;

        public const float BorderBlendThreshold = 0.25f;

        public struct SeedPoint
        {
            public float     X;
            public float     Y;
            public BiomeType Biome;
            public float     ClimateTemp;
            public float     ClimatePrecip;
        }

        public VoronoiBiomeMap(
            int          width,
            int          height,
            int          seed,
            ElevationMap elevation,
            ClimateMap   climate)
        {
            _width  = width;
            _height = height;
            _seeds  = GenerateSeeds(seed, elevation, climate);
        }

        public BiomeType GetBiome(int x, int y)
        {
            GetNearestSeeds(x, y, out var nearest, out _, out _);
            return nearest.Biome;
        }

        /// <summary>
        /// Returns primary biome, secondary biome, and blend weight.
        /// Blend weight is always 0 when either biome is Ocean —
        /// water edges are hard elevation cuts, never blended.
        /// </summary>
        public (BiomeType primary, BiomeType secondary, float blend)
            GetBiomeBlend(int x, int y)
        {
            GetNearestSeeds(x, y,
                out var nearest,
                out var secondNearest,
                out float blendWeight);

            // Never blend across a water boundary
            if (nearest.Biome      == BiomeType.Ocean ||
                secondNearest.Biome == BiomeType.Ocean)
            {
                blendWeight = 0f;
            }

            // Also never blend if both biomes are the same
            if (nearest.Biome == secondNearest.Biome)
                blendWeight = 0f;

            return (nearest.Biome, secondNearest.Biome, blendWeight);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private SeedPoint[] GenerateSeeds(
            int          seed,
            ElevationMap elevation,
            ClimateMap   climate)
        {
            var rng   = new Random(seed + 11000);
            var seeds = new SeedPoint[SeedCount];

            int gridW = (int)MathF.Ceiling(MathF.Sqrt(SeedCount));
            int gridH = (int)MathF.Ceiling((float)SeedCount / gridW);

            float cellW = _width  / (float)gridW;
            float cellH = _height / (float)gridH;

            int placed = 0;
            for (int gy = 0; gy < gridH && placed < SeedCount; gy++)
            {
                for (int gx = 0; gx < gridW && placed < SeedCount; gx++)
                {
                    float jx = (float)(rng.NextDouble() * 0.8 + 0.1);
                    float jy = (float)(rng.NextDouble() * 0.8 + 0.1);

                    float sx = (gx + jx) * cellW;
                    float sy = (gy + jy) * cellH;

                    int ix = MathHelper.Clamp((int)sx, 0, _width  - 1);
                    int iy = MathHelper.Clamp((int)sy, 0, _height - 1);

                    float elev  = elevation.Get(ix, iy);
                    float temp  = climate.GetTemperature(ix, iy);
                    float prec  = climate.GetPrecipitation(ix, iy);

                    float tempJitter = (float)(rng.NextDouble() - 0.5) * 0.08f;
                    float precJitter = (float)(rng.NextDouble() - 0.5) * 0.08f;

                    float jitteredTemp = MathHelper.Clamp(temp + tempJitter, 0f, 1f);
                    float jitteredPrec = MathHelper.Clamp(prec + precJitter, 0f, 1f);

                    seeds[placed] = new SeedPoint
                    {
                        X             = sx,
                        Y             = sy,
                        Biome         = BiomeClassifier.Classify(
                                            elev, jitteredTemp, jitteredPrec),
                        ClimateTemp   = jitteredTemp,
                        ClimatePrecip = jitteredPrec
                    };

                    placed++;
                }
            }

            return seeds;
        }

        private void GetNearestSeeds(
            int           x,
            int           y,
            out SeedPoint nearest,
            out SeedPoint secondNearest,
            out float     blendWeight)
        {
            float dist1 = float.MaxValue;
            float dist2 = float.MaxValue;
            int   idx1  = 0;
            int   idx2  = 1;

            for (int i = 0; i < _seeds.Length; i++)
            {
                float dx   = _seeds[i].X - x;
                float dy   = _seeds[i].Y - y;
                float dist = dx * dx + dy * dy;

                if (dist < dist1)
                {
                    dist2 = dist1; idx2 = idx1;
                    dist1 = dist;  idx1 = i;
                }
                else if (dist < dist2)
                {
                    dist2 = dist; idx2 = i;
                }
            }

            nearest       = _seeds[idx1];
            secondNearest = _seeds[idx2];

            float d1    = MathF.Sqrt(dist1);
            float d2    = MathF.Sqrt(dist2);
            float ratio = d1 / (d1 + d2);

            float blendStart = 0.5f - BorderBlendThreshold;
            blendWeight = MathHelper.Clamp(
                (ratio - blendStart) / BorderBlendThreshold,
                0f, 1f);

            blendWeight = blendWeight * blendWeight * (3f - 2f * blendWeight);
        }
    }
}