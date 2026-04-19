using System;
using SimGame.Core;

namespace SimGame.World
{
    public class TemperatureSystem
    {
        private readonly int     _width;
        private readonly int     _height;
        private readonly float[] _conductionBuffer;
        private int              _lastHour = -1;

        public TemperatureSystem(int width, int height)
        {
            _width            = width;
            _height           = height;
            _conductionBuffer = new float[width * height];
        }

        public void Initialise(Tile[,] tiles, TimeSystem time)
        {
            RebuildBaseTemperatures(tiles, time);
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                    tiles[x, y].Temperature = ComputeFullTemp(
                        ref tiles[x, y], time.LightFactorSmooth);
            _lastHour = time.Hour;
        }

        public void Tick(Tile[,] tiles, TimeSystem time, WindSystem wind)
        {
            if (time.Hour != _lastHour)
            {
                RebuildBaseTemperatures(tiles, time);
                _lastHour = time.Hour;
            }

            float light = time.LightFactorSmooth;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    float temp = ComputeFullTemp(ref tiles[x, y], light);
                    if (tiles[x, y].IsWalkable)
                        temp += wind.GetWindChill(x, y);
                    tiles[x, y].Temperature = temp;
                }
            }

            ApplyConduction(tiles);
        }

        private void RebuildBaseTemperatures(Tile[,] tiles, TimeSystem time)
        {
            float seasonOffset = TileTemperatureProfile.SeasonOffset[time.Season];

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    ref var tile = ref tiles[x, y];

                    float altitudePenalty  = tile.Elevation * -20f;
                    float biomeOffset      = BiomeData.TemperatureOffset(tile.Biome);

                    float b = TileTemperatureProfile.BaseTemp(tile.Type)
                            + seasonOffset
                            + altitudePenalty
                            + biomeOffset;

                    b *= TileTemperatureProfile.MoistureFactor(tile.Type);
                    tile.BaseTemperature = b;
                }
            }
        }

        private static float ComputeFullTemp(ref Tile tile, float lightFactor)
        {
            float amplitude = TileTemperatureProfile.DiurnalAmplitude(tile.Type);
            float diurnal   = (lightFactor * 2f - 1f) * amplitude;
            return tile.BaseTemperature + diurnal;
        }

        private void ApplyConduction(Tile[,] tiles)
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    ref var tile = ref tiles[x, y];
                    float   rate = TileTemperatureProfile.ConductionRate(tile.Type);

                    float neighbourSum   = 0f;
                    int   neighbourCount = 0;

                    Accumulate(tiles, x - 1, y, ref neighbourSum, ref neighbourCount);
                    Accumulate(tiles, x + 1, y, ref neighbourSum, ref neighbourCount);
                    Accumulate(tiles, x, y - 1, ref neighbourSum, ref neighbourCount);
                    Accumulate(tiles, x, y + 1, ref neighbourSum, ref neighbourCount);

                    if (neighbourCount == 0)
                    {
                        _conductionBuffer[x * _height + y] = tile.Temperature;
                        continue;
                    }

                    float avg = neighbourSum / neighbourCount;
                    _conductionBuffer[x * _height + y] =
                        tile.Temperature * (1f - rate) + avg * rate;
                }
            }

            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                    tiles[x, y].Temperature = _conductionBuffer[x * _height + y];
        }

        private void Accumulate(
            Tile[,] tiles, int x, int y,
            ref float sum, ref int count)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height) return;
            sum += tiles[x, y].Temperature;
            count++;
        }
    }
}