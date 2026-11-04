using System;
using UnityEngine;

/// <summary>Fake location for Editor testing. Optionally walks in a circle so movement code can be exercised.</summary>
public class MockLocationProvider : MonoBehaviour, ILocationProvider
{
    [SerializeField] double latitude = 12.9916;   // IIT Madras (approximate centre)
    [SerializeField] double longitude = 80.2336;
    [SerializeField] float accuracyMetres = 3f;
    [SerializeField] float headingDeg = 0f;
    [Header("Optional walking")]
    [SerializeField] bool walkInCircle;
    [SerializeField] float radiusMetres = 30f;
    [SerializeField] float speedMetresPerSecond = 1.4f;
    [SerializeField] float updatesPerSecond = 2f;

    public LocationSource Source => LocationSource.Mock;
    public bool IsRunning { get; private set; }
    public bool HasFix { get; private set; }
    public LocationFix Current { get; private set; }
    public event Action<LocationFix> FixUpdated;
    public event Action<string> Failed { add { } remove { } }

    float nextUpdate, angle;

    public void StartProvider() { IsRunning = true; nextUpdate = 0f; }
    public void StopProvider() { IsRunning = false; HasFix = false; }

    void Update()
    {
        if (!IsRunning || Time.time < nextUpdate) return;
        float dt = 1f / Mathf.Max(0.1f, updatesPerSecond);
        nextUpdate = Time.time + dt;

        double lat = latitude, lon = longitude;
        float heading = headingDeg;
        if (walkInCircle)
        {
            angle += speedMetresPerSecond / Mathf.Max(1f, radiusMetres) * dt;
            var offset = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radiusMetres;
            CoordinateConverter.FromEastNorth(offset, latitude, longitude, out lat, out lon);
            heading = Mathf.Repeat(angle * Mathf.Rad2Deg + 90f, 360f); // tangent direction
        }

        Current = new LocationFix(lat, lon, 0, accuracyMetres, heading, Time.timeAsDouble, Source);
        HasFix = true;
        FixUpdated?.Invoke(Current);
    }
}
