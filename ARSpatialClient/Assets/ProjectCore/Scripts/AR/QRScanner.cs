using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// QR Scanner controller - uses QRScannerUI for display.
/// Handles QR detection and location validation.
/// </summary>
public class QRScanner : MonoBehaviour
{
    private QRScannerUI m_UI;
    private ModeManager m_ModeManager;

    void Awake()
    {
        // UI component is created lazily in WireUI() to avoid duplicates
        
        if (QRLocationManager.Instance != null)
            QRLocationManager.Instance.OnLocationChanged += OnLocationSet;
    }

    void OnDestroy()
    {
        if (QRLocationManager.Instance != null)
            QRLocationManager.Instance.OnLocationChanged -= OnLocationSet;
    }

    public void WireUI(Transform canvasParent, ModeManager modeManager)
    {
        m_ModeManager = modeManager;

        if (canvasParent == null)
        {
            Debug.LogError("[QRScanner] WireUI failed: canvasParent is null (ui.RootCanvas.transform not ready?).");
            return;
        }

        if (modeManager == null)
        {
            Debug.LogError("[QRScanner] WireUI failed: modeManager is null.");
            return;
        }

        // Lazily create UI if Awake didn't run yet / UI component missing
        if (m_UI == null)
        {
            m_UI = gameObject.AddComponent<QRScannerUI>();
            if (m_UI == null)
            {
                Debug.LogError("[QRScanner] WireUI failed: could not create QRScannerUI.");
                return;
            }
        }

        // Build the UI
        m_UI.BuildUI(canvasParent, OnQRDetected, CloseScanner);

        if (QRLocationManager.Instance != null)
        {
            QRLocationManager.Instance.OnLocationChanged -= OnLocationSet;
            QRLocationManager.Instance.OnLocationChanged += OnLocationSet;
        }
    }

    public void OpenScanner()
    {
        if (m_ModeManager != null)
            m_ModeManager.EnterScannerMode();
        
        if (m_UI != null)
            m_UI.OpenScanner();
    }

    public void CloseScanner()
    {
        if (m_UI != null)
            m_UI.CloseScanner();
        
        if (m_ModeManager != null)
            m_ModeManager.EnterNavigationMode();
    }

    private void OnQRDetected(string qrText)
    {
        Debug.Log($"[QRScanner] Processing QR: {qrText}");

        bool success = QRLocationManager.Instance != null &&
                       QRLocationManager.Instance.ParseAndSetFromQR(qrText);

        if (!success)
        {
            Debug.LogWarning("[QRScanner] Invalid QR code");
            CampusRuntimeUI ui = FindObjectOfType<CampusRuntimeUI>();
            if (ui != null) ui.ShowStatus("Invalid QR code format.");
            CloseScanner();
            return;
        }

        string displayName = GetDisplayName(QRLocationManager.Instance.CurrentNodeId);
        Debug.Log($"[QRScanner] Location set: {displayName}");

        // Start walk-to-calibrate — user must walk 2-3 steps to determine heading
        QRLocationManager.Instance.BeginCalibration();

        // Auto-close scanner and return to navigation mode
        CloseScanner();
    }

    private void OnLocationSet(string nodeId)
    {
        Debug.Log($"[QRScanner] Location changed to: {nodeId}");
    }

    private string GetDisplayName(string nodeId)
    {
        LocationRegistry registry = AppController.Instance != null
            ? AppController.Instance.Locations
            : FindObjectOfType<LocationRegistry>();

        if (registry == null)
            return nodeId;

        LocationData location = registry.GetLocation(nodeId);
        return location != null ? location.displayName : nodeId;
    }
}
