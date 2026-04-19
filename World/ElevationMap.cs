using System;
using Microsoft.Xna.Framework;

namespace SimGame.World
{
    /// <summary>
    /// Generates terrain elevation only. No biome logic here.
    /// The terrain profile (flat/hilly/mountainous) is seed-driven
    /// but purely a physical property — biomes are determined separately
    /// from climate which is driven by latitude.
    ///
    /// Profiles:
    ///   Flat plains   25% — gentle lowlands
    ///   Mixed         40% — hills and flat areas
    ///   Hilly         25% — pronounced hills
    ///   Mountainous   10% — significant peaks
    /// </summary>
    public class ElevationMap
    {
        private readonly float[,] _elevation;
        private readonly int      _width;
        private readonly int      _height;

        // Exposed so World can show it in the HUD
        public TerrainProfile Profile { get; private set; }

        public int Width  => _width;
        public int Height => _height;

        private const int   ErosionIterations  = 60000;
        private const float ErosionInertia     = 0.05f;
        private const float ErosionCapacity    = 4f;
        private const float ErosionDeposition  = 0.3f;
        private const float ErosionErosion     = 0.3f;
        private const float ErosionEvaporation = 0.02f;
        private const int   ErosionMaxSteps    = 64;
        private const float ErosionMinSlope    = 0.01f;
        private const float ErosionGravity     = 4f;

        public enum TerrainProfile
        {
            FlatPlains,
            Mixed,
            Hilly,
            Mountainous
        }

        public ElevationMap(int width, int height, int seed)
        {
            _width     = width;
            _height    = height;
            _elevation = new float[width, height];

            Generate(seed);
            ApplyHydraulicErosion(seed);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public float Get(int x, int y)
        {
            if (!InBounds(x, y)) return 0f;
            return _elevation[x, y];
        }

        public float GetMetres(int x, int y) => Get(x, y) * 3000f;

        public (int dx, int dy)? GetFlowDirection(int x, int y)
        {
            float lowest     = _elevation[x, y];
            int   bestDx     = 0;
            int   bestDy     = 0;
            bool  foundLower = false;

            Span<(int dx, int dy)> dirs = stackalloc (int, int)[]
            {
                (0, -1), (1, 0), (0, 1), (-1, 0)
            };

            foreach (var (dx, dy) in dirs)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (!InBounds(nx, ny)) continue;
                if (_elevation[nx, ny] < lowest)
                {
                    lowest     = _elevation[nx, ny];
                    bestDx     = dx;
                    bestDy     = dy;
                    foundLower = true;
                }
            }

            return foundLower ? (bestDx, bestDy) : null;
        }

        public bool IsLocalMinimum(int x, int y)
            => GetFlowDirection(x, y) == null;

        public float GetSlope(int x, int y)
        {
            float e     = _elevation[x, y];
            float sum   = 0f;
            int   count = 0;

            Span<(int dx, int dy)> dirs = stackalloc (int, int)[]
            {
                (0, -1), (1, 0), (0, 1), (-1, 0)
            };

            foreach (var (dx, dy) in dirs)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (!InBounds(nx, ny)) continue;
                sum += MathF.Abs(_elevation[nx, ny] - e);
                count++;
            }

            return count == 0 ? 0f : sum / count;
        }

        // ── Generation ────────────────────────────────────────────────────────

        private void Generate(int seed)
        {
            var rng = new Random(seed + 500);

            // ── Terrain profile ───────────────────────────────────────────────
            double roll = rng.NextDouble();
            Profile = roll switch
            {
                < 0.25 => TerrainProfile.FlatPlains,
                < 0.65 => TerrainProfile.Mixed,
                < 0.90 => TerrainProfile.Hilly,
                _      => TerrainProfile.Mountainous
            };

            // Profile drives purely physical elevation properties
            float ridgedBlend;
            float elevationScale;
            float elevationLift;
            float lakeStrength;

            switch (Profile)
            {
                case TerrainProfile.FlatPlains:
                    ridgedBlend    = 0.05f;
                    elevationScale = 0.40f;
                    elevationLift  = 0.30f;
                    lakeStrength   = 0.60f;
                    break;
                case TerrainProfile.Mixed:
                    ridgedBlend    = 0.15f;
                    elevationScale = 0.65f;
                    elevationLift  = 0.22f;
                    lakeStrength   = 0.45f;
                    break;
                case TerrainProfile.Hilly:
                    ridgedBlend    = 0.25f;
                    elevationScale = 0.80f;
                    elevationLift  = 0.15f;
                    lakeStrength   = 0.35f;
                    break;
                default: // Mountainous
                    ridgedBlend    = 0.40f;
                    elevationScale = 1.00f;
                    elevationLift  = 0.10f;
                    lakeStrength   = 0.20f;
                    break;
            }

            // ── Coastal option ────────────────────────────────────────────────
            bool  isCoastal   = rng.NextDouble() < 0.30;
            int   coastalEdge = rng.Next(4);
            float coastDepth  = (float)(rng.NextDouble() * 0.30 + 0.10);

            // ── Noise fields ──────────────────────────────────────────────────
            var primary = new FastNoiseLite(seed + 1000);
            primary.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            primary.SetFrequency(0.030f);
            primary.SetFractalType(FastNoiseLite.FractalType.FBm);
            primary.SetFractalOctaves(7);
            primary.SetFractalGain(0.5f);
            primary.SetFractalLacunarity(2.0f);

            var ridged = new FastNoiseLite(seed + 1500);
            ridged.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            ridged.SetFrequency(0.035f);
            ridged.SetFractalType(FastNoiseLite.FractalType.Ridged);
            ridged.SetFractalOctaves(4);
            ridged.SetFractalGain(0.5f);
            ridged.SetFractalLacunarity(2.2f);

            var macro = new FastNoiseLite(seed + 1800);
            macro.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            macro.SetFrequency(0.008f);
            macro.SetFractalType(FastNoiseLite.FractalType.FBm);
            macro.SetFractalOctaves(2);

            var warpX = new FastNoiseLite(seed + 2000);
            warpX.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            warpX.SetFrequency(0.022f);
            warpX.SetFractalType(FastNoiseLite.FractalType.FBm);
            warpX.SetFractalOctaves(3);

            var warpY = new FastNoiseLite(seed + 3000);
            warpY.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            warpY.SetFrequency(0.022f);
            warpY.SetFractalType(FastNoiseLite.FractalType.FBm);
            warpY.SetFractalOctaves(3);

            var warpX2 = new FastNoiseLite(seed + 4000);
            warpX2.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            warpX2.SetFrequency(0.038f);

            var warpY2 = new FastNoiseLite(seed + 4500);
            warpY2.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            warpY2.SetFrequency(0.038f);

            var lakeNoise = new FastNoiseLite(seed + 6000);
            lakeNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            lakeNoise.SetFrequency(0.015f);
            lakeNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            lakeNoise.SetFractalOctaves(2);

            var lakeWarpX = new FastNoiseLite(seed + 6100);
            lakeWarpX.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            lakeWarpX.SetFrequency(0.020f);

            var lakeWarpY = new FastNoiseLite(seed + 6200);
            lakeWarpY.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            lakeWarpY.SetFrequency(0.020f);

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    // Domain warp
                    float wx1 = warpX.GetNoise(x, y) * 28f;
                    float wy1 = warpY.GetNoise(x, y) * 28f;
                    float wx2 = warpX2.GetNoise(x + wx1, y + wy1) * 12f;
                    float wy2 = warpY2.GetNoise(x + wx1, y + wy1) * 12f;
                    float sx  = x + wx1 + wx2;
                    float sy  = y + wy1 + wy2;

                    // Sample
                    float p = (primary.GetNoise(sx, sy) + 1f) * 0.5f;
                    float r = (ridged.GetNoise(sx, sy)  + 1f) * 0.5f;
                    float m = (macro.GetNoise(x, y)     + 1f) * 0.5f;

                    float smoothBlend = 1f - ridgedBlend;
                    float terrain = p * smoothBlend * 0.75f
                                  + r * ridgedBlend
                                  + m * smoothBlend * 0.25f;

                    // Scale around centre
                    terrain = (terrain - 0.5f) * elevationScale + 0.5f;
                    terrain  = MathHelper.Clamp(terrain + elevationLift, 0f, 1f);

                    // Lakes
                    float lwx     = lakeWarpX.GetNoise(x, y) * 22f;
                    float lwy     = lakeWarpY.GetNoise(x, y) * 22f;
                    float lakeVal = (lakeNoise.GetNoise(x + lwx, y + lwy) + 1f) * 0.5f;

                    float lakeThreshold  = MathHelper.Lerp(0.18f, 0.30f, lakeStrength);
                    float lakeDepression = MathHelper.Clamp(
                        (lakeThreshold - lakeVal) / lakeThreshold, 0f, 1f);
                    lakeDepression = lakeDepression * lakeDepression * lakeDepression;

                    terrain = MathHelper.Lerp(terrain, terrain * 0.20f,
                        lakeDepression * 0.75f);

                    // Coast
                    if (isCoastal)
                    {
                        float cf = GetCoastalFactor(x, y, coastalEdge, coastDepth);
                        terrain  = MathHelper.Lerp(terrain, 0.08f, cf);
                    }

                    _elevation[x, y] = MathHelper.Clamp(terrain, 0f, 1f);
                }
            }
        }

        private float GetCoastalFactor(int x, int y, int edge, float depthFraction)
        {
            float edgeDist = edge switch
            {
                0 => y / (float)_height,
                1 => (_width  - 1 - x) / (float)_width,
                2 => (_height - 1 - y) / (float)_height,
                _ => x / (float)_width
            };

            float f = MathHelper.Clamp(1f - edgeDist / depthFraction, 0f, 1f);
            return f * f * (3f - 2f * f);
        }

        // ── Hydraulic Erosion ─────────────────────────────────────────────────

        private void ApplyHydraulicErosion(int seed)
        {
            var rng = new Random(seed + 8000);

            for (int iter = 0; iter < ErosionIterations; iter++)
            {
                float posX     = (float)(rng.NextDouble() * (_width  - 2) + 1);
                float posY     = (float)(rng.NextDouble() * (_height - 2) + 1);
                float dirX     = 0f;
                float dirY     = 0f;
                float speed    = 1f;
                float water    = 1f;
                float sediment = 0f;

                for (int step = 0; step < ErosionMaxSteps; step++)
                {
                    int nodeX = (int)posX;
                    int nodeY = (int)posY;
                    if (!InBounds(nodeX, nodeY)) break;

                    var (gradX, gradY, height) = CalculateGradient(posX, posY);

                    dirX = dirX * ErosionInertia - gradX * (1f - ErosionInertia);
                    dirY = dirY * ErosionInertia - gradY * (1f - ErosionInertia);

                    float len = MathF.Sqrt(dirX * dirX + dirY * dirY);
                    if (len < 0.0001f) break;
                    dirX /= len;
                    dirY /= len;

                    float newPosX = posX + dirX;
                    float newPosY = posY + dirY;
                    if (!InBounds((int)newPosX, (int)newPosY)) break;

                    var (_, _, newHeight) = CalculateGradient(newPosX, newPosY);
                    float deltaHeight = newHeight - height;

                    float capacity = MathF.Max(
                        -deltaHeight * speed * water * ErosionCapacity,
                        ErosionMinSlope);

                    if (sediment > capacity || deltaHeight > 0f)
                    {
                        float deposit = deltaHeight > 0f
                            ? MathF.Min(deltaHeight, sediment)
                            : (sediment - capacity) * ErosionDeposition;
                        sediment -= deposit;
                        DepositAt(posX, posY, deposit);
                    }
                    else
                    {
                        float erodeAmount = MathF.Min(
                            (capacity - sediment) * ErosionErosion,
                            -deltaHeight);
                        ErodeAt(posX, posY, erodeAmount);
                        sediment += erodeAmount;
                    }

                    speed = MathF.Sqrt(MathF.Max(0f,
                        speed * speed + deltaHeight * ErosionGravity));
                    water *= (1f - ErosionEvaporation);
                    posX   = newPosX;
                    posY   = newPosY;

                    if (water < 0.01f) break;
                }
            }

            NormaliseElevation();
        }

        private (float gradX, float gradY, float height) CalculateGradient(float x, float y)
        {
            int x0 = MathHelper.Clamp((int)x,     0, _width  - 1);
            int y0 = MathHelper.Clamp((int)y,     0, _height - 1);
            int x1 = MathHelper.Clamp((int)x + 1, 0, _width  - 1);
            int y1 = MathHelper.Clamp((int)y + 1, 0, _height - 1);

            float u = x - (int)x;
            float v = y - (int)y;

            float h00 = _elevation[x0, y0];
            float h10 = _elevation[x1, y0];
            float h01 = _elevation[x0, y1];
            float h11 = _elevation[x1, y1];

            float gradX  = (h10 - h00) * (1f - v) + (h11 - h01) * v;
            float gradY  = (h01 - h00) * (1f - u) + (h11 - h10) * u;
            float height = h00 * (1f - u) * (1f - v)
                         + h10 * u        * (1f - v)
                         + h01 * (1f - u) * v
                         + h11 * u        * v;

            return (gradX, gradY, height);
        }

        private void DepositAt(float x, float y, float amount)
        {
            int x0 = MathHelper.Clamp((int)x,     0, _width  - 1);
            int y0 = MathHelper.Clamp((int)y,     0, _height - 1);
            int x1 = MathHelper.Clamp((int)x + 1, 0, _width  - 1);
            int y1 = MathHelper.Clamp((int)y + 1, 0, _height - 1);

            float u = x - (int)x;
            float v = y - (int)y;

            _elevation[x0, y0] += amount * (1f - u) * (1f - v);
            _elevation[x1, y0] += amount * u        * (1f - v);
            _elevation[x0, y1] += amount * (1f - u) * v;
            _elevation[x1, y1] += amount * u        * v;
        }

        private void ErodeAt(float x, float y, float amount)
        {
            int   cx         = (int)x;
            int   cy         = (int)y;
            const int radius = 2;
            float totalWeight = 0f;

            Span<(int ex, int ey, float w)> pts =
                stackalloc (int, int, float)[(radius * 2 + 1) * (radius * 2 + 1)];
            int count = 0;

            for (int ex = cx - radius; ex <= cx + radius; ex++)
            {
                for (int ey = cy - radius; ey <= cy + radius; ey++)
                {
                    if (!InBounds(ex, ey)) continue;
                    float dx   = ex - x;
                    float dy   = ey - y;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                    if (dist >= radius) continue;
                    float w = 1f - dist / radius;
                    pts[count++] = (ex, ey, w);
                    totalWeight += w;
                }
            }

            if (totalWeight <= 0f) return;

            for (int i = 0; i < count; i++)
            {
                var (ex, ey, w) = pts[i];
                _elevation[ex, ey] = MathHelper.Clamp(
                    _elevation[ex, ey] - amount * (w / totalWeight),
                    0f, 1f);
            }
        }

        private void NormaliseElevation()
        {
            float min = float.MaxValue;
            float max = float.MinValue;

            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                {
                    if (_elevation[x, y] < min) min = _elevation[x, y];
                    if (_elevation[x, y] > max) max = _elevation[x, y];
                }

            float range = max - min;
            if (range < 0.0001f) return;

            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                {
                    float n = (_elevation[x, y] - min) / range;
                    _elevation[x, y] = MathHelper.Clamp(n * 0.88f + 0.12f, 0f, 1f);
                }
        }

        private bool InBounds(int x, int y)
            => x >= 0 && y >= 0 && x < _width && y < _height;
    }
}