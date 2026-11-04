using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Downloads campus places from the free Overpass API (OpenStreetMap). Results are cached on the device,
/// so the public servers are hit once per campus, not on every launch. Please keep it that way: the shared
/// Overpass servers are run by volunteers. If one server is busy or unreachable, the next is tried.
/// </summary>
public class OverpassAPIManager : MonoBehaviour
{
    [SerializeField] string[] endpoints =
    {
        "https://lz4.overpass-api.de/api/interpreter",
        "https://z.overpass-api.de/api/interpreter",
        "https://overpass-api.de/api/interpreter",
    };
    [SerializeField] string userAgent = "XR-Campus-Navigation/0.1 (student project)";
    [SerializeField] int timeoutSeconds = 40;

    public IEnumerator FetchPois(double lat, double lon, float radiusMetres, bool forceRefresh,
                                 Action<List<CampusPoi>> onDone, Action<string> onError)
    {
        string cachePath = Path.Combine(Application.persistentDataPath,
            $"pois_{lat:F4}_{lon:F4}_{(int)radiusMetres}.json");

        if (!forceRefresh && File.Exists(cachePath))
        {
            if (TryParse(File.ReadAllText(cachePath), onDone, "cache")) yield break;
        }

        var form = new WWWForm();
        form.AddField("data", OverpassQueryBuilder.NamedPlacesAround(lat, lon, radiusMetres));

        string lastError = "No Overpass servers configured.";
        foreach (string url in endpoints)
        {
            using (var req = UnityWebRequest.Post(url, form))
            {
                req.timeout = timeoutSeconds;
                req.SetRequestHeader("User-Agent", userAgent);
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string json = req.downloadHandler.text;
                    if (TryParse(json, onDone, "network"))
                    {
                        try { File.WriteAllText(cachePath, json); }
                        catch (Exception e) { Debug.LogWarning("[Overpass] Could not write cache: " + e.Message); }
                        yield break;
                    }
                    lastError = $"{url}: unreadable response";
                }
                else
                {
                    lastError = $"{url}: {req.error} ({req.responseCode})";
                }
            }
            Debug.LogWarning("[Overpass] " + lastError + " - trying next server.");
        }

        // Every server failed: fall back to an old cache if we have one.
        if (File.Exists(cachePath) && TryParse(File.ReadAllText(cachePath), onDone, "stale cache")) yield break;

        Debug.LogWarning("[Overpass] All servers failed. Last error: " + lastError);
        onError?.Invoke("Could not download campus places. " + lastError);
    }

    static bool TryParse(string json, Action<List<CampusPoi>> onDone, string origin)
    {
        try
        {
            var pois = OSMDataParser.ParsePois(json);
            Debug.Log($"[Overpass] {pois.Count} places loaded from {origin}.");
            onDone?.Invoke(pois);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Overpass] Bad data from {origin}: {e.Message}");
            return false;
        }
    }
}
