using System;
using Microsoft.Xna.Framework;

namespace SimGame.World
{
    /// <summary>
    /// Represents the planetary position of this world map.
    ///
    /// Latitude:  -90 (south pole) to +90 (north pole), 0 = equator
    /// Longitude: -180 to +180, affects noise offset only
    ///
    /// At 0.0078 degrees per tile and 128 tiles tall, the map spans
    /// exactly 1 degree of latitude top to bottom — a very zoomed-in
    /// slice of the planet where climate is essentially uniform and
    /// all variation comes from elevation and local noise.
    /// </summary>
    public class PlanetarySettings
    {
        public float CentreLatitude  { get; }
        public float CentreLongitude { get; }
        public float LatitudeSpan    { get; }
        public float LatitudeMax     { get; }
        public float LatitudeMin     { get; }

        public string LatitudeLabel  => FormatLatitude(CentreLatitude);
        public string LongitudeLabel => FormatLongitude(CentreLongitude);

        public string ClimateZoneLabel => MathF.Abs(CentreLatitude) switch
        {
            <  10f => "Equatorial",
            <  25f => "Tropical",
            <  40f => "Subtropical",
            <  55f => "Temperate",
            <  70f => "Subpolar",
            _      => "Polar"
        };

        // 1 degree / 256 tiles = 0.00390625 degrees per tile
        private const float DegreesPerTile = 1f / 256f;


        public PlanetarySettings(int worldHeight, int seed)
        {
            var rng = new Random(seed + 99999);

            // Random centre latitude, avoid pure poles
            CentreLatitude  = (float)(rng.NextDouble() * 150.0 - 75.0);
            CentreLongitude = (float)(rng.NextDouble() * 360.0 - 180.0);

            LatitudeSpan = worldHeight * DegreesPerTile;
            LatitudeMax  = CentreLatitude + LatitudeSpan * 0.5f;
            LatitudeMin  = CentreLatitude - LatitudeSpan * 0.5f;
        }

        public float GetLatitude(int y, int worldHeight)
        {
            float t = y / (float)(worldHeight - 1);
            return MathHelper.Lerp(LatitudeMax, LatitudeMin, t);
        }

        public static float BaseTemperature(float latitudeDegrees)
        {
            float latRad = latitudeDegrees * MathF.PI / 180f;
            float cosLat = MathF.Cos(latRad);
            return MathF.Pow(MathF.Max(0f, cosLat), 0.7f);
        }

        public static float BasePrecipitation(float latitudeDegrees)
        {
            float absLat = MathF.Abs(latitudeDegrees);

            if (absLat < 10f)
                return MathHelper.Lerp(0.85f, 1.00f, 1f - absLat / 10f);
            if (absLat < 25f)
                return MathHelper.Lerp(0.65f, 0.85f, 1f - (absLat - 10f) / 15f);
            if (absLat < 35f)
                return MathHelper.Lerp(0.15f, 0.65f, 1f - (absLat - 25f) / 10f);
            if (absLat < 50f)
                return MathHelper.Lerp(0.15f, 0.55f, (absLat - 35f) / 15f);
            if (absLat < 65f)
                return MathHelper.Lerp(0.40f, 0.55f, 1f - (absLat - 50f) / 15f);
            if (absLat < 80f)
                return MathHelper.Lerp(0.15f, 0.40f, 1f - (absLat - 65f) / 15f);

            return MathHelper.Lerp(0.05f, 0.15f, 1f - (absLat - 80f) / 10f);
        }

        public static WindDirection PrevailingWind(float latitudeDegrees)
        {
            float absLat = MathF.Abs(latitudeDegrees);
            bool  south  = latitudeDegrees < 0f;

            if (absLat < 30f)
                return south ? WindDirection.NorthWest : WindDirection.SouthWest;
            if (absLat < 60f)
                return south ? WindDirection.NorthEast : WindDirection.SouthWest;

            return south ? WindDirection.NorthEast : WindDirection.SouthEast;
        }

        private static string FormatLatitude(float lat)
        {
            string dir = lat >= 0 ? "N" : "S";
            return $"{MathF.Abs(lat):F4}° {dir}";
        }

        private static string FormatLongitude(float lon)
        {
            string dir = lon >= 0 ? "E" : "W";
            return $"{MathF.Abs(lon):F4}° {dir}";
        }
    }
}