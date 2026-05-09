using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class CampusApiClient : MonoBehaviour
{
    [SerializeField] private string m_BaseUrl = "http://192.168.1.4:8000";

    public string BaseUrl
    {
        get
        {
            // Try to get URL from AppController first
            if (AppController.Instance != null && !string.IsNullOrEmpty(AppController.Instance.BaseUrl))
                return AppController.Instance.BaseUrl.Trim().TrimEnd('/');
            
            return (m_BaseUrl ?? string.Empty).Trim().TrimEnd('/');
        }
    }

    public void SetBaseUrl(string baseUrl)
    {
        m_BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "http://127.0.0.1:8000" : baseUrl.Trim();
    }

    [Serializable]
    public class ChatHistoryMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class LocationsResponseWrapper
    {
        public List<LocationData> locations;
    }

    [Serializable]
    private class ChatRequestPayload
    {
        public List<ChatHistoryMessage> messages;
        public string query;
    }

    [Serializable]
    public class ChatResponsePayload
    {
        public string answer;
        public string destination;
        public float confidence;
        public string source;
    }

    [Serializable]
    private class PathRequestPayload
    {
        public string start_node_id;
        public string destination_node_id;
    }

    [Serializable]
    public class PathPointPayload
    {
        public string id;
        public float x;
        public float y;
        public float z;
        public float rotation_y;
        public string building;
        public int floor;
    }

    [Serializable]
    public class PathResponsePayload
    {
        public List<PathPointPayload> path;
        public List<string> directions;
    }

    [Serializable]
    private class ErrorResponsePayload
    {
        public string detail;
    }

    public IEnumerator FetchLocations(Action<List<LocationData>> onSuccess, Action<string> onError)
    {
        string url = $"{BaseUrl}/locations";
        Debug.Log($"[CampusApiClient] Fetching locations from: {url}");
        
        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 10; // 10 second timeout to prevent app freeze
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            string errorMsg = GetErrorMessage(request, "Could not load campus locations.");
            Debug.LogError($"[CampusApiClient] FetchLocations failed: {errorMsg}");
            onError?.Invoke(errorMsg);
            yield break;
        }

        Debug.Log($"[CampusApiClient] FetchLocations success: {request.downloadHandler.text}");
        LocationsResponseWrapper response = JsonUtility.FromJson<LocationsResponseWrapper>(request.downloadHandler.text);
        onSuccess?.Invoke(response?.locations ?? new List<LocationData>());
    }

    public IEnumerator ResolveDestination(
        string query,
        List<ChatHistoryMessage> history,
        Action<ChatResponsePayload> onSuccess,
        Action<string> onError)
    {
        ChatRequestPayload payload = new ChatRequestPayload
        {
            messages = history ?? new List<ChatHistoryMessage>(),
            query = query
        };

        yield return SendJsonRequest(
            $"{BaseUrl}/chat",
            JsonUtility.ToJson(payload),
            text =>
            {
                ChatResponsePayload response = JsonUtility.FromJson<ChatResponsePayload>(text);
                onSuccess?.Invoke(response);
            },
            onError);
    }

    public IEnumerator RequestPath(
        string startNodeId,
        string destinationNodeId,
        Action<PathResponsePayload> onSuccess,
        Action<string> onError)
    {
        PathRequestPayload payload = new PathRequestPayload
        {
            start_node_id = startNodeId,
            destination_node_id = destinationNodeId
        };

        yield return SendJsonRequest(
            $"{BaseUrl}/get-path",
            JsonUtility.ToJson(payload),
            text =>
            {
                PathResponsePayload response = JsonUtility.FromJson<PathResponsePayload>(text);
                onSuccess?.Invoke(response);
            },
            onError);
    }

    private IEnumerator SendJsonRequest(string url, string json, Action<string> onSuccess, Action<string> onError)
    {
        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 10; // 10 second timeout to prevent app freeze

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(GetErrorMessage(request, "Network request failed."));
            yield break;
        }

        onSuccess?.Invoke(request.downloadHandler.text);
    }

    private string GetErrorMessage(UnityWebRequest request, string fallback)
    {
        string body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
        if (!string.IsNullOrWhiteSpace(body))
        {
            ErrorResponsePayload error = JsonUtility.FromJson<ErrorResponsePayload>(body);
            if (error != null && !string.IsNullOrWhiteSpace(error.detail))
                return error.detail;
            return body;
        }
        if (!string.IsNullOrWhiteSpace(request.error))
            return request.error;
        return fallback;
    }
}
