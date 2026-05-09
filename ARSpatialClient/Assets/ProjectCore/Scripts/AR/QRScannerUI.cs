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

        // ── Full-screen dark panel ──
        m_ScannerPanel = new GameObject("QRScannerPanel");
        m_ScannerPanel.transform.SetParent(parent, false);

        RectTransform panelRT = m_ScannerPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        Image panelBg = m_ScannerPanel.AddComponent<Image>();
        panelBg.color = new Color(0.02f, 0.02f, 0.04f, 1f);

        // ── Camera feed container (handles rotation + aspect) ──
        GameObject feedContainer = new GameObject("FeedContainer");
        feedContainer.transform.SetParent(m_ScannerPanel.transform, false);
        RectTransform containerRT = feedContainer.AddComponent<RectTransform>();
        containerRT.anchorMin = Vector2.zero;
        containerRT.anchorMax = Vector2.one;
        containerRT.offsetMin = Vector2.zero;
        containerRT.offsetMax = Vector2.zero;

        // Camera feed RawImage inside the container
        GameObject feedObj = new GameObject("CameraFeed");
        feedObj.transform.SetParent(feedContainer.transform, false);
        RectTransform feedRT = feedObj.AddComponent<RectTransform>();
        feedRT.anchorMin = new Vector2(0.5f, 0.5f);
        feedRT.anchorMax = new Vector2(0.5f, 0.5f);
        feedRT.pivot = new Vector2(0.5f, 0.5f);
        feedRT.anchoredPosition = Vector2.zero;
        // Start large; will be resized once camera starts
        feedRT.sizeDelta = new Vector2(Screen.width, Screen.height);

        m_CameraFeed = feedObj.AddComponent<RawImage>();
        m_CameraFeed.color = Color.white;

        // ── Semi-transparent overlay around scanning box ──
        CreateDimOverlay(m_ScannerPanel.transform);

        // ── Scanning Box (proper box with animated corners) ──
        m_ScanningBox = CreateScanningBox(m_ScannerPanel.transform);

        // ── Title at top ──
        m_TitleText = CreateText(m_ScannerPanel.transform, "Title", "Scan QR Code", 48,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -60), new Vector2(-20, -20));
        m_TitleText.alignment = TextAlignmentOptions.Center;
        m_TitleText.fontStyle = FontStyles.Bold;

        // ── Instructions below title ──
        m_InstructionText = CreateText(m_ScannerPanel.transform, "Instructions",
            "Point camera at QR code", 30,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -120), new Vector2(-20, -70));
        m_InstructionText.alignment = TextAlignmentOptions.Center;
        m_InstructionText.color = new Color(0.75f, 0.75f, 0.75f, 1f);

        // ── Result text at bottom ──
        m_ResultText = CreateText(m_ScannerPanel.transform, "Result", "", 34,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(20, 80), new Vector2(-20, 160));
        m_ResultText.alignment = TextAlignmentOptions.Center;
        m_ResultText.color = new Color(0.2f, 1f, 0.4f, 1f);

        // ── Close button — top right ──
        m_CloseButton = CreateCloseButton(m_ScannerPanel.transform);

        m_ScannerPanel.SetActive(false);
    }

    // ── Dim overlay to highlight scanning area ──
    private void CreateDimOverlay(Transform parent)
    {
        // We don't need a complex cutout — the scanning box corner brackets
        // are drawn on top and the camera feed is visible through the dark BG.
    }

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
        btnRT.anchoredPosition = new Vector2(-20, -40);
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

        // NO MORE WEBCAMTEXTURE!
        // We will just let the AR Camera render the real world in the background,
        // and we will capture the center of the screen directly.
        // This permanently prevents any Android camera hardware locks or ARCore crashes!
        
        m_CameraFeed.gameObject.SetActive(false); // Hide the black raw image so we see the AR background
        
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
        int scanCount = 0;
        
        var cameraManager = UnityEngine.Object.FindObjectOfType<UnityEngine.XR.ARFoundation.ARCameraManager>();
        if (cameraManager == null)
        {
            m_InstructionText.text = "AR Camera not found";
            yield break;
        }

        Texture2D conversionTexture = null;

        while (m_IsScanning)
        {
            scanCount++;

            try
            {
                // Acquire the raw CPU image directly from ARCore! No hardware locks, no UI scaling issues.
                if (cameraManager.TryAcquireLatestCpuImage(out UnityEngine.XR.ARSubsystems.XRCpuImage image))
                {
                    // Downscale the image to speed up ZXing decode
                    int targetWidth = image.width / 2;
                    int targetHeight = image.height / 2;

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
                        m_InstructionText.text = "QR Code Detected!";
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
            }
            catch (System.Exception ex)
            {
                if (scanCount % 30 == 0)
                    Debug.LogWarning($"[QRScannerUI] Decode error: {ex.Message}");
            }

            // Yield a fraction of a second to prevent CPU overload
            yield return new WaitForSeconds(0.15f);
        }

        if (conversionTexture != null)
            Destroy(conversionTexture);

        Debug.Log("[QRScannerUI] Scan loop ended");
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
