using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Temporary on-screen test panel: shows the current location fix, how many campus places loaded,
/// and lets you search places. Remove once the real UI exists.
/// </summary>
public class LocationDebugPanel : MonoBehaviour
{
    [SerializeField] LocationHub hub;
    [SerializeField] CampusPOIDatabase db;
    [SerializeField] float uiScale = 1.5f;

    string query = "";
    string lastQuery = "";
    string message = "";
    List<CampusPoi> results = new List<CampusPoi>();

    void OnEnable()
    {
        if (hub != null) hub.LocationFailed += OnFail;
        if (db != null) db.LoadFailed += OnFail;
    }

    void OnDisable()
    {
        if (hub != null) hub.LocationFailed -= OnFail;
        if (db != null) db.LoadFailed -= OnFail;
    }

    void OnFail(string m) => message = m;

    void OnGUI()
    {
        GUI.skin.label.richText = true;
        GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));
        GUILayout.BeginArea(new Rect(10, 10, Screen.width / uiScale - 20, Screen.height / uiScale - 20), GUI.skin.box);

        GUILayout.Label("<b>Location</b>");
        if (hub != null && hub.HasFix)
        {
            var f = hub.Current;
            GUILayout.Label($"{f.Latitude:F6}, {f.Longitude:F6}\n" +
                            $"accuracy {f.HorizontalAccuracy:F1} m   heading {(float.IsNaN(f.HeadingDeg) ? "n/a" : f.HeadingDeg.ToString("F0") + "°")}   source {f.Source}");
        }
        else GUILayout.Label("No fix yet");

        GUILayout.Space(6);
        GUILayout.Label("<b>Campus places</b>");
        if (db == null) GUILayout.Label("CampusPOIDatabase not assigned");
        else
        {
            GUILayout.Label(db.IsLoaded ? $"{db.All.Count} places loaded" : "Loading...");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload from OSM")) { StartCoroutine(db.Load(true)); message = "Reloading..."; }
            if (GUILayout.Button("Nearest 5") && hub != null && hub.HasFix)
                results = db.Nearest(hub.Current.Latitude, hub.Current.Longitude, 5);
            GUILayout.EndHorizontal();

            GUILayout.Label("Search:");
            query = GUILayout.TextField(query);
            if (query != lastQuery) { lastQuery = query; results = db.Search(query, 8); }

            foreach (var p in results)
                GUILayout.Label(Describe(p));
        }

        if (!string.IsNullOrEmpty(message)) GUILayout.Label("<color=orange>" + message + "</color>");
        GUILayout.EndArea();
    }

    string Describe(CampusPoi p)
    {
        string s = $"• {p.Name}  ({p.Category})";
        if (hub != null && hub.HasFix)
        {
            double d = CoordinateConverter.DistanceMetres(hub.Current.Latitude, hub.Current.Longitude, p.Latitude, p.Longitude);
            double b = CoordinateConverter.BearingDeg(hub.Current.Latitude, hub.Current.Longitude, p.Latitude, p.Longitude);
            s += $"  {d:F0} m @ {b:F0}°";
        }
        return s;
    }
}
