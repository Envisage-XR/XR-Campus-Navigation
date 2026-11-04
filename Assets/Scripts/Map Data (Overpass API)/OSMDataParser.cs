using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// Turns an Overpass JSON response into CampusPoi objects. Uses a small built-in JSON reader because Unity's
/// JsonUtility cannot read OSM's free-form "tags" dictionary.
/// </summary>
public static class OSMDataParser
{
    static readonly string[] CategoryKeys = { "amenity", "tourism", "shop", "leisure", "office", "entrance", "man_made", "building", "highway" };

    public static List<CampusPoi> ParsePois(string json)
    {
        var root = MiniJson.Parse(json) as Dictionary<string, object>
                   ?? throw new FormatException("Top level is not an object.");
        var result = new List<CampusPoi>();
        if (!root.TryGetValue("elements", out var elementsObj) || !(elementsObj is List<object> elements)) return result;

        foreach (var e in elements)
        {
            if (!(e is Dictionary<string, object> el)) continue;
            if (!(el.TryGetValue("tags", out var tagsObj) && tagsObj is Dictionary<string, object> rawTags)) continue;

            var tags = new Dictionary<string, string>();
            foreach (var kv in rawTags) tags[kv.Key] = kv.Value as string ?? Convert.ToString(kv.Value, CultureInfo.InvariantCulture);

            if (!tags.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name)) continue;

            double lat, lon;
            if (!TryNumber(el, "lat", out lat) || !TryNumber(el, "lon", out lon))
            {
                if (!(el.TryGetValue("center", out var c) && c is Dictionary<string, object> center &&
                      TryNumber(center, "lat", out lat) && TryNumber(center, "lon", out lon))) continue;
            }

            string type = el.TryGetValue("type", out var t) ? t as string : "node";
            string id = el.TryGetValue("id", out var i) ? Convert.ToString(i, CultureInfo.InvariantCulture) : name;

            result.Add(new CampusPoi
            {
                Id = type + "/" + id,
                Name = name.Trim(),
                Category = CategoryOf(tags),
                Latitude = lat,
                Longitude = lon,
                Tags = tags
            });
        }
        return result;
    }

    static string CategoryOf(Dictionary<string, string> tags)
    {
        foreach (var key in CategoryKeys)
            if (tags.TryGetValue(key, out var v) && v != "yes") return v;
        foreach (var key in CategoryKeys)
            if (tags.ContainsKey(key)) return key;
        return "place";
    }

    static bool TryNumber(Dictionary<string, object> d, string key, out double value)
    {
        value = 0;
        if (d.TryGetValue(key, out var o) && o is double dbl) { value = dbl; return true; }
        return false;
    }

    // ---------------- minimal JSON reader ----------------
    static class MiniJson
    {
        public static object Parse(string s)
        {
            int i = 0;
            var v = Value(s, ref i);
            return v;
        }

        static void Skip(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static object Value(string s, ref int i)
        {
            Skip(s, ref i);
            if (i >= s.Length) throw new FormatException("Unexpected end of JSON.");
            char c = s[i];
            if (c == '{') return Obj(s, ref i);
            if (c == '[') return Arr(s, ref i);
            if (c == '"') return Str(s, ref i);
            if (c == 't') { Expect(s, ref i, "true"); return true; }
            if (c == 'f') { Expect(s, ref i, "false"); return false; }
            if (c == 'n') { Expect(s, ref i, "null"); return null; }
            return Num(s, ref i);
        }

        static void Expect(string s, ref int i, string word)
        {
            if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) throw new FormatException("Bad literal at " + i);
            i += word.Length;
        }

        static Dictionary<string, object> Obj(string s, ref int i)
        {
            var d = new Dictionary<string, object>();
            i++; Skip(s, ref i);
            if (s[i] == '}') { i++; return d; }
            while (true)
            {
                Skip(s, ref i);
                string key = Str(s, ref i);
                Skip(s, ref i);
                if (s[i++] != ':') throw new FormatException("Expected ':' at " + (i - 1));
                d[key] = Value(s, ref i);
                Skip(s, ref i);
                char c = s[i++];
                if (c == '}') return d;
                if (c != ',') throw new FormatException("Expected ',' or '}' at " + (i - 1));
            }
        }

        static List<object> Arr(string s, ref int i)
        {
            var l = new List<object>();
            i++; Skip(s, ref i);
            if (s[i] == ']') { i++; return l; }
            while (true)
            {
                l.Add(Value(s, ref i));
                Skip(s, ref i);
                char c = s[i++];
                if (c == ']') return l;
                if (c != ',') throw new FormatException("Expected ',' or ']' at " + (i - 1));
            }
        }

        static string Str(string s, ref int i)
        {
            if (s[i] != '"') throw new FormatException("Expected string at " + i);
            i++;
            var sb = new StringBuilder();
            while (true)
            {
                if (i >= s.Length) throw new FormatException("Unterminated string.");
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                        i += 4;
                        break;
                    default: throw new FormatException("Bad escape \\" + e);
                }
            }
        }

        static double Num(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (start == i) throw new FormatException("Unexpected character '" + s[i] + "' at " + i);
            return double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
        }
    }
}
