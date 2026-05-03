using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ZXING_ENABLED
using ZXing;
using ZXing.Common;
#endif

public class QRScanner : MonoBehaviour
{
    private GameObject m_ScanPanel;
    private TextMeshProUGUI m_StatusTMP;
    private TextMeshProUGUI m_LocationTMP;
    private RawImage m_PreviewImage;
    private ModeManager m_ModeManager;

#if ZXING_ENABLED
    private bool m_IsScanning;
    private BarcodeReaderGeneric m_BarcodeReader;
    private WebCamTexture m_CamTexture;
#endif

    void Awake()
    {
        if (QRLocationManager.Instance != null)
            QRLocationManager.Instance.OnLocationChanged += OnLocationSet;
    }

    void OnDestroy()
    {
        if (QRLocationManager.Instance != null)
            QRLocationManager.Instance.OnLocationChanged -= OnLocationSet;

#if ZXING_ENABLED
        StopCamera();
#endif
    }

    public void WireUI(
        GameObject scanPanel,
        TextMeshProUGUI statusTMP,
        TextMeshProUGUI locationTMP,
        RawImage previewImage,
        ModeManager modeManager)
    {
        m_ScanPanel = scanPanel;
        m_StatusTMP = statusTMP;
        m_LocationTMP = locationTMP;
        m_PreviewImage = previewImage;
        m_ModeManager = modeManager;

        if (QRLocationManager.Instance != null)
        {
            QRLocationManager.Instance.OnLocationChanged -= OnLocationSet;
            QRLocationManager.Instance.OnLocationChanged += OnLocationSet;
        }

        if (m_ScanPanel != null)
            m_ScanPanel.SetActive(false);
    }

    public void OpenScanner()
    {
        if (m_ScanPanel != null)
            m_ScanPanel.SetActive(true);
        if (m_ModeManager != null)
            m_ModeManager.EnterScannerMode();

        SetLocation(string.Empty);

#if ZXING_ENABLED && !UNITY_EDITOR
        SetStatus("Opening camera...");
        StartCoroutine(StartCameraScanner());
#elif UNITY_EDITOR
        // Editor test mode - show simple instruction
        SetStatus("EDITOR TEST MODE: QR scanning requires device camera.");
        SetLocation("Click X to close. Use MENU to navigate without QR.");
#else
        SetStatus("Camera not available.");
        SetLocation("ZXing library not configured.");
#endif
    }

    public void CloseScanner()
    {
#if ZXING_ENABLED
        m_IsScanning = false;
        StopCamera();
#endif
        if (m_ScanPanel != null)
            m_ScanPanel.SetActive(false);
        if (m_ModeManager != null)
            m_ModeManager.EnterNavigationMode();
    }

    public void SimulateScan(string qrJson)
    {
        ProcessQRResult(qrJson);
    }

#if ZXING_ENABLED
    private IEnumerator StartCameraScanner()
    {
        Debug.Log("[QRScanner] Starting camera scanner...");
        
#if UNITY_ANDROID && !UNITY_EDITOR
        // Request camera permission on Android
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            Debug.Log("[QRScanner] Requesting camera permission...");
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            
            // Wait for permission dialog
            float timeout = 0f;
            while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera) && timeout < 10f)
            {
                timeout += Time.deltaTime;
                yield return null;
            }
            
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                Debug.LogError("[QRScanner] Camera permission denied");
                SetStatus("Camera permission denied.");
                SetLocation("Please grant camera permission in Settings.");
                yield break;
            }
        }
        
        Debug.Log("[QRScanner] Camera permission granted");
#endif

        WebCamDevice[] devices = WebCamTexture.devices;
        Debug.Log($"[QRScanner] Found {devices.Length} camera devices");
        
        if (devices == null || devices.Length == 0)
        {
            Debug.LogError("[QRScanner] No camera found");
            SetStatus("No camera found.");
            SetLocation("Device does not have a camera.");
            yield break;
        }

        // Log available cameras
        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"[QRScanner] Camera {i}: {devices[i].name} (Front: {devices[i].isFrontFacing})");
        }

        // Prefer back camera for QR scanning
        string cameraName = devices[0].name;
        for (int i = 0; i < devices.Length; i++)
        {
            if (!devices[i].isFrontFacing)
            {
                cameraName = devices[i].name;
                Debug.Log($"[QRScanner] Using back camera: {cameraName}");
                break;
            }
        }

        if (m_CamTexture == null)
        {
            m_CamTexture = new WebCamTexture(cameraName, 1280, 720, 30);
            Debug.Log($"[QRScanner] Created WebCamTexture: {cameraName} 1280x720@30fps");
        }

        if (m_PreviewImage != null)
        {
            m_PreviewImage.texture = m_CamTexture;
            m_PreviewImage.color = Color.white;
            m_PreviewImage.enabled = true;
            m_PreviewImage.gameObject.SetActive(true);
            Debug.Log("[QRScanner] Assigned texture to preview image");
        }
        else
        {
            Debug.LogError("[QRScanner] Preview image is null!");
        }

        m_BarcodeReader = new BarcodeReaderGeneric();
        
        Debug.Log("[QRScanner] Starting camera...");
        m_CamTexture.Play();
        
        // Wait for camera to start
        float startTimeout = 0f;
        while (!m_CamTexture.isPlaying && startTimeout < 5f)
        {
            startTimeout += Time.deltaTime;
            yield return null;
        }
        
        if (!m_CamTexture.isPlaying)
        {
            Debug.LogError("[QRScanner] Camera failed to start");
            SetStatus("Camera failed to start.");
            SetLocation("Please restart the app.");
            yield break;
        }
        
        Debug.Log($"[QRScanner] Camera started: {m_CamTexture.width}x{m_CamTexture.height}");
        
        m_IsScanning = true;
        SetStatus("Point camera at a campus QR code.");

        yield return new WaitForSeconds(0.5f);
        StartCoroutine(ScanLoop());
    }

    private IEnumerator ScanLoop()
    {
        while (m_IsScanning)
        {
            yield return new WaitForSeconds(0.25f);

            if (m_CamTexture == null || !m_CamTexture.isPlaying || m_CamTexture.width < 100)
                continue;

            Color32[] pixels = m_CamTexture.GetPixels32();
            int width = m_CamTexture.width;
            int height = m_CamTexture.height;

            byte[] bytes = new byte[pixels.Length * 4];
            for (int i = 0; i < pixels.Length; i++)
            {
                bytes[i * 4] = pixels[i].r;
                bytes[i * 4 + 1] = pixels[i].g;
                bytes[i * 4 + 2] = pixels[i].b;
                bytes[i * 4 + 3] = pixels[i].a;
            }

            RGBLuminanceSource source = new RGBLuminanceSource(bytes, width, height);
            Result result = m_BarcodeReader.Decode(source);
            if (result == null || string.IsNullOrEmpty(result.Text))
                continue;

            m_IsScanning = false;
            ProcessQRResult(result.Text);
        }
    }

    private void StopCamera()
    {
        if (m_CamTexture != null && m_CamTexture.isPlaying)
            m_CamTexture.Stop();
        if (m_PreviewImage != null)
            m_PreviewImage.texture = null;
        m_CamTexture = null;
    }
#endif

    private void ProcessQRResult(string qrText)
    {
        bool success = QRLocationManager.Instance != null &&
                       QRLocationManager.Instance.ParseAndSetFromQR(qrText);

        if (!success)
        {
            SetStatus("Invalid campus QR code.");
            SetLocation(string.Empty);
            return;
        }

        string displayName = GetDisplayName(QRLocationManager.Instance.CurrentNodeId);
        SetStatus("Location detected.");
        SetLocation($"You are at: {displayName}");
        StartCoroutine(AutoCloseAfterSuccess());
    }

    private IEnumerator AutoCloseAfterSuccess()
    {
        yield return new WaitForSeconds(1.0f);
        CloseScanner();
    }

    private void OnLocationSet(string nodeId)
    {
        SetLocation($"You are at: {GetDisplayName(nodeId)}");
    }

    private void SetStatus(string message)
    {
        if (m_StatusTMP != null)
            m_StatusTMP.text = message ?? string.Empty;
    }

    private void SetLocation(string message)
    {
        if (m_LocationTMP != null)
            m_LocationTMP.text = message ?? string.Empty;
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
