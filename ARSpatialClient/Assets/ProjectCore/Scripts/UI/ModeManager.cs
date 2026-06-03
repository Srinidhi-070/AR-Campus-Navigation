using System.Collections;
using UnityEngine;

public class ModeManager : MonoBehaviour
{
    private CampusRuntimeUI m_UI;
    private PathVisualizer m_PathVisualizer;
    private bool m_ChatOpen;
    private bool m_MenuOpen;
    private bool m_ScannerOpen;

    public void Initialize(CampusRuntimeUI ui, PathVisualizer pathVisualizer)
    {
        m_UI = ui;
        m_PathVisualizer = pathVisualizer;
        EnterNavigationMode();
    }

    public void EnterScannerMode()
    {
        if (m_UI == null) return;

        m_ScannerOpen = true;
        m_ChatOpen = false;
        m_MenuOpen = false;
        m_UI.SetMenuVisible(false);
        m_UI.SetChatVisible(false);
        m_UI.SetNavigationChromeVisible(false);

        if (m_PathVisualizer != null)
            m_PathVisualizer.gameObject.SetActive(false);
    }

    public void EnterNavigationMode()
    {
        if (m_UI == null) return;

        m_ScannerOpen = false;
        m_UI.SetNavigationChromeVisible(true);
        m_UI.SetMenuVisible(m_MenuOpen);
        m_UI.SetChatVisible(m_ChatOpen);

        if (m_PathVisualizer != null)
            m_PathVisualizer.gameObject.SetActive(true);
    }

    public void ToggleMenu()
    {
        if (m_UI == null || m_ScannerOpen) return;

        m_MenuOpen = !m_MenuOpen;
        if (m_MenuOpen)
            m_ChatOpen = false;

        m_UI.SetChatVisible(m_ChatOpen);
        m_UI.SetMenuVisible(m_MenuOpen);
    }

    public void ToggleChat()
    {
        Debug.Log("[ModeManager] ToggleChat called");
        
        if (m_UI == null)
        {
            Debug.LogError("[ModeManager] UI is null!");
            return;
        }
        
        if (m_ScannerOpen)
        {
            Debug.Log("[ModeManager] Scanner is open, ignoring chat toggle");
            return;
        }

        m_ChatOpen = !m_ChatOpen;
        Debug.Log($"[ModeManager] Chat state toggled to: {m_ChatOpen}");
        
        if (m_ChatOpen)
        {
            m_MenuOpen = false;
            Debug.Log("[ModeManager] Closing menu because chat is opening");
        }

        m_UI.SetMenuVisible(m_MenuOpen);
        m_UI.SetChatVisible(m_ChatOpen);
        Debug.Log($"[ModeManager] UI updated - Menu: {m_MenuOpen}, Chat: {m_ChatOpen}");
        
        if (m_ChatOpen && m_UI.ChatInput != null)
        {
            StartCoroutine(FocusChatInput());
        }
    }
    
    private IEnumerator FocusChatInput()
    {
        yield return null;
        if (m_UI.ChatInput != null)
        {
            m_UI.ChatInput.ActivateInputField();
            m_UI.ChatInput.Select();
        }
    }

    public void CloseChat()
    {
        m_ChatOpen = false;
        if (m_UI != null)
            m_UI.SetChatVisible(false);
    }
}
