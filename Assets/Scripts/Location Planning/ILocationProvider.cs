using System;

public enum LocationSource { None, Mock, Gps, Geospatial }

/// <summary>A single position reading. Heading is compass degrees clockwise from true north, or NaN if unknown.</summary>
public readonly struct LocationFix
{
    public readonly double Latitude;
    public readonly double Longitude;
    public readonly double Altitude;
    public readonly float HorizontalAccuracy; // metres; smaller is better
    public readonly float HeadingDeg;
    public readonly double Timestamp;         // Unity time (seconds) when received
    public readonly LocationSource Source;

    public LocationFix(double lat, double lon, double alt, float horizontalAccuracy,
                       float headingDeg, double timestamp, LocationSource source)
    {
        Latitude = lat; Longitude = lon; Altitude = alt;
        HorizontalAccuracy = horizontalAccuracy; HeadingDeg = headingDeg;
        Timestamp = timestamp; Source = source;
    }
}

/// <summary>Anything that can report where the user is. GPS, Geospatial (VPS) and mock all implement this.</summary>
public interface ILocationProvider
{
    LocationSource Source { get; }
    bool IsRunning { get; }
    bool HasFix { get; }
    LocationFix Current { get; }
    event Action<LocationFix> FixUpdated;
    event Action<string> Failed;
    void StartProvider();
    void StopProvider();
}
