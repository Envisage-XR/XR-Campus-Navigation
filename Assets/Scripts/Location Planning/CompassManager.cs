using UnityEngine;

/// <summary>Smoothed compass heading (degrees clockwise from true north).</summary>
public class CompassManager : MonoBehaviour
{
    [Tooltip("Higher = snappier, lower = smoother.")]
    [SerializeField] float responsiveness = 8f;

    public bool HasHeading { get; private set; }
    public float HeadingDeg { get; private set; }
    /// <summary>Reported accuracy in degrees, or -1 if the device does not say.</summary>
    public float AccuracyDeg => Input.compass.headingAccuracy;

    void OnEnable()
    {
        Input.compass.enabled = true;
    }

    void Update()
    {
        if (!Input.compass.enabled) return;

        // trueHeading needs location services running (GPSManager starts them); fall back to magnetic until then.
        float raw = Input.compass.trueHeading > 0f ? Input.compass.trueHeading : Input.compass.magneticHeading;

        if (!HasHeading) { HeadingDeg = raw; HasHeading = true; return; }

        float k = 1f - Mathf.Exp(-responsiveness * Time.unscaledDeltaTime);
        HeadingDeg = Mathf.Repeat(Mathf.LerpAngle(HeadingDeg, raw, k), 360f);
    }
}
