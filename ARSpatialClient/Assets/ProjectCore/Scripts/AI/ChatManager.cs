using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour
{
    [SerializeField] private CampusApiClient m_ApiClient;
    [SerializeField] private NavigationFlowController m_NavigationFlow;

    [HideInInspector] public TMP_InputField inputField;
    [HideInInspector] public Transform chatContainer;
    [HideInInspector] public TextMeshProUGUI statusText;
    [HideInInspector] public ScrollRect chatScrollRect;

    private readonly List<CampusApiClient.ChatHistoryMessage> m_History = new List<CampusApiClient.ChatHistoryMessage>();
    private const int MaxHistoryMessages = 20;
    private GameObject m_ThinkingBubble;

    public void Configure(
        CampusApiClient apiClient,
        NavigationFlowController navigationFlow,
        TMP_InputField chatInput,
        Transform contentRoot,
        TextMeshProUGUI status,
        ScrollRect scrollRect)
    {
        m_ApiClient = apiClient;
        m_NavigationFlow = navigationFlow;
        inputField = chatInput;
        chatContainer = contentRoot;
        statusText = status;
        chatScrollRect = scrollRect;

        // Force the container to expand rows to full width so bubble alignment works
        if (chatContainer != null)
        {
            VerticalLayoutGroup vlg = chatContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childControlWidth = true;
                vlg.childForceExpandWidth = true;
            }
        }
    }

    public void SendChatMessage(string userText)
    {
        userText = (userText ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(userText))
            return;

        SpawnBubble(userText, true);
        m_History.Add(new CampusApiClient.ChatHistoryMessage { role = "user", content = userText });

        // Cap history to prevent unbounded growth and oversized API payloads
        while (m_History.Count > MaxHistoryMessages)
            m_History.RemoveAt(0);

        if (inputField != null)
            inputField.text = string.Empty;

        if (m_ApiClient == null || m_NavigationFlow == null)
        {
            SpawnBubble("Chat is not configured yet.", false);
            return;
        }

        if (m_ThinkingBubble != null)
            Destroy(m_ThinkingBubble);
            
        m_ThinkingBubble = SpawnBubble("Thinking...", false);
        StartCoroutine(SendToBackend(userText));
    }

    private IEnumerator SendToBackend(string query)
    {
        yield return m_ApiClient.ResolveDestination(
            query,
            m_History,
            response =>
            {
                if (m_ThinkingBubble != null)
                {
                    Destroy(m_ThinkingBubble);
                    m_ThinkingBubble = null;
                }

                string answer = response != null && !string.IsNullOrEmpty(response.answer)
                    ? response.answer
                    : "I could not resolve that destination.";

                m_History.Add(new CampusApiClient.ChatHistoryMessage { role = "assistant", content = answer });
                SpawnBubble(answer, false);

                if (response != null && !string.IsNullOrEmpty(response.destination))
                    m_NavigationFlow.NavigateToDestination(response.destination);
            },
            error =>
            {
                if (m_ThinkingBubble != null)
                {
                    Destroy(m_ThinkingBubble);
                    m_ThinkingBubble = null;
                }

                SpawnBubble($"Could not reach the backend. {error}", false);
            });

        yield return new WaitForEndOfFrame();
        if (chatScrollRect != null)
            chatScrollRect.verticalNormalizedPosition = 0f;
    }

    private GameObject SpawnBubble(string text, bool isUser)
    {
        if (chatContainer == null)
            return null;

        GameObject row = new GameObject(isUser ? "UserRow" : "AssistantRow");
        row.transform.SetParent(chatContainer, false);
        RectTransform rowRT = row.AddComponent<RectTransform>();
        rowRT.sizeDelta = Vector2.zero;

        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = isUser ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.padding = new RectOffset(12, 12, 6, 6);

        ContentSizeFitter rowFitter = row.AddComponent<ContentSizeFitter>();
        rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        rowFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        GameObject bubble = new GameObject("Bubble");
        bubble.transform.SetParent(row.transform, false);
        Image bubbleImage = bubble.AddComponent<Image>();
        
        // Use the rounded sprite from CampusRuntimeUI
        CampusRuntimeUI ui = Object.FindObjectOfType<CampusRuntimeUI>();
        if (ui != null)
        {
            bubbleImage.sprite = ui.GetRoundedSprite();
            bubbleImage.type = Image.Type.Sliced;
        }

        bubbleImage.color = isUser
            ? new Color(0.25f, 0.35f, 1f, 1f) // Vibrant blue for user
            : new Color(0.12f, 0.14f, 0.18f, 1f); // Dark pill for assistant

        if (!isUser)
        {
            Outline outline = bubble.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.3f);
            outline.effectDistance = new Vector2(2, -2);
        }

        // Use layout group to pad the text inside the bubble
        VerticalLayoutGroup bubbleLayout = bubble.AddComponent<VerticalLayoutGroup>();
        bubbleLayout.padding = new RectOffset(18, 18, 12, 12);
        bubbleLayout.childAlignment = TextAnchor.MiddleCenter;
        bubbleLayout.childControlWidth = true;
        bubbleLayout.childControlHeight = true;

        ContentSizeFitter bubbleFitter = bubble.AddComponent<ContentSizeFitter>();
        bubbleFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        bubbleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(bubble.transform, false);
        
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 26;
        tmp.color = Color.white;
        tmp.alignment = isUser ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
        tmp.enableWordWrapping = true;
        
        // Prevent bubble from getting endlessly wide
        LayoutElement textLayout = textGO.AddComponent<LayoutElement>();
        textLayout.preferredWidth = Mathf.Min(text.Length * 14f, 600f);
        
        return row;
    }

    /// <summary>
    /// Clears all chat history and destroys spawned UI bubble GameObjects.
    /// </summary>
    public void ClearChat()
    {
        m_History.Clear();

        if (chatContainer != null)
        {
            for (int i = chatContainer.childCount - 1; i >= 0; i--)
                Destroy(chatContainer.GetChild(i).gameObject);
        }
    }
}
