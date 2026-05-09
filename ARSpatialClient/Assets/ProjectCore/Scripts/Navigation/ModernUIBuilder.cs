using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// LEGACY: This script is from the old system and is NOT used in production runtime.
/// It's kept for reference but disabled by CampusRuntimeInstaller.
/// The new system uses CampusRuntimeUI.cs instead.
/// </summary>
public class ModernUIBuilder : MonoBehaviour
{
    void Awake()
    {
        Debug.LogWarning("[ModernUIBuilder] LEGACY: This script is disabled. Use CampusRuntimeUI instead.");
        enabled = false;
    }
}
