using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animates a scan line up and down inside the QR scan frame.
/// Attach to the ScanLine GameObject. Automatically starts/stops
/// when the QR panel is shown/hidden.
/// </summary>
public class ScanLineAnimator : MonoBehaviour
{
    [Tooltip("Total travel distance in pixels (half above, half below center)")]
    [SerializeField] private float travelDistance = 240f;

    [Tooltip("Seconds to complete one full sweep (top → bottom)")]
    [SerializeField] private float sweepDuration = 1.8f;

    private RectTransform m_RT;
    private Image         m_Image;
    private float         m_Time;
    private bool          m_Running;

    // Gradient colors for the scan line pulse
    private static readonly Color ColorBright = new Color(0.0f, 0.90f, 0.92f, 0.95f);
    private static readonly Color ColorFade   = new Color(0.0f, 0.90f, 0.92f, 0.20f);

    void Awake()
    {
        m_RT    = GetComponent<RectTransform>();
        m_Image = GetComponent<Image>();
    }

    void OnEnable()
    {
        m_Time    = 0f;
        m_Running = true;
    }

    void OnDisable() => m_Running = false;

    void Update()
    {
        if (!m_Running) return;

        m_Time += Time.deltaTime / sweepDuration;

        // Ping-pong: 0 → 1 → 0 → ...
        float t = Mathf.PingPong(m_Time, 1f);

        // Map t [0,1] → y position [-half, +half]
        float half = travelDistance * 0.5f;
        float y    = Mathf.Lerp(-half, half, t);

        m_RT.anchoredPosition = new Vector2(0f, y);

        // Pulse alpha: brightest at center of sweep, fades at edges
        float pulse = 1f - Mathf.Abs(t - 0.5f) * 2f;   // 0 at edges, 1 at center
        if (m_Image != null)
            m_Image.color = Color.Lerp(ColorFade, ColorBright, pulse);
    }
}
