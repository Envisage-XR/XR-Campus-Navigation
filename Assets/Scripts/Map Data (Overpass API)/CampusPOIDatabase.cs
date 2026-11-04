using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CampusPoi
{
    public string Id;
    public string Name;
    public string Category;   // e.g. "library", "cafe", "university", "entrance"
    public double Latitude;
    public double Longitude;
    public Dictionary<string, string> Tags = new Dictionary<string, string>();
}

/// <summary>Searchable list of campus places, loaded from OpenStreetMap via Overpass. Feeds the search UI.</summary>
public class CampusPOIDatabase : MonoBehaviour
{
    [SerializeField] OverpassAPIManager overpass;
    [Header("Campus area (default: IIT Madras, approximate centre; verify on a map)")]
    [SerializeField] double centerLatitude = 12.9916;
    [SerializeField] double centerLongitude = 80.2336;
    [SerializeField] float radiusMetres = 1800f;
    [SerializeField] bool loadOnStart = true;

    public IReadOnlyList<CampusPoi> All => pois;
    public bool IsLoaded { get; private set; }
    public event Action Loaded;
    public event Action<string> LoadFailed;

    readonly List<CampusPoi> pois = new List<CampusPoi>();

    void Start()
    {
        if (loadOnStart) StartCoroutine(Load(false));
    }

    public IEnumerator Load(bool forceRefresh)
    {
        yield return overpass.FetchPois(centerLatitude, centerLongitude, radiusMetres, forceRefresh,
            list =>
            {
                pois.Clear();
                pois.AddRange(list);
                IsLoaded = true;
                Loaded?.Invoke();
            },
            error => LoadFailed?.Invoke(error));
    }

    /// <summary>Ranked search: names starting with the query first, then word starts, then anywhere, then category/tags.</summary>
    public List<CampusPoi> Search(string query, int max = 10)
    {
        var results = new List<CampusPoi>();
        if (string.IsNullOrWhiteSpace(query)) return results;
        string q = query.Trim().ToLowerInvariant();

        var scored = new List<KeyValuePair<int, CampusPoi>>();
        foreach (var p in pois)
        {
            int score = Score(p, q);
            if (score >= 0) scored.Add(new KeyValuePair<int, CampusPoi>(score, p));
        }
        scored.Sort((a, b) =>
        {
            int c = a.Key.CompareTo(b.Key);
            return c != 0 ? c : string.CompareOrdinal(a.Value.Name, b.Value.Name);
        });
        for (int i = 0; i < scored.Count && i < max; i++) results.Add(scored[i].Value);
        return results;
    }

    /// <summary>The n closest places to a point, nearest first.</summary>
    public List<CampusPoi> Nearest(double lat, double lon, int n = 5, string category = null)
    {
        var withDist = new List<KeyValuePair<double, CampusPoi>>();
        foreach (var p in pois)
        {
            if (category != null && !string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase)) continue;
            withDist.Add(new KeyValuePair<double, CampusPoi>(
                CoordinateConverter.DistanceMetres(lat, lon, p.Latitude, p.Longitude), p));
        }
        withDist.Sort((a, b) => a.Key.CompareTo(b.Key));
        var results = new List<CampusPoi>();
        for (int i = 0; i < withDist.Count && i < n; i++) results.Add(withDist[i].Value);
        return results;
    }

    static int Score(CampusPoi p, string q)
    {
        string name = p.Name.ToLowerInvariant();
        if (name.StartsWith(q, StringComparison.Ordinal)) return 0;
        if (name.Contains(" " + q)) return 1;
        if (name.Contains(q)) return 2;
        if (p.Category != null && p.Category.ToLowerInvariant().Contains(q)) return 3;
        foreach (var v in p.Tags.Values)
            if (v.ToLowerInvariant().Contains(q)) return 4;
        return -1;
    }
}
