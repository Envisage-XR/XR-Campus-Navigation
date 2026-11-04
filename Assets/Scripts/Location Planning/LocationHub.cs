using System;
using UnityEngine;

/// <summary>
/// Single place the rest of the app reads the user's location from. Picks the best available provider:
/// preferred (e.g. Geospatial, when tracking well) > GPS > Mock (Editor).
/// Named LocationHub because UnityEngine.LocationService already exists.
/// </summary>
public class LocationHub : MonoBehaviour
{
    [Tooltip("Use the mock provider in the Editor instead of device GPS.")]
    [SerializeField] bool useMockInEditor = true;
    [SerializeField] GPSManager gps;
    [SerializeField] MockLocationProvider mock;
    [Tooltip("Optional. Any MonoBehaviour implementing ILocationProvider (e.g. the Geospatial provider).")]
    [SerializeField] MonoBehaviour preferredProvider;
    [Tooltip("Preferred-provider fixes worse than this fall back to GPS.")]
    [SerializeField] float preferredMaxAccuracyMetres = 10f;

    public event Action<LocationFix> LocationUpdated;
    public event Action<string> LocationFailed;

    public bool HasFix { get; private set; }
    public LocationFix Current { get; private set; }

    ILocationProvider preferred, baseline;

    void Awake()
    {
        preferred = preferredProvider as ILocationProvider;
        if (preferredProvider != null && preferred == null)
            Debug.LogError("[LocationHub] preferredProvider does not implement ILocationProvider.");

#if UNITY_EDITOR
        baseline = useMockInEditor && mock != null ? mock : (ILocationProvider)gps;
#else
        baseline = gps;
#endif
    }

    void OnEnable()
    {
        Hook(baseline); Hook(preferred);
        baseline?.StartProvider();
        preferred?.StartProvider();
    }

    void OnDisable()
    {
        baseline?.StopProvider(); preferred?.StopProvider();
        Unhook(baseline); Unhook(preferred);
    }

    void Hook(ILocationProvider p)   { if (p != null) { p.FixUpdated += OnFix; p.Failed += OnFail; } }
    void Unhook(ILocationProvider p) { if (p != null) { p.FixUpdated -= OnFix; p.Failed -= OnFail; } }

    void OnFix(LocationFix fix)
    {
        // Ignore baseline fixes while the preferred provider has a recent, good one.
        if (preferred != null && preferred.HasFix && fix.Source != preferred.Source &&
            preferred.Current.HorizontalAccuracy <= preferredMaxAccuracyMetres &&
            Time.timeAsDouble - preferred.Current.Timestamp < 3.0) return;

        Current = fix;
        HasFix = true;
        LocationUpdated?.Invoke(fix);
    }

    void OnFail(string message) => LocationFailed?.Invoke(message);
}
