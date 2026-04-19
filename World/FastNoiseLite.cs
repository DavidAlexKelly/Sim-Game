// FastNoiseLite - simplified, verified C# implementation
// Uses Perlin noise as the primary noise type (robust, no index bugs)

using System;
using System.Runtime.CompilerServices;

namespace SimGame.World
{
    public class FastNoiseLite
    {
        public enum NoiseType   { OpenSimplex2, Perlin, Value }
        public enum FractalType { None, FBm, Ridged }

        private int         _seed        = 1337;
        private float       _frequency   = 0.01f;
        private NoiseType   _noiseType   = NoiseType.Perlin;
        private FractalType _fractalType = FractalType.None;
        private int         _octaves     = 3;
        private float       _lacunarity  = 2.0f;
        private float       _gain        = 0.5f;
        private float       _fractalBounding = 1f;

        public FastNoiseLite(int seed = 1337) { _seed = seed; }

        public void SetSeed(int seed)             { _seed = seed; }
        public void SetFrequency(float f)         { _frequency = f; }
        public void SetNoiseType(NoiseType t)     { _noiseType = t; }
        public void SetFractalType(FractalType t) { _fractalType = t; UpdateBounding(); }
        public void SetFractalOctaves(int o)      { _octaves = o;    UpdateBounding(); }
        public void SetFractalLacunarity(float l) { _lacunarity = l; }
        public void SetFractalGain(float g)       { _gain = g;       UpdateBounding(); }

        private void UpdateBounding()
        {
            float amp = 1f, ampFractal = 1f;
            for (int i = 1; i < _octaves; i++) { amp *= _gain; ampFractal += amp; }
            _fractalBounding = 1f / ampFractal;
        }

        public float GetNoise(float x, float y)
        {
            x *= _frequency;
            y *= _frequency;
            return _fractalType switch
            {
                FractalType.FBm    => FractalFBm(x, y),
                FractalType.Ridged => FractalRidged(x, y),
                _                  => Single(_seed, x, y)
            };
        }

        private float FractalFBm(float x, float y)
        {
            float sum = 0, amp = _fractalBounding;
            int seed = _seed;
            for (int i = 0; i < _octaves; i++)
            {
                sum += Single(seed++, x, y) * amp;
                x   *= _lacunarity;
                y   *= _lacunarity;
                amp *= _gain;
            }
            return sum;
        }

        private float FractalRidged(float x, float y)
        {
            float sum = 0, amp = _fractalBounding;
            int seed = _seed;
            for (int i = 0; i < _octaves; i++)
            {
                sum += (1f - Math.Abs(Single(seed++, x, y))) * amp;
                x   *= _lacunarity;
                y   *= _lacunarity;
                amp *= _gain;
            }
            return sum * 2f - 1f;
        }

        private float Single(int seed, float x, float y) => _noiseType switch
        {
            NoiseType.Value => ValueNoise(seed, x, y),
            _               => PerlinNoise(seed, x, y)
        };

        // ── Perlin Noise ──────────────────────────────────────────────────────
        private static float PerlinNoise(int seed, float x, float y)
        {
            int x0 = Floor(x), y0 = Floor(y);
            float dx = x - x0, dy = y - y0;
            float u = Fade(dx), v = Fade(dy);

            float n00 = Grad(Hash(seed, x0,     y0    ), dx,      dy      );
            float n10 = Grad(Hash(seed, x0 + 1, y0    ), dx - 1f, dy      );
            float n01 = Grad(Hash(seed, x0,     y0 + 1), dx,      dy - 1f );
            float n11 = Grad(Hash(seed, x0 + 1, y0 + 1), dx - 1f, dy - 1f );

            return Lerp(Lerp(n00, n10, u), Lerp(n01, n11, u), v) * 1.41421356f;
        }

        // ── Value Noise ───────────────────────────────────────────────────────
        private static float ValueNoise(int seed, float x, float y)
        {
            int x0 = Floor(x), y0 = Floor(y);
            float dx = x - x0, dy = y - y0;
            float u = Fade(dx), v = Fade(dy);

            float n00 = ValCoord(seed, x0,     y0    );
            float n10 = ValCoord(seed, x0 + 1, y0    );
            float n01 = ValCoord(seed, x0,     y0 + 1);
            float n11 = ValCoord(seed, x0 + 1, y0 + 1);

            return Lerp(Lerp(n00, n10, u), Lerp(n01, n11, u), v);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Floor(float f) => f >= 0 ? (int)f : (int)f - 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Lerp(float a, float b, float t) => a + t * (b - a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Hash(int seed, int x, int y)
        {
            int h = seed ^ (x * 1619) ^ (y * 31337);
            h ^= h >> 13;
            h  = h * (h * h * 15731 + 789221) + 1376312589;
            return h;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Grad(int hash, float x, float y)
        {
            switch (hash & 3)
            {
                case 0:  return  x + y;
                case 1:  return -x + y;
                case 2:  return  x - y;
                default: return -x - y;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ValCoord(int seed, int x, int y)
        {
            int h = Hash(seed, x, y);
            return (h & 0x7FFFFFFF) * (1f / 0x7FFFFFFF) * 2f - 1f;
        }
    }
}