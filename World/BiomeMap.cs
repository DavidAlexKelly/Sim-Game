namespace SimGame.World
{
    /// <summary>
    /// Builds the per-tile biome assignment using Voronoi regions
    /// classified by physical properties from ClimateMap and ElevationMap.
    ///
    /// Voronoi cells produce large coherent biome regions with organic
    /// borders rather than noisy per-tile classification.
    /// Border blending data is exposed for WorldGen to use when
    /// selecting tile types near biome boundaries.
    /// </summary>
    public class BiomeMap
    {
        private readonly int              _width;
        private readonly int              _height;
        private readonly VoronoiBiomeMap  _voronoi;

        public ClimateMap Climate { get; }

        public BiomeMap(
            int               width,
            int               height,
            int               seed,
            ElevationMap      elevation,
            PlanetarySettings planet)
        {
            _width  = width;
            _height = height;

            Climate  = new ClimateMap(width, height, seed, elevation, planet);
            _voronoi = new VoronoiBiomeMap(width, height, seed, elevation, Climate);
        }

        public BiomeType Get(int x, int y)
            => _voronoi.GetBiome(x, y);

        /// <summary>
        /// Returns primary biome, secondary biome, and blend weight (0-1).
        /// Used by WorldGen to smoothly transition tile types at borders.
        /// </summary>
        public (BiomeType primary, BiomeType secondary, float blend)
            GetBlend(int x, int y)
            => _voronoi.GetBiomeBlend(x, y);
    }
}