using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ZXING_ENABLED
using ZXing;
#endif

/// <summary>
/// QR Scanner UI — full-screen camera feed with centered scanning box.
/// Properly handles camera orientation, aspect ratio, and device permissions.
/// </summary>
public class QRScannerUI : MonoBehaviour
{
    // UI References
    private GameObject m_ScannerPanel;
    private RawImage m_CameraFeed;
    private AspectRatioFitter m_AspectFitter;
    private GameObject m_ScanningBox;
    private TextMeshProUGUI m_TitleText;
    private TextMeshProUGUI m_InstructionText;
    private TextMeshProUGUI m_ResultText;
    private Button m_CloseButton;
    private Image[] m_CornerImages;

    // Camera
#if ZXING_ENABLED
    private BarcodeReaderGeneric m_Reader;
    private bool m_IsScanning;
#endif

    // Callbacks
    private System.Action<string> m_OnQRDetected;
    private System.Action m_OnClose;
    // Coroutine tracking
    private Coroutine m_PulseCoroutine;

    /// <summary>
    /// Creates the QR scanner UI at runtime.
    /// </summary>
    public void BuildUI(Transform parent, System.Action<string> onQRDetected, System.Action onClose)
    {
        m_OnQRDetected = onQRDetected;
        m_OnClose = onClose;

        // ── Full-screen panel — TRANSPARENT so AR camera passthrough is visible ──
        m_ScannerPanel = new GameObject("QRScannerPanel");
        m_ScannerPanel.transform.SetParent(parent, false);

        RectTransform panelRT = m_ScannerPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        // Semi-transparent dark overlay — lets AR camera show through
        Image panelBg = m_ScannerPanel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.55f);

        // Camera feed RawImage — hidden because AR camera renders the real world behind this panel
        GameObject feedObj = new GameObject("CameraFeed");
        feedObj.transform.SetParent(m_ScannerPanel.transform, false);
        RectTransform feedRT = feedObj.AddComponent<RectTransform>();
        feedRT.anchorMin = Vector2.zero;
        feedRT.anchorMax = Vector2.one;
        feedRT.offsetMin = Vector2.zero;
        feedRT.offsetMax = Vector2.zero;
        m_CameraFeed = feedObj.AddComponent<RawImage>();
        m_CameraFeed.color = Color.white;
        m_CameraFeed.gameObject.SetActive(false); // Hidden — AR camera passthrough is our "feed"

        // ── Scanning Box (proper box with animated corners) ──
        m_ScanningBox = CreateScanningBox(m_ScannerPanel.transform);

        // ── Title — positioned below notch/front camera safe area ──
        m_TitleText = CreateText(m_ScannerPanel.transform, "Title", "Scan QR Code", 44,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -140), new Vector2(-20, -90));
        m_TitleText.alignment = TextAlignmentOptions.Center;
        m_TitleText.fontStyle = FontStyles.Bold;

        // ── Instructions below title ──
        m_InstructionText = CreateText(m_ScannerPanel.transform, "Instructions",
            "Point camera at QR code", 28,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -190), new Vector2(-20, -145));
        m_InstructionText.alignment = TextAlignmentOptions.Center;
        m_InstructionText.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        // ── Result text at bottom ──
        m_ResultText = CreateText(m_ScannerPanel.transform, "Result", "", 34,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 100), new Vector2(-20, 180));
        m_ResultText.alignment = TextAlignmentOptions.Center;
        m_ResultText.color = new Color(0.2f, 1f, 0.4f, 1f);

        // ── Close button — top right, below notch ──
        m_CloseButton = CreateCloseButton(m_ScannerPanel.transform);

        m_ScannerPanel.SetActive(false);
    }

    // ── Dim overlay is now the panel background itself (semi-transparent) ──
    // The scanning box corner brackets are drawn on top and the AR camera
    // passthrough is visible through the semi-transparent panel.

    // ── Scanning Box with 4 solid corner brackets ──
    private GameObject CreateScanningBox(Transform parent)
    {
        GameObject box = new GameObject("ScanningBox");
        box.transform.SetParent(parent, false);

        RectTransform boxRT = box.AddComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.anchoredPosition = Vector2.zero;
        boxRT.sizeDelta = new Vector2(560, 560);

        Color bracketColor = new Color(0f, 0.9f, 0.85f, 1f); // Cyan-teal
        float cornerLen = 70f;
        float thickness = 6f;

        m_CornerImages = new Image[8]; // 2 per corner (H + V)
        int idx = 0;

        // Top-Left
        m_CornerImages[idx++] = CreateBracketLine(box.transform, "TL_H", new Vector2(0, 1), cornerLen, thickness, bracketColor, true, false);
        m_CornerImages[idx++] = CreateBracketLine(box.transform, "TL_V", new Vector2(0, 1), thickness, cornerLen, bracketColor, true, false);
        // Top-Right
        m_CornerImages[idx++] = CreateBracketLine(box.transform, "TR_H", new Vector2(1, 1), cornerLen, thickness, bracketColor, false, false);
        m_CornerImages[idx++] = CreateBracketLine(box.transform, "TR_V", new Vector2(1, 1), thickness, cornerLen, bracketColor, false, false);
        // Bottom-Left
        m_CornerImages[idx++] = CreateBracketLine(box.transform, "BL_H", new Vector2(0, 0), cornerLen, thickness, bracketColor, true, true);
        m_CornerImages[idx++] = CreateBracketLine(box.transform, "BL_V", new Vector2(0, 0), thickness, cornerLen, bracketColor, true, true);
        // Bottom-Right
        m_CornerImages[idx++] = CreateBracketLine(box.transform, "BR_H", new Vector2(1, 0), cornerLen, thickness, bracketColor, false, true);
        m_CornerImages[idx++] = CreateBracketLine(box.transform, "BR_V", new Vector2(1, 0), thickness, cornerLen, bracketColor, false, true);

        // Subtle border lines connecting corners (thin guide lines)
        CreateGuideLines(box.transform);

        return box;
    }

    private Image CreateBracketLine(Transform parent, string name, Vector2 anchor,
        float width, float height, Color color, bool anchorLeft, bool anchorBottom)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(width, height);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private void CreateGuideLines(Transform parent)
    {
        Color lineColor = new Color(1f, 1f, 1f, 0.12f);
        // Top line
        CreateBracketLine(parent, "Top", new Vector2(0, 1), 0, 1, lineColor, true, false)
            .rectTransform.anchorMax = new Vector2(1, 1);
        // Bottom line
        CreateBracketLine(parent, "Bottom", new Vector2(0, 0), 0, 1, lineColor, true, true)
            .rectTransform.anchorMax = new Vector2(1, 0);
        // Left line
        CreateBracketLine(parent, "Left", new Vector2(0, 0), 1, 0, lineColor, true, true)
            .rectTransform.anchorMax = new Vector2(0, 1);
        // Right line
        CreateBracketLine(parent, "Right", new Vector2(1, 0), 1, 0, lineColor, false, true)
            .rectTransform.anchorMax = new Vector2(1, 1);
    }

    private Button CreateCloseButton(Transform parent)
    {
        GameObject btnObj = new GameObject("CloseButton");
        btnObj.transform.SetParent(parent, false);

        RectTransform btnRT = btnObj.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 1);
        btnRT.anchorMax = new Vector2(1, 1);
        btnRT.pivot = new Vector2(1, 1);
        btnRT.anchoredPosition = new Vector2(-20, -100); // Lower to avoid notch/front camera
        btnRT.sizeDelta = new Vector2(90, 90);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.9f, 0.15f, 0.15f, 0.92f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        ColorBlock cb = btn.colors;
        cb.pressedColor = new Color(0.7f, 0.1f, 0.1f, 1f);
        btn.colors = cb;

        // X text
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRT = txtObj.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = "X";
        txt.fontSize = 52;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        txt.fontStyle = FontStyles.Bold;
        txt.raycastTarget = false;

        btn.onClick.AddListener(() => {
            if (m_OnClose != null) m_OnClose();
            else CloseScanner();
        });

        return btn;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;

        return tmp;
    }

    // ── Scanner Open / Close ──

    public void OpenScanner()
    {
        if (m_ScannerPanel == null) return;

        m_ScannerPanel.SetActive(true);
        m_ResultText.text = "";
        m_InstructionText.text = "Point camera at QR code";

        // Start corner pulse animation
        if (m_PulseCoroutine != null) StopCoroutine(m_PulseCoroutine);
        m_PulseCoroutine = StartCoroutine(PulseCorners());

#if ZXING_ENABLED && !UNITY_EDITOR
        StartCoroutine(StartCamera());
#elif UNITY_EDITOR
        m_InstructionText.text = "QR scanning requires device camera";
        m_ResultText.text = "Build to Android device to test";
#else
        m_InstructionText.text = "Camera not available";
#endif
    }

    public void CloseScanner()
    {
        if (m_PulseCoroutine != null)
        {
            StopCoroutine(m_PulseCoroutine);
            m_PulseCoroutine = null;
        }

#if ZXING_ENABLED
        StopCamera();
#endif
        if (m_ScannerPanel != null)
            m_ScannerPanel.SetActive(false);
    }

    // ── Corner Pulse Animation ──

    private IEnumerator PulseCorners()
    {
        if (m_CornerImages == null) yield break;

        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * 2f;
            float alpha = 0.6f + 0.4f * Mathf.Sin(t);
            Color c = new Color(0f, 0.9f, 0.85f, alpha);
            foreach (var img in m_CornerImages)
            {
                if (img != null) img.color = c;
            }
            yield return null;
        }
    }

    // ── Camera Handling ──

#if ZXING_ENABLED
    private IEnumerator StartCamera()
    {
        Debug.Log("[QRScannerUI] Starting AR screen capture mode...");

        // The AR Camera renders the real world behind our transparent scanner panel.
        // We capture frames via XRCpuImage for QR decoding — no WebCamTexture needed!
        // This prevents Android camera hardware locks and ARCore crashes.
        
        if (m_CameraFeed != null)
            m_CameraFeed.gameObject.SetActive(false); // Already hidden — AR passthrough is our feed
        
        // Initialize barcode reader
        m_Reader = new BarcodeReaderGeneric();
        m_Reader.Options.TryHarder = true;
        m_Reader.Options.PossibleFormats = new System.Collections.Generic.List<BarcodeFormat>
        {
            BarcodeFormat.QR_CODE
        };

        m_InstructionText.text = "Align QR code inside the box";
        m_IsScanning = true;

        StartCoroutine(ScanLoop());
        yield break;
    }

    private void FixCameraDisplay()
    {
        // Not needed anymore since we are reading from the screen!
    }

    private IEnumerator ScanLoop()
    {
        // ── Wait for ARCameraManager to be available ──
        // On app startup, AR components may not be ready immediately.
        UnityEngine.XR.ARFoundation.ARCameraManager cameraManager = null;
        float waitStart = Time.time;
        float maxWait = 8f; // Wait up to 8 seconds for AR to initialize
        
        Debug.Log("[QRScannerUI] Looking for ARCameraManager...");
        
        while (cameraManager == null && (Time.time - waitStart) < maxWait)
        {
            cameraManager = UnityEngine.Object.FindObjectOfType<UnityEngine.XR.ARFoundation.ARCameraManager>();
            if (cameraManager == null)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        if (cameraManager == null)
        {
            Debug.LogError("[QRScannerUI] ARCameraManager not found after waiting!");
            if (m_InstructionText != null)
                m_InstructionText.text = "AR Camera not available";
            yield break;
        }
        
        Debug.Log($"[QRScannerUI] ARCameraManager found: {cameraManager.gameObject.name}");
        
        // ── Wait for AR session to be ready ──
        var arSession = UnityEngine.Object.FindObjectOfType<UnityEngine.XR.ARFoundation.ARSession>();
        if (arSession != null)
        {
            Debug.Log("[QRScannerUI] Waiting for AR session to be ready...");
            float sessionWait = Time.time;
            while ((Time.time - sessionWait) < 5f)
            {
                if (UnityEngine.XR.ARFoundation.ARSession.state == UnityEngine.XR.ARFoundation.ARSessionState.SessionTracking)
                {
                    Debug.Log("[QRScannerUI] AR session is tracking!");
                    break;
                }
                yield return new WaitForSeconds(0.3f);
            }
            Debug.Log($"[QRScannerUI] AR session state: {UnityEngine.XR.ARFoundation.ARSession.state}");
        }
        
        // ── Warm-up: wait for first CPU image ──
        Debug.Log("[QRScannerUI] Warming up camera...");
        yield return new WaitForSeconds(0.5f);

        Texture2D conversionTexture = null;
        int scanCount = 0;
        int successfulCaptures = 0;
        int failedCaptures = 0;

        Debug.Log("[QRScannerUI] Starting scan loop...");

        while (m_IsScanning)
        {
            scanCount++;

            try
            {
                // Acquire the raw CPU image directly from ARCore
                if (cameraManager.TryAcquireLatestCpuImage(out UnityEngine.XR.ARSubsystems.XRCpuImage image))
                {
                    successfulCaptures++;
                    
                    // Use higher resolution for reliable QR detection
                    // 320x240 was too low — QR codes need ~640px minimum to decode
                    int targetWidth = image.width;
                    int targetHeight = image.height;
                    
                    // Only downscale if very large (saves CPU while keeping QR readable)
                    if (image.width > 1280)
                    {
                        targetWidth = image.width * 3 / 4;
                        targetHeight = image.height * 3 / 4;
                    }

                    var conversionParams = new UnityEngine.XR.ARSubsystems.XRCpuImage.ConversionParams
                    {
                        inputRect = new RectInt(0, 0, image.width, image.height),
                        outputDimensions = new Vector2Int(targetWidth, targetHeight),
                        outputFormat = TextureFormat.RGBA32,
                        transformation = UnityEngine.XR.ARSubsystems.XRCpuImage.Transformation.None
                    };

                    if (conversionTexture == null || conversionTexture.width != targetWidth || conversionTexture.height != targetHeight)
                    {
                        if (conversionTexture != null) Destroy(conversionTexture);
                        conversionTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
                        Debug.Log($"[QRScannerUI] Created conversion texture: {targetWidth}x{targetHeight}");
                    }

                    var rawTextureData = conversionTexture.GetRawTextureData<byte>();
                    image.Convert(conversionParams, rawTextureData);
                    image.Dispose(); // MUST dispose to prevent memory leak
                    
                    conversionTexture.Apply();

                    Color32[] pixels = conversionTexture.GetPixels32();
                    var luminance = new Color32LuminanceSource(pixels, targetWidth, targetHeight);
                    var result = m_Reader.Decode(luminance);

                    if (result != null && !string.IsNullOrEmpty(result.Text))
                    {
                        Debug.Log($"[QRScannerUI] ✅ QR detected: {result.Text}");
                        m_IsScanning = false;
                        
                        if (m_InstructionText != null)
                            m_InstructionText.text = "QR Code Detected!";
                        if (m_ResultText != null)
                            m_ResultText.text = "Processing...";

                        if (m_CornerImages != null)
                        {
                            Color green = new Color(0.1f, 1f, 0.3f, 1f);
                            foreach (var img in m_CornerImages)
                                if (img != null) img.color = green;
                        }

                        m_OnQRDetected?.Invoke(result.Text);
                        break;
                    }
                }
                else
                {
                    failedCaptures++;
                    if (failedCaptures == 1 || failedCaptures % 20 == 0)
                        Debug.Log($"[QRScannerUI] TryAcquireLatestCpuImage returned false (attempt {failedCaptures})");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[QRScannerUI] Frame {scanCount} error: {ex.GetType().Name}: {ex.Message}");
            }

            // Log progress periodically
            if (scanCount % 30 == 0)
                Debug.Log($"[QRScannerUI] Scanning... frames={scanCount}, captures={successfulCaptures}, fails={failedCaptures}");

            yield return new WaitForSeconds(0.15f);
        }

        if (conversionTexture != null)
            Destroy(conversionTexture);

        Debug.Log($"[QRScannerUI] Scan loop ended. Total frames={scanCount}, captures={successfulCaptures}, fails={failedCaptures}");
    }

    private void StopCamera()
    {
        m_IsScanning = false;
        // No camera to stop! The AR Camera continues running flawlessly in the background.
    }
#endif

    void OnDestroy()
    {
#if ZXING_ENABLED
        StopCamera();
#endif
    }
}
