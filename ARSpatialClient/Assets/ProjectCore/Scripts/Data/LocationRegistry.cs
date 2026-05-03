using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads nodes.json from Resources and provides location lookup.
/// Runs at -90 so it is ready before NavigationManager.Start (default order 0).
/// </summary>
[DefaultExecutionOrder(-90)]
public class LocationRegistry : MonoBehaviour
{
    private readonly Dictionary<string, LocationData> m_Map = new Dictionary<string, LocationData>();
    public bool IsLoaded { get; private set; }
    public int Count => m_Map.Count;

    public LocationData GetLocation(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        m_Map.TryGetValue(id.ToUpper(), out LocationData data);
        return data;
    }

    public bool HasLocation(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return m_Map.ContainsKey(id.ToUpper());
    }

    public IEnumerable<string>       GetAllIds()       => m_Map.Keys;
    public IEnumerable<LocationData> GetAllLocations() => m_Map.Values;

    public void SetLocations(IEnumerable<LocationData> locations)
    {
        m_Map.Clear();
        IsLoaded = false;

        if (locations == null)
            return;

        foreach (LocationData loc in locations)
        {
            if (string.IsNullOrEmpty(loc.id)) continue;

            string key = loc.id.ToUpper();

            if (m_Map.ContainsKey(key))
            {
                Debug.LogWarning($"[LocationRegistry] Duplicate id '{key}' skipped.");
                continue;
            }

            m_Map[key] = loc;
        }

        IsLoaded = true;
        Debug.Log($"[LocationRegistry] Loaded {m_Map.Count} nodes.");
    }

    public void Clear()
    {
        m_Map.Clear();
        IsLoaded = false;
    }
}
