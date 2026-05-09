using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// LEGACY: This script is from the old system and is NOT used in production runtime.
/// It's kept for reference but disabled by CampusRuntimeInstaller.
/// The new system uses ChatManager.cs instead.
/// </summary>
public class AIManager : MonoBehaviour
{
    [Header("Backend")]
    [SerializeField] private string m_BackendUrl = "http://192.168.1.7:8000/ask";

    [System.Serializable]
    private class QueryRequest { public string query; }

    public void Ask(string query)
    {
        Debug.LogWarning("[AIManager] LEGACY: This script is disabled. Use ChatManager instead.");
    }

    IEnumerator SendQuery(string query)
    {
        Debug.LogWarning("[AIManager] LEGACY: This script is disabled. Use ChatManager instead.");
        yield break;
    }

    private void NavigateTo(string locationId)
    {
        Debug.LogWarning("[AIManager] LEGACY: This script is disabled. Use NavigationFlowController instead.");
    }
}
