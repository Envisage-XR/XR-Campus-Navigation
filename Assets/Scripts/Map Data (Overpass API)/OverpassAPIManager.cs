using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Downloads campus places from the free Overpass API (OpenStreetMap). Results are cached on the device,
/// so the public server is hit once per campus, not on every launch. Please keep it that way: the shared
/// Overpass servers are run by volunteers.
/// </summary>
public class OverpassAPIManager : MonoBehaviour
{
    [SerializeField] string endpoint = "https://overpass-api.de/api/interpreter";
    [SerializeField] string userAgent = "XR-Campus-Navigation/0.1 (student project)";
    [SerializeField] int timeoutSeconds = 40;

    public IEnumerator FetchPois(double lat, double lon, float radiusMetres, bool forceRefresh,
                                 Action<List<CampusPoi>> onDone, Action<string> onError)
    {
        string cachePath = Path.Combine(Application.persistentDataPath,
            $"pois_{lat:F4}_{lon:F4}_{(int)radiusMetres}.json");

        if (!forceRefresh && File.Exists(cachePath))
        {
            if (TryParse(File.ReadAllText(cachePath), onDone, onError, "cache")) yield break;
        }

        var form = new WWWForm();
        form.AddField("data", OverpassQueryBuilder.NamedPlacesAround(lat, lon, radiusMetres));

        using (var req = UnityWebRequest.Post(endpoint, form))
        {
            req.timeout = timeoutSeconds;
            req.SetRequestHeader("User-Agent", userAgent);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                // Fall back to a stale cache if the network is down.
                if (File.Exists(cachePath) && TryParse(File.ReadAllText(cachePath), onDone, onError, "stale cache")) yield break;
                Fail(onError, $"Overpass request failed: {req.error} ({req.responseCode})");
                yield break;
            }

            string json = req.downloadHandler.text;
            if (TryParse(json, onDone, onError, "network"))
            {
                try { File.WriteAllText(cachePath, json); }
                catch (Exception e) { Debug.LogWarning("[Overpass] Could not write cache: " + e.Message); }
            }
        }
    }

    static bool TryParse(string json, Action<List<CampusPoi>> onDone, Action<string> onError, string origin)
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
            if (origin == "network") onError?.Invoke("Could not read Overpass response: " + e.Message);
            return false;
        }
    }

    static void Fail(Action<string> onError, string message)
    {
        Debug.LogWarning("[Overpass] " + message);
        onError?.Invoke(message);
    }
}
