using System.Globalization;

/// <summary>Builds Overpass QL queries. Static: no scene object needed.</summary>
public static class OverpassQueryBuilder
{
    /// <summary>
    /// All named features (buildings, amenities, entrances, shops, etc.) within a radius of a point.
    /// "out center" gives one lat/lon per feature, even for building outlines.
    /// </summary>
    public static string NamedPlacesAround(double lat, double lon, float radiusMetres)
    {
        var inv = CultureInfo.InvariantCulture;
        string around = $"(around:{radiusMetres.ToString("0", inv)},{lat.ToString("0.000000", inv)},{lon.ToString("0.000000", inv)})";
        return
            "[out:json][timeout:25];\n" +
            "(\n" +
            $"  nwr[\"name\"][~\"^(building|amenity|tourism|leisure|shop|office|entrance|man_made)$\"~\".\"]{around};\n" +
            ");\n" +
            "out center tags;";
    }
}
