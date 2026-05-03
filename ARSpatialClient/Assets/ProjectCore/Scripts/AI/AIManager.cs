using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Sends natural language queries to the Python RAG backend,
/// parses the response, and triggers navigation to the resolved location.
/// </summary>
public class AIManager : MonoBehaviour
{
    [Header("Backend")]
    [SerializeField] private string m_BackendUrl = "http://192.168.1.7:8000/ask";
    // ⚠️ Change this IP to your PC's local IP when testing on Android
    // Run 'ipconfig' in cmd to find your IPv4 address

    [System.Serializable]
    private class QueryRequest { public string query; }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Sends a natural language query to the backend.</summary>
    public void Ask(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        StartCoroutine(SendQuery(query.Trim()));
    }

    // ── Private ──────────────────────────────────────────────────────────────
    IEnumerator SendQuery(string query)
    {
        AppController.Instance?.UI?.ShowStatus("Thinking...");

        string json = JsonUtility.ToJson(new QueryRequest { query = query });

        using UnityWebRequest request = new UnityWebRequest(m_BackendUrl, "POST");
        request.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            AIResponse response = JsonUtility.FromJson<AIResponse>(request.downloadHandler.text);
            Debug.Log($"[AIManager] Answer: {response.answer} | Target: {response.target} | Confidence: {response.confidence}");

            AppController.Instance?.UI?.DisplayDirections(new List<string> { response.answer });
            AppController.Instance?.UI?.ShowStatus(string.Empty);

            if (!string.IsNullOrEmpty(response.target))
                NavigateTo(response.target);
        }
        else
        {
            Debug.LogError("[AIManager] Request failed: " + request.error);
            AppController.Instance?.UI?.DisplayDirections(
                new List<string> { "Could not reach server. Is the backend running?" });
            AppController.Instance?.UI?.ShowStatus("Error");
        }
    }

    private void NavigateTo(string locationId)
    {
        var nav         = AppController.Instance?.Navigation;
        var pathfinding = AppController.Instance?.Pathfinding;
        var visualizer  = AppController.Instance?.Visualizer;
        var directions  = AppController.Instance?.Directions;
        var ui          = AppController.Instance?.UI;

        if (nav == null || pathfinding == null || visualizer == null)
        {
            Debug.LogError("[AIManager] One or more required managers are null via AppController.");
            return;
        }

        List<NavigationNode> nodes = nav.GetAllNodes();

        if (nodes.Count < 1)
        {
            Debug.LogWarning("[AIManager] No nodes placed yet.");
            ui?.DisplayDirections(new List<string> { "Tap the floor to place navigation nodes first." });
            return;
        }

        // ── Find start: QR scan location (required) ──────────────────────────
        if (QRLocationManager.Instance == null || !QRLocationManager.Instance.HasLocation)
        {
            ui?.DisplayDirections(new List<string> { "Scan a QR code first to set your location." });
            return;
        }
        NavigationNode start = nav.FindNodeById(QRLocationManager.Instance.CurrentNodeId);
        if (start == null)
        {
            ui?.DisplayDirections(new List<string> { "QR location not found in navigation graph. Please rescan." });
            return;
        }

        // ── Find target by location id ───────────────────────────────────────
        NavigationNode target = nav.FindNodeById(locationId);

        if (target == null)
        {
            Debug.LogWarning($"[AIManager] No node found for location '{locationId}'.");
            ui?.DisplayDirections(new List<string> { $"Location '{locationId}' has no node placed yet." });
            return;
        }

        if (start == target)
        {
            ui?.DisplayDirections(new List<string> { "You are already at the destination." });
            return;
        }

        List<NavigationNode> path = pathfinding.FindPath(start, target);

        if (path != null)
        {
            visualizer.DrawPath(path);

            if (directions != null && ui != null)
                ui.DisplayDirections(directions.GenerateDirections(path));

            Debug.Log($"[AIManager] Path found to '{locationId}': {path.Count} nodes.");
        }
        else
        {
            Debug.LogWarning($"[AIManager] No path found to '{locationId}'.");
            ui?.DisplayDirections(
                new List<string> { $"No path found to {locationId}. Place more nodes to connect the route." });
        }
    }


}
