using System;
using UnityEngine;

/// <summary>
/// Lat/lon helpers. Uses a local equirectangular projection around an origin, which is accurate to
/// centimetres over a campus-sized area (a few km). X = east, Y = north, in metres.
/// </summary>
public static class CoordinateConverter
{
    const double EarthRadius = 6378137.0;
    const double Deg2Rad = Math.PI / 180.0;
    const double Rad2Deg = 180.0 / Math.PI;

    public static Vector2 ToEastNorth(double lat, double lon, double originLat, double originLon)
    {
        double east = (lon - originLon) * Deg2Rad * EarthRadius * Math.Cos(originLat * Deg2Rad);
        double north = (lat - originLat) * Deg2Rad * EarthRadius;
        return new Vector2((float)east, (float)north);
    }

    public static void FromEastNorth(Vector2 eastNorth, double originLat, double originLon,
                                     out double lat, out double lon)
    {
        lat = originLat + (eastNorth.y / EarthRadius) * Rad2Deg;
        lon = originLon + (eastNorth.x / (EarthRadius * Math.Cos(originLat * Deg2Rad))) * Rad2Deg;
    }

    /// <summary>Unity world position (x = east, z = north) relative to an origin, at the given height.</summary>
    public static Vector3 ToUnity(double lat, double lon, double originLat, double originLon, float y = 0f)
    {
        var en = ToEastNorth(lat, lon, originLat, originLon);
        return new Vector3(en.x, y, en.y);
    }

    /// <summary>Great-circle distance in metres.</summary>
    public static double DistanceMetres(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = (lat2 - lat1) * Deg2Rad, dLon = (lon2 - lon1) * Deg2Rad;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Deg2Rad) * Math.Cos(lat2 * Deg2Rad) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * EarthRadius * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    /// <summary>Initial bearing from point 1 to point 2, degrees clockwise from north (0-360).</summary>
    public static double BearingDeg(double lat1, double lon1, double lat2, double lon2)
    {
        double p1 = lat1 * Deg2Rad, p2 = lat2 * Deg2Rad, dl = (lon2 - lon1) * Deg2Rad;
        double y = Math.Sin(dl) * Math.Cos(p2);
        double x = Math.Cos(p1) * Math.Sin(p2) - Math.Sin(p1) * Math.Cos(p2) * Math.Cos(dl);
        return (Math.Atan2(y, x) * Rad2Deg + 360.0) % 360.0;
    }
}
