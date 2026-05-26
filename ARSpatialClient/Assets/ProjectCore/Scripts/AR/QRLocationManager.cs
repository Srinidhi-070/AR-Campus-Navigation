using System;
using UnityEngine;

/// <summary>
/// Stores the user's current location after a QR scan.
/// Acts as the single source of truth for "where am I right now".
/// 
/// Flow:
///   QRScanner scans → calls QRLocationManager.SetLocation()
///   → BeginCalibration() starts walk tracking
///   → User walks 2-3 steps → ARCore VIO tracks displacement
///   → Walk direction is matched against graph edges from the start node
///   → CalibratedYawOffset is computed (AR space → Map space rotation)
///   → OnCalibrationComplete fires
///   → NavigationFlowController uses CalibratedYawOffset for path alignment
/// </summary>
public class QRLocationManager : MonoBehaviour
{
    public static QRLocationManager Instance { get; private set; }

    // ── Calibration ───────────────────────────────────────────────────────────
    public enum CalibrationState { NotCalibrated, WaitingForWalk, Calibrated }

    public CalibrationState CurrentCalibrationState { get; private set; } = CalibrationState.NotCalibrated;
    public float CalibratedYawOffset { get; private set; } = 0f;

    /// <summary>How far the user has walked since calibration started (XZ plane).</summary>
    public float CalibrationWalkDistance { get; private set; } = 0f;

    /// <summary>Minimum walk distance required to calibrate (meters).</summary>
    public float RequiredWalkDistance => CALIBRATION_WALK_THRESHOLD;

    /// <summary>Fires when walk-to-calibrate completes successfully.</summary>
    public event Action OnCalibrationComplete;

    private const float CALIBRATION_WALK_THRESHOLD = 1.0f; // meters — needs a genuine walk
    private const float CALIBRATION_MIN_TIME = 1.5f;       // seconds — let AR fully stabilize after scanner closes
    private const float CALIBRATION_MIN_SPEED = 0.3f;      // m/s — reject jitter / standing still
    private const float CALIBRATION_MAX_JUMP = 1.0f;       // meters/frame — reject AR relocalization jumps
    private Vector3 m_CalibrationStartPos;
    private Vector3 m_CalibrationPrevPos;
    private float m_CalibrationStartTime;
    private float m_CalibrationAccumulatedDist;             // incremental step distance (not straight-line)

    // ── Current Location ──────────────────────────────────────────────────────
    public string CurrentNodeId   { get; private set; } = "";
    public string CurrentBuilding { get; private set; } = "";
    public int    CurrentFloor    { get; private set; } = 0;
    public float  ScanCameraRotationY { get; private set; } = 0f;
    public float  ScanCompassHeading  { get; private set; } = 0f;
    public bool   HasLocation     => !string.IsNullOrEmpty(CurrentNodeId);

    // ── Event fired when location changes ─────────────────────────────────────
    public event Action<string> OnLocationChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[QRLocationManager] Ready.");
    }

    // ── Public API ────────────────────────────────────────────────────────────

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
            ScanCompassHeading  = Input.compass.trueHeading;
            Debug.Log($"[QRLocation] Camera Y rotation at scan: {ScanCameraRotationY}° | Compass: {ScanCompassHeading}°");
        }

        Debug.Log($"[QRLocation] Location set → Node: {CurrentNodeId} | Building: {CurrentBuilding} | Floor: {CurrentFloor}");

        OnLocationChanged?.Invoke(CurrentNodeId);
    }

    /// <summary>
    /// Starts walk-to-calibrate. Called after a successful QR scan.
    /// Records the camera position and waits for the user to walk ~2m.
    /// </summary>
    public void BeginCalibration()
    {
        if (!HasLocation)
        {
            Debug.LogWarning("[QRLocationManager] Cannot calibrate without a location.");
            return;
        }

        Transform cam = Camera.main != null ? Camera.main.transform : null;
        if (cam == null)
        {
            Debug.LogWarning("[QRLocationManager] No camera for calibration. Auto-calibrating with compass fallback.");
            ForceCalibrationFromCompass();
            return;
        }

        m_CalibrationStartPos = cam.position;
        m_CalibrationPrevPos = cam.position;
        m_CalibrationStartTime = Time.time;
        m_CalibrationAccumulatedDist = 0f;
        CalibrationWalkDistance = 0f;
        CurrentCalibrationState = CalibrationState.WaitingForWalk;

        Debug.Log($"[QRLocationManager] Calibration started. Walk {CALIBRATION_WALK_THRESHOLD}m to calibrate direction.");
    }

    /// <summary>
    /// Forces calibration to complete immediately with a specific yaw offset.
    /// Used by the Editor simulator and for re-anchoring from a second QR scan.
    /// </summary>
    public void ForceCalibrate(float yawOffset)
    {
        CalibratedYawOffset = yawOffset;
        CurrentCalibrationState = CalibrationState.Calibrated;
        Debug.Log($"[QRLocationManager] Force-calibrated with yawOffset={yawOffset:F1}°");
        OnCalibrationComplete?.Invoke();
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
        CurrentCalibrationState = CalibrationState.NotCalibrated;
        CalibratedYawOffset = 0f;
        CalibrationWalkDistance = 0f;
        Debug.Log("[QRLocationManager] Location cleared.");
    }

    // ── Walk-to-Calibrate Update Loop ─────────────────────────────────────────

    void Update()
    {
        if (CurrentCalibrationState != CalibrationState.WaitingForWalk)
            return;

        Transform cam = Camera.main != null ? Camera.main.transform : null;
        if (cam == null)
            return;

        // Debounce: don't track in the first 1.5s (let AR fully stabilize after scanner closes)
        float elapsed = Time.time - m_CalibrationStartTime;
        if (elapsed < CALIBRATION_MIN_TIME)
        {
            // Keep resetting start position during debounce so jitter doesn't count
            m_CalibrationStartPos = cam.position;
            m_CalibrationPrevPos = cam.position;
            return;
        }

        Vector3 currentPos = cam.position;

        // Incremental step distance (frame-to-frame) in XZ plane
        Vector3 stepDelta = currentPos - m_CalibrationPrevPos;
        stepDelta.y = 0f;
        float stepDist = stepDelta.magnitude;

        // Reject AR relocalization jumps (>1m in a single frame is not real walking)
        if (stepDist > CALIBRATION_MAX_JUMP)
        {
            Debug.LogWarning($"[QRLocationManager] AR position jump detected ({stepDist:F2}m). Resetting calibration anchor.");
            m_CalibrationStartPos = currentPos;
            m_CalibrationPrevPos = currentPos;
            m_CalibrationAccumulatedDist = 0f;
            CalibrationWalkDistance = 0f;
            return;
        }

        // Only count movement above minimum speed (reject jitter / standing still)
        float speed = stepDist / Mathf.Max(Time.deltaTime, 0.001f);
        if (speed >= CALIBRATION_MIN_SPEED)
        {
            m_CalibrationAccumulatedDist += stepDist;
        }

        m_CalibrationPrevPos = currentPos;

        // Use straight-line displacement for direction, but accumulated distance for threshold
        Vector3 displacement = currentPos - m_CalibrationStartPos;
        displacement.y = 0f;
        CalibrationWalkDistance = m_CalibrationAccumulatedDist;

        if (m_CalibrationAccumulatedDist < CALIBRATION_WALK_THRESHOLD)
            return;

        // Also require that straight-line displacement is at least 50% of accumulated
        // (rejects pacing back and forth — user must walk in a consistent direction)
        float straightLine = displacement.magnitude;
        if (straightLine < m_CalibrationAccumulatedDist * 0.5f)
        {
            Debug.Log($"[QRLocationManager] Walk too meandering (straight={straightLine:F2}m, total={m_CalibrationAccumulatedDist:F2}m). Keep walking straight.");
            return;
        }

        // User has walked far enough in a straight line — compute the yaw offset
        TryCompleteCalibration(displacement);
    }

    // ── Core Calibration Logic ────────────────────────────────────────────────

    private void TryCompleteCalibration(Vector3 walkDisplacementXZ)
    {
        Vector3 walkDir = walkDisplacementXZ.normalized;

        // Get the start node's data and neighbors from the registry
        LocationRegistry registry = AppController.Instance != null
            ? AppController.Instance.Locations
            : FindObjectOfType<LocationRegistry>();

        if (registry == null || !registry.IsLoaded)
        {
            Debug.LogWarning("[QRLocationManager] Registry not loaded. Using compass fallback.");
            ForceCalibrationFromCompass();
            return;
        }

        LocationData startNode = registry.GetLocation(CurrentNodeId);
        if (startNode == null || startNode.neighbors == null || startNode.neighbors.Length == 0)
        {
            Debug.LogWarning("[QRLocationManager] Start node has no neighbors. Using compass fallback.");
            ForceCalibrationFromCompass();
            return;
        }

        // Compare the user's AR walk direction against each graph edge from the start node
        float bestDot = -2f;
        Vector3 bestEdgeDir = Vector3.forward;

        foreach (string neighborId in startNode.neighbors)
        {
            LocationData neighbor = registry.GetLocation(neighborId);
            if (neighbor == null)
                continue;

            // Map-space edge direction (XZ plane)
            Vector3 edgeDir = new Vector3(
                neighbor.x - startNode.x,
                0f,
                neighbor.z - startNode.z
            );

            if (edgeDir.sqrMagnitude < 0.0001f)
                continue;

            edgeDir.Normalize();

            float dot = Vector3.Dot(walkDir, edgeDir);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestEdgeDir = edgeDir;
            }
        }

        if (bestDot < 0.5f)
        {
            // Low confidence — user is walking at a steep angle to all edges.
            // Wait for more data or a straighter walk.
            Debug.Log($"[QRLocationManager] Low calibration confidence ({bestDot:F2}). Walk along a corridor for best results.");
            return;
        }

        // Compute yaw offset:
        // walkAngleAR  = angle of walk direction in AR world space
        // edgeAngleMap = angle of matched edge in map space
        // yawOffset    = how much to rotate map space to align with AR space
        float walkAngleAR  = Mathf.Atan2(walkDir.x, walkDir.z) * Mathf.Rad2Deg;
        float edgeAngleMap = Mathf.Atan2(bestEdgeDir.x, bestEdgeDir.z) * Mathf.Rad2Deg;
        float yawOffset = walkAngleAR - edgeAngleMap;

        CalibratedYawOffset = yawOffset;
        CurrentCalibrationState = CalibrationState.Calibrated;

        Debug.Log($"[QRLocationManager] ✅ Calibration complete! " +
                  $"walkAngle={walkAngleAR:F1}° edgeAngle={edgeAngleMap:F1}° " +
                  $"yawOffset={yawOffset:F1}° confidence={bestDot:F2}");

        OnCalibrationComplete?.Invoke();
    }

    private void ForceCalibrationFromCompass()
    {
        // Fallback: use compass heading as a rough alignment
        // This is the old behavior — unreliable indoors but better than nothing
        float camYaw = ScanCameraRotationY;
        float compassHeading = ScanCompassHeading;
        float yawOffset = camYaw - compassHeading;

        CalibratedYawOffset = yawOffset;
        CurrentCalibrationState = CalibrationState.Calibrated;

        Debug.LogWarning($"[QRLocationManager] Compass fallback calibration: yawOffset={yawOffset:F1}°");
        OnCalibrationComplete?.Invoke();
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
