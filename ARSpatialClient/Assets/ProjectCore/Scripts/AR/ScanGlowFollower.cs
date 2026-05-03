using UnityEngine;

/// <summary>
/// Mirrors the Y position of a target RectTransform every frame.
/// Used to keep the scan glow aligned with the animated scan line.
/// </summary>
public class ScanGlowFollower : MonoBehaviour
{
    [HideInInspector] public RectTransform target;

    private RectTransform m_RT;

    void Awake() => m_RT = GetComponent<RectTransform>();

    void LateUpdate()
    {
        if (target == null || m_RT == null) return;
        Vector2 pos = m_RT.anchoredPosition;
        pos.y = target.anchoredPosition.y;
        m_RT.anchoredPosition = pos;
    }
}
