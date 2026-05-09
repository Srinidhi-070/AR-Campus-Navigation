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
        // Create UI component
        m_UI = gameObject.AddComponent<QRScannerUI>();
        
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
