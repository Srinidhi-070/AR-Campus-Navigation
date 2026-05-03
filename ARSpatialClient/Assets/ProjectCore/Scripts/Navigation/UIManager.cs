using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages all runtime UI text output for the navigation system.
/// Assign text references in the Inspector.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI m_DirectionText;
    [SerializeField] private TextMeshProUGUI m_StatusText;

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Displays a list of direction steps in the direction panel.</summary>
    public void DisplayDirections(List<string> directions)
    {
        if (m_DirectionText == null) return;
        m_DirectionText.text = directions != null ? string.Join("\n", directions) : string.Empty;
    }

    /// <summary>
    /// Shows a status message (e.g. "Thinking...", "Scanning...", "Error").
    /// Pass an empty string to clear.
    /// </summary>
    public void ShowStatus(string message)
    {
        if (m_StatusText == null) return;
        m_StatusText.text = message ?? string.Empty;
    }

    /// <summary>Clears all UI text.</summary>
    public void ClearAll()
    {
        if (m_DirectionText != null) m_DirectionText.text = string.Empty;
        if (m_StatusText    != null) m_StatusText.text    = string.Empty;
    }

    public void SetTextTargets(TextMeshProUGUI directionText, TextMeshProUGUI statusText)
    {
        m_DirectionText = directionText;
        m_StatusText = statusText;
    }
}
