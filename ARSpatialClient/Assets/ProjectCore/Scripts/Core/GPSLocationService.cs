using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides GPS-based features for AR Campus Navigation:
/// 1. Auto-detects which building the user is near using GPS coordinates
/// 2. Provides GPS-derived heading (from movement) as a more reliable alternative to compass
/// 
/// GPS is used as a SUPPLEMENTARY signal — QR scanning remains the primary indoor positioning method.
/// GPS accuracy is ~5-15m which is sufficient for building-level detection but not room-level.
/// </summary>
public class GPSLocationService : MonoBehaviour
{
    // ── Building GPS Database ──
    // Add entries here for each campus building. GPS coordinates (WGS84).
    [Serializable]
    public class BuildingGPS
    {
        public string buildingName;
        public double latitude;
        public double longitude;
        public float radiusMeters = 100f; // Detection radius around the building center
    }

    [Header("Building GPS Coordinates")]
    [SerializeField] private List<BuildingGPS> m_Buildings = new List<BuildingGPS>
    {
        new BuildingGPS
        {
            buildingName = "Apex Block",
            latitude = 13.029972,
            longitude = 77.564972,
            radiusMeters = 150f
        }
    };

    [Header("GPS Settings")]
    [SerializeField] private float m_DesiredAccuracyMeters = 10f;
    [SerializeField] private float m_UpdateDistanceMeters = 2f;
    [SerializeField] private float m_HeadingUpdateInterval = 0.5f;

    // ── Public State ──
    public bool IsGPSAvailable { get; private set; }
    public bool HasGPSFix { get; private set; }
    public float GPSAccuracy { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    /// <summary>GPS-derived heading from movement (degrees, 0=North, clockwise). -1 if unavailable.</summary>
    public float GPSHeading { get; private set; } = -1f;
    public bool HasGPSHeading => GPSHeading >= 0f;

    /// <summary>Name of the nearest detected building, or null if none within range.</summary>
    public string DetectedBuilding { get; private set; }

    // ── Events ──
    public event Action<string> OnBuildingDetected;
    public event Action<float> OnGPSHeadingUpdated;

    // ── Internal State ──
    private double m_PrevLatitude;
    private double m_PrevLongitude;
    private bool m_HasPreviousPosition;
    private float m_HeadingTimer;
    private bool m_IsStarting;

    void Start()
    {
        StartCoroutine(InitializeGPS());
    }

    private IEnumerator InitializeGPS()
    {
        m_IsStarting = true;

        // Check if user has location services enabled
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("[GPSLocationService] Location services disabled by user.");
            IsGPSAvailable = false;
            m_IsStarting = false;
            yield break;
        }

        // Start the location service
        Input.location.Start(m_DesiredAccuracyMeters, m_UpdateDistanceMeters);

        // Wait for initialization (max 20 seconds)
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait <= 0 || Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogWarning($"[GPSLocationService] GPS initialization failed. Status: {Input.location.status}");
            IsGPSAvailable = false;
            m_IsStarting = false;
            yield break;
        }

        IsGPSAvailable = true;
        m_IsStarting = false;
        Debug.Log("[GPSLocationService] GPS initialized successfully.");

        // Initial reading
        UpdateGPSPosition();
    }

    void Update()
    {
        if (!IsGPSAvailable || m_IsStarting) return;
        if (Input.location.status != LocationServiceStatus.Running) return;

        // Periodically update GPS position and heading
        m_HeadingTimer += Time.deltaTime;
        if (m_HeadingTimer >= m_HeadingUpdateInterval)
        {
            m_HeadingTimer = 0f;
            UpdateGPSPosition();
        }
    }

    private void UpdateGPSPosition()
    {
        LocationInfo loc = Input.location.lastData;
        Latitude = loc.latitude;
        Longitude = loc.longitude;
        GPSAccuracy = loc.horizontalAccuracy;
        HasGPSFix = GPSAccuracy < 50f; // Consider it a fix if under 50m accuracy

        if (!HasGPSFix) return;

        // ── Building Detection ──
        string previousBuilding = DetectedBuilding;
        DetectedBuilding = null;
        float closestDistance = float.MaxValue;

        foreach (BuildingGPS building in m_Buildings)
        {
            float distance = HaversineDistance(Latitude, Longitude, building.latitude, building.longitude);
            if (distance < building.radiusMeters && distance < closestDistance)
            {
                closestDistance = distance;
                DetectedBuilding = building.buildingName;
            }
        }

        if (DetectedBuilding != null && DetectedBuilding != previousBuilding)
        {
            Debug.Log($"[GPSLocationService] Building detected: {DetectedBuilding} (distance: {closestDistance:F0}m, accuracy: {GPSAccuracy:F0}m)");
            OnBuildingDetected?.Invoke(DetectedBuilding);
        }

        // ── GPS Heading from Movement ──
        // GPS heading derived from consecutive positions is FAR more reliable than
        // the magnetometer compass indoors. We use it when the user has moved enough.
        if (m_HasPreviousPosition)
        {
            float moveDist = HaversineDistance(m_PrevLatitude, m_PrevLongitude, Latitude, Longitude);

            // Only compute heading if user moved at least 2 meters (avoids GPS jitter)
            if (moveDist > 2f)
            {
                float heading = ComputeBearing(m_PrevLatitude, m_PrevLongitude, Latitude, Longitude);
                GPSHeading = heading;

                Debug.Log($"[GPSLocationService] GPS heading: {heading:F1}° (moved {moveDist:F1}m)");
                OnGPSHeadingUpdated?.Invoke(heading);

                m_PrevLatitude = Latitude;
                m_PrevLongitude = Longitude;
            }
        }
        else
        {
            m_PrevLatitude = Latitude;
            m_PrevLongitude = Longitude;
            m_HasPreviousPosition = true;
        }
    }

    /// <summary>
    /// Haversine formula — distance in meters between two GPS coordinates.
    /// </summary>
    private static float HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters

        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return (float)(R * c);
    }

    /// <summary>
    /// Compute bearing (heading) in degrees from point 1 to point 2.
    /// Returns 0-360 where 0=North, 90=East.
    /// </summary>
    private static float ComputeBearing(double lat1, double lon1, double lat2, double lon2)
    {
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double lat1Rad = lat1 * Math.PI / 180.0;
        double lat2Rad = lat2 * Math.PI / 180.0;

        double y = Math.Sin(dLon) * Math.Cos(lat2Rad);
        double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) -
                   Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);

        double bearing = Math.Atan2(y, x) * 180.0 / Math.PI;
        return (float)((bearing + 360.0) % 360.0);
    }

    void OnDestroy()
    {
        if (Input.location.status == LocationServiceStatus.Running)
            Input.location.Stop();
    }
}
