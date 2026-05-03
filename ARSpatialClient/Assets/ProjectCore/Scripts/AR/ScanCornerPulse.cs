using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pulses the color of a corner bracket Image between two colors.
/// Attach to each corner bar (H and V) inside the ScanFrame.
/// </summary>
public class ScanCornerPulse : MonoBehaviour
{
    [SerializeField] private float speed = 1.4f;

    private Image m_Image;
    private float m_Offset;

    private static readonly Color ColorA = new Color(0.0f,  0.85f, 0.88f, 1f);
    private static readonly Color ColorB = new Color(0.0f,  0.60f, 0.62f, 0.45f);

    void Awake()
    {
        m_Image  = GetComponent<Image>();
        m_Offset = Random.Range(0f, Mathf.PI * 2f);  // stagger corners slightly
    }

    void Update()
    {
        if (m_Image == null) return;
        float t = (Mathf.Sin(Time.time * speed + m_Offset) + 1f) * 0.5f;
        m_Image.color = Color.Lerp(ColorA, ColorB, t);
    }
}
