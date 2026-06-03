using System;
using UnityEngine;

/// <summary>
/// Stores the user's current location after a QR scan.
/// </summary>
public class QRLocationManager : MonoBehaviour
{
    public static QRLocationManager Instance { get; private set; }

    // ── Current Location ──────────────────────────────────────────────────────
    public string CurrentNodeId   { get; private set; } = "";
    public string CurrentBuilding { get; private set; } = "";
    public int    CurrentFloor    { get; private set; } = 0;
    
    // The exact rotation of the AR Camera (Y-axis) at the moment the QR code was scanned.
    // Used by NavigationFlowController to instantly align the AR map.
    public float  ScanCameraRotationY { get; private set; } = 0f;
    public Vector3 ScanCameraPosition { get; private set; } = Vector3.zero;
    
    public bool   HasLocation     => !string.IsNullOrEmpty(CurrentNodeId);

    // ── Event fired when location changes ─────────────────────────────────────
    public event Action<string> OnLocationChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[QRLocationManager] Ready.");
    }

    /// <summary>
    /// Called by QRScanner after a successful scan.
    /// </summary>
    public void SetLocation(string nodeId, string building, int floor)
    {
        CurrentNodeId   = nodeId.ToUpper();
        CurrentBuilding = building;
        CurrentFloor    = floor;

        if (Camera.main != null)
        {
            ScanCameraRotationY = Camera.main.transform.eulerAngles.y;
            ScanCameraPosition = Camera.main.transform.position;
            Debug.Log($"[QRLocation] Camera Y rotation at scan: {ScanCameraRotationY}°");
        }

        Debug.Log($"[QRLocation] Location set → Node: {CurrentNodeId} | Building: {CurrentBuilding} | Floor: {CurrentFloor}");

        OnLocationChanged?.Invoke(CurrentNodeId);
    }

    /// <summary>
    /// Safely updates the user's current floor without resetting the AR world anchor.
    /// Used when transitioning via stairs/lifts natively in AR.
    /// </summary>
    public void UpdateFloor(int floor)
    {
        CurrentFloor = floor;
        Debug.Log($"[QRLocation] Floor updated seamlessly to {CurrentFloor}");
        // We don't invoke OnLocationChanged here because the core AR anchor hasn't changed.
    }

    /// <summary>
    /// Parses QR JSON and sets location.
    /// Expected format: {"building":"Main Block","floor":0,"node_id":"ENTRANCE"}
    /// </summary>
    public bool ParseAndSetFromQR(string qrJson)
    {
        try
        {
            QRPayload payload = JsonUtility.FromJson<QRPayload>(qrJson);

            if (payload == null ||
                string.IsNullOrWhiteSpace(payload.node_id) ||
                string.IsNullOrWhiteSpace(payload.building))
            {
                Debug.LogWarning("[QRLocationManager] Invalid QR payload: " + qrJson);
                return false;
            }

            LocationRegistry registry = AppController.Instance != null
                ? AppController.Instance.Locations
                : FindObjectOfType<LocationRegistry>();

            if (registry == null || !registry.IsLoaded)
            {
                Debug.LogWarning("[QRLocationManager] Location registry is not loaded. Bypassing graph validation.");
                SetLocation(payload.node_id, payload.building, payload.floor);
                return true;
            }

            LocationData location = registry.GetLocation(payload.node_id);
            if (location == null)
            {
                Debug.LogWarning("[QRLocationManager] QR node not found in runtime graph: " + payload.node_id);
                return false;
            }

            if (!location.qr_point)
            {
                Debug.LogWarning("[QRLocationManager] QR node is not marked as a valid QR start point: " + payload.node_id);
                return false;
            }

            if (!string.Equals(location.building, payload.building, StringComparison.OrdinalIgnoreCase) ||
                location.floor != payload.floor)
            {
                Debug.LogWarning("[QRLocationManager] QR payload does not match exported node metadata.");
                return false;
            }

            SetLocation(location.id, location.building, location.floor);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[QRLocationManager] Failed to parse QR: " + e.Message);
            return false;
        }
    }

    public void ClearLocation()
    {
        CurrentNodeId   = "";
        CurrentBuilding = "";
        CurrentFloor    = 0;
        ScanCameraRotationY = 0f;
        ScanCameraPosition = Vector3.zero;
        Debug.Log("[QRLocationManager] Location cleared.");
    }

    // ── Serializable QR payload ───────────────────────────────────────────────
    [Serializable]
    private class QRPayload
    {
        public string building;
        public int    floor;
        public string node_id;
    }
}
