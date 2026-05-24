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

#if UNITY_EDITOR
    [Header("Editor QR Simulation (no camera needed)")]
    [SerializeField] private string m_TestNodeId = "HOUSE_ENTRANCE_1";
    [SerializeField] private string m_TestBuilding = "House";
    [SerializeField] private int m_TestFloor = 1;
#endif

    // Callbacks
    private System.Action<string> m_OnQRDetected;
    private System.Action m_OnClose;
    // Coroutine tracking
    private Coroutine m_PulseCoroutine;

    private Sprite m_RoundedSprite;
    public Sprite GetRoundedSprite()
    {
        if (m_RoundedSprite != null) return m_RoundedSprite;

        int size = 128;
        int cornerRadius = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (x < cornerRadius && y < cornerRadius)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, cornerRadius));
                    pixels[y * size + x] = dist <= cornerRadius ? Color.white : Color.clear;
                }
                else if (x >= size - cornerRadius && y < cornerRadius)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(size - cornerRadius - 1, cornerRadius));
                    pixels[y * size + x] = dist <= cornerRadius ? Color.white : Color.clear;
                }
                else if (x < cornerRadius && y >= size - cornerRadius)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, size - cornerRadius - 1));
                    pixels[y * size + x] = dist <= cornerRadius ? Color.white : Color.clear;
                }
                else if (x >= size - cornerRadius && y >= size - cornerRadius)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(size - cornerRadius - 1, size - cornerRadius - 1));
                    pixels[y * size + x] = dist <= cornerRadius ? Color.white : Color.clear;
                }
                else
                {
                    pixels[y * size + x] = Color.white;
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        m_RoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
        return m_RoundedSprite;
    }

    /// <summary>
    /// Creates the QR scanner UI at runtime.
    /// </summary>
    public void BuildUI(Transform parent, System.Action<string> onQRDetected, System.Action onClose)
    {
        m_OnQRDetected = onQRDetected;
        m_OnClose = onClose;

        // ── Full-screen panel ──
        m_ScannerPanel = new GameObject("QRScannerPanel");
        m_ScannerPanel.transform.SetParent(parent, false);

        RectTransform panelRT = m_ScannerPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        Image panelBg = m_ScannerPanel.AddComponent<Image>();
        panelBg.color = new Color(0.04f, 0.05f, 0.08f, 0.7f); // Dark overlay

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
        m_CameraFeed.gameObject.SetActive(false);

        // ── Center Scanner Card (The clear/rounded area) ──
        GameObject cardObj = new GameObject("ScannerCard");
        cardObj.transform.SetParent(m_ScannerPanel.transform, false);
        RectTransform cardRT = cardObj.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(800, 1400); // Tall portrait card

        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.sprite = GetRoundedSprite();
        cardBg.type = Image.Type.Sliced;
        cardBg.color = new Color(0.2f, 0.22f, 0.25f, 0.3f); // Slight tint, mostly clear to see camera

        // ── Title Text ──
        m_TitleText = CreateText(cardObj.transform, "Title", "Scan the QR code\nto locate yourself", 48,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-300, -250), new Vector2(300, -100));
        m_TitleText.alignment = TextAlignmentOptions.Center;
        m_TitleText.fontStyle = FontStyles.Bold;

        // ── Scanning Box Brackets ──
        m_ScanningBox = CreateScanningBox(cardObj.transform);

        // ── Instruction / Result Text (Optional, below brackets) ──
        m_InstructionText = CreateText(cardObj.transform, "Instructions", "", 32,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, -380), new Vector2(300, -320));
        m_InstructionText.alignment = TextAlignmentOptions.Center;
        m_InstructionText.color = new Color(0.8f, 0.82f, 0.85f, 1f);

        m_ResultText = CreateText(cardObj.transform, "Result", "", 34,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-300, -450), new Vector2(300, -380));
        m_ResultText.alignment = TextAlignmentOptions.Center;
        m_ResultText.color = new Color(0.25f, 0.85f, 0.4f, 1f);

        // ── Top Buttons (Back) ──
        m_CloseButton = CreateCircularButton(m_ScannerPanel.transform, "CloseButton", "←", new Vector2(0, 1), new Vector2(60, -60));
        m_CloseButton.onClick.AddListener(() => {
            if (m_OnClose != null) m_OnClose();
            else CloseScanner();
        });
        
        m_ScannerPanel.SetActive(false);
    }

    private Button CreatePillButton(Transform parent, string name, string label)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        Image img = btnObj.AddComponent<Image>();
        img.sprite = GetRoundedSprite();
        img.type = Image.Type.Sliced;
        img.color = new Color(0.3f, 0.33f, 0.38f, 0.9f); // Gray translucent pill

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        cb.colorMultiplier = 1f;
        btn.colors = cb;

        TextMeshProUGUI txt = CreateText(btnObj.transform, "Text", label, 36, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontStyle = FontStyles.Bold;

        return btn;
    }

    private Button CreateCircularButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPos)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(120, 120);

        Image img = btnObj.AddComponent<Image>();
        img.sprite = GetRoundedSprite();
        img.type = Image.Type.Sliced;
        img.color = new Color(0.2f, 0.22f, 0.25f, 0.8f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        cb.colorMultiplier = 1f;
        btn.colors = cb;

        TextMeshProUGUI txt = CreateText(btnObj.transform, "Text", label, 52, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontStyle = FontStyles.Bold;

        return btn;
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
        boxRT.anchoredPosition = new Vector2(0, 50); // Shift up slightly
        boxRT.sizeDelta = new Vector2(560, 560);

        Color bracketColor = new Color(0.6f, 0.65f, 0.7f, 1f); // Subtle greyish-white like the image
        float cornerLen = 80f;
        float thickness = 12f;

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
        img.sprite = GetRoundedSprite(); // Round the bracket lines!
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return img;
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
        // Editor/dev mode: simulate a successful QR scan so navigation can be tested in Simulator.
        // This prevents “nothing happens” in the Unity Editor.
        m_InstructionText.text = "Editor mode: simulating QR scan...";
        m_ResultText.text = $"Node: {m_TestNodeId}";

        StartCoroutine(SimulateEditorQrThenClose());
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

#if UNITY_EDITOR
    private IEnumerator SimulateEditorQrThenClose()
    {
        // tiny delay so the UI renders
        yield return null;
        yield return new WaitForSeconds(0.2f);

        // Must match QRLocationManager.QRPayload format:
        // {"building":"Main Block","floor":0,"node_id":"ENTRANCE"}
        string payload = $"{{\"building\":\"{m_TestBuilding}\",\"floor\":{m_TestFloor},\"node_id\":\"{m_TestNodeId}\"}}";

        if (m_CornerImages != null)
        {
            Color green = new Color(0.1f, 1f, 0.3f, 1f);
            foreach (var img in m_CornerImages)
                if (img != null) img.color = green;
        }

        if (m_InstructionText != null) m_InstructionText.text = "QR Code Detected!";
        if (m_ResultText != null) m_ResultText.text = "Processing...";

#if ZXING_ENABLED
        m_IsScanning = false;
#endif
        m_OnQRDetected?.Invoke(payload);

        // QRScanner.cs will call CloseScanner() after location set,
        // but we close the UI here too to keep Simulator responsive.
        CloseScanner();
    }
#endif

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

            bool imageAcquired = false;
            Color32[] pixels = null;
            int w = 0, h = 0;

            try
            {
                // Acquire the raw CPU image directly from ARCore
                if (cameraManager.TryAcquireLatestCpuImage(out UnityEngine.XR.ARSubsystems.XRCpuImage image))
                {
                    w = image.width;
                    h = image.height;

                    using (image) // GUARANTEES image is disposed even if an exception occurs below
                    {
                        successfulCaptures++;
                        
                        // Use higher resolution for reliable QR detection
                        // 320x240 was too low — QR codes need ~640px minimum to decode
                        
                        // Downscale aggressively to max 640 width to save CPU/Memory
                        // 640px is plenty for ZXing to read standard QR codes
                        if (w > 640)
                        {
                            h = (h * 640) / w;
                            w = 640;
                        }

                        var conversionParams = new UnityEngine.XR.ARSubsystems.XRCpuImage.ConversionParams
                        {
                            inputRect = new RectInt(0, 0, image.width, image.height),
                            outputDimensions = new Vector2Int(w, h),
                            outputFormat = TextureFormat.RGBA32,
                            transformation = UnityEngine.XR.ARSubsystems.XRCpuImage.Transformation.None
                        };

                        if (conversionTexture == null || conversionTexture.width != w || conversionTexture.height != h)
                        {
                            if (conversionTexture != null) Destroy(conversionTexture);
                            conversionTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
                            Debug.Log($"[QRScannerUI] Created conversion texture: {w}x{h}");
                        }

                        var rawTextureData = conversionTexture.GetRawTextureData<byte>();
                        image.Convert(conversionParams, rawTextureData);
                        // image is disposed automatically at the end of the using block
                    }
                    
                    conversionTexture.Apply();
                    pixels = conversionTexture.GetPixels32();
                    imageAcquired = true;
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

            // Outside of try-catch block so we can use yield return
            if (imageAcquired && pixels != null)
            {
                // Offload heavy ZXing decode to a background thread to prevent ARCore IMU timeout
                // This is CRITICAL to avoid blocking the main thread and crashing the AR Session!
                var decodeTask = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        var luminance = new Color32LuminanceSource(pixels, w, h);
                        return m_Reader.Decode(luminance);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[QRScannerUI] ZXing Decode exception: {ex.Message}");
                        return null;
                    }
                });

                // Yield until background thread finishes so Unity can process ARCore sensor events
                while (!decodeTask.IsCompleted)
                {
                    yield return null;
                }

                var result = decodeTask.Result;

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
