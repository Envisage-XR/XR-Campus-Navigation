using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>Device GPS via Unity's Input.location, with permission handling, accuracy filtering and smoothing.</summary>
public class GPSManager : MonoBehaviour, ILocationProvider
{
    [Header("Request")]
    [SerializeField] float desiredAccuracyMetres = 5f;
    [SerializeField] float updateDistanceMetres = 1f;
    [SerializeField] float startTimeoutSeconds = 20f;

    [Header("Filtering")]
    [Tooltip("Readings worse than this are ignored.")]
    [SerializeField] float maxAcceptedAccuracyMetres = 30f;
    [Tooltip("0 = no smoothing, closer to 1 = heavier smoothing.")]
    [Range(0f, 0.95f)] [SerializeField] float smoothing = 0.6f;

    [SerializeField] CompassManager compass; // optional; supplies heading

    public LocationSource Source => LocationSource.Gps;
    public bool IsRunning { get; private set; }
    public bool HasFix { get; private set; }
    public LocationFix Current { get; private set; }
    public event Action<LocationFix> FixUpdated;
    public event Action<string> Failed;

    double lastDeviceTimestamp = -1;
    double smLat, smLon;
    Coroutine startRoutine;

    public void StartProvider()
    {
        if (IsRunning || startRoutine != null) return;
        startRoutine = StartCoroutine(StartRoutine());
    }

    public void StopProvider()
    {
        if (startRoutine != null) { StopCoroutine(startRoutine); startRoutine = null; }
        if (Input.location.status != LocationServiceStatus.Stopped) Input.location.Stop();
        IsRunning = false;
        HasFix = false;
        lastDeviceTimestamp = -1;
    }

    IEnumerator StartRoutine()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            float wait = 0f;
            while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation) && wait < 15f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Fail("Location permission denied.");
                yield break;
            }
        }
#endif
        if (!Input.location.isEnabledByUser)
        {
            Fail("Location services are turned off on this device.");
            yield break;
        }

        Input.location.Start(desiredAccuracyMetres, updateDistanceMetres);

        float t = 0f;
        while (Input.location.status == LocationServiceStatus.Initializing && t < startTimeoutSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            Fail(t >= startTimeoutSeconds ? "Timed out waiting for GPS." : "Could not start GPS.");
            yield break;
        }

        startRoutine = null;
        IsRunning = true;
    }

    void Update()
    {
        if (!IsRunning || Input.location.status != LocationServiceStatus.Running) return;

        var d = Input.location.lastData;
        if (d.timestamp == lastDeviceTimestamp) return;   // nothing new
        lastDeviceTimestamp = d.timestamp;

        if (d.horizontalAccuracy > maxAcceptedAccuracyMetres) return;

        if (!HasFix)
        {
            smLat = d.latitude; smLon = d.longitude;
        }
        else
        {
            double a = Mathf.Clamp01(smoothing);
            smLat = smLat * a + d.latitude * (1 - a);
            smLon = smLon * a + d.longitude * (1 - a);
        }

        float heading = compass != null && compass.HasHeading ? compass.HeadingDeg : float.NaN;
        Current = new LocationFix(smLat, smLon, d.altitude, d.horizontalAccuracy, heading, Time.timeAsDouble, Source);
        HasFix = true;
        FixUpdated?.Invoke(Current);
    }

    void OnDisable() => StopProvider();

    void Fail(string message)
    {
        startRoutine = null;
        IsRunning = false;
        Debug.LogWarning($"[GPSManager] {message}");
        Failed?.Invoke(message);
    }
}
